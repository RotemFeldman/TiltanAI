using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemySensors))]
[RequireComponent(typeof(CombatAgent))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNNController : MonoBehaviour
{
    [Header("Brain JSON (different files = different brains)")]
    [Tooltip("Assign a TextAsset that contains the NeuralNetDef JSON (layers/weights/biases).")]
    public TextAsset brainJson;

    [Header("Decision")]
    [Tooltip("How often to re-evaluate the NN and choose an action (seconds).")]
    public float replanInterval = 0.25f;

    [Tooltip("How far to attempt to move away when retreating.")]
    public float retreatDistance = 5f;

    [Tooltip("Desired stop distance when chasing.")]
    public float chaseStopDistance = 0.3f;

    private EnemySensors sensors;
    private CombatAgent combat;
    private NavMeshAgent agent;
    private Damageable hp;

    private NeuralNetDef brain;
    private float[] outputBuf;
    private float timer;
    private bool initialized;
    private bool brainBrokenLogged; // avoid spamming logs

    private enum Act { Search = 0, Chase = 1, Attack = 2, Retreat = 3, Potion = 4 }

    void Awake()
    {
        sensors = GetComponent<EnemySensors>();
        combat  = GetComponent<CombatAgent>();
        agent   = GetComponent<NavMeshAgent>();
        hp      = GetComponent<Damageable>();

        // Set a sane stopping distance for chasing if not already set
        if (agent != null && agent.stoppingDistance <= 0.01f)
            agent.stoppingDistance = chaseStopDistance;

        // Do NOT disable if brainJson is null; spawner may assign it right after Instantiate.
        // We'll lazy-init in Update when brainJson becomes available.
    }

    void OnEnable()
    {
        // Try to init if someone enabled us after assignment
        TryInitBrain();
    }

    /// <summary>
    /// Call this to assign the brain at runtime (e.g., from your spawner).
    /// </summary>
    public void SetBrain(TextAsset ta)
    {
        brainJson = ta;
        initialized = false; // force re-init in case we are replacing a brain
        brainBrokenLogged = false;
        TryInitBrain();
        enabled = true; // ensure we’re running
    }

    /// <summary>
    /// Safe lazy init. Does nothing until brainJson is present and valid.
    /// </summary>
    public void TryInitBrain()
    {
        if (initialized) return;
        if (brainJson == null) return;

        brain = JsonUtility.FromJson<NeuralNetDef>(brainJson.text);
        if (brain == null || brain.layers == null || brain.layers.Length < 2)
        {
            if (!brainBrokenLogged)
            {
                Debug.LogError($"{name}: Invalid brain JSON (layers missing/too short). Falling back to heuristic chase.");
                brainBrokenLogged = true;
            }
            return;
        }

        int outSize = brain.layers[brain.layers.Length - 1];
        if (outSize <= 0)
        {
            if (!brainBrokenLogged)
            {
                Debug.LogError($"{name}: Brain output size is invalid ({outSize}). Falling back to heuristic chase.");
                brainBrokenLogged = true;
            }
            return;
        }

        outputBuf = new float[outSize];
        initialized = true;
    }

    void Update()
    {
        if (!initialized)
        {
            TryInitBrain();
            if (!initialized) return; // still waiting for brain assignment
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = replanInterval;
            ThinkAndAct();
        }
        
        // If truly idle, kick a small search step
        if (!agent.pathPending && !agent.hasPath && agent.velocity.sqrMagnitude < 0.0001f)
            DoSearch();
    }

    void ThinkAndAct()
    {
        sensors.Scan();

        float h01 = combat.Health01;
        sensors.GetInputs(h01, out var x, out var hasTarget);

        Act act;

        // If brain is not ready, or buffers are missing, or a Forward error occurs, fall back.
        if (!initialized || brain == null || outputBuf == null)
        {
            act = HeuristicDecision(hasTarget, h01);
        }
        else
        {
            try
            {
                // Adapt input size to the brain’s expected input size
                float[] xin = PrepareInputsForBrain(x);

                NeuralNet.Forward(brain, xin, outputBuf);
                int pick = NeuralNet.ArgMax(outputBuf);
                act = (Act)pick;
            }
            catch (System.Exception ex)
            {
                if (!brainBrokenLogged)
                {
                    Debug.LogError($"{name}: NeuralNet.Forward failed: {ex.Message}. Falling back to heuristic chase.");
                    brainBrokenLogged = true;
                }
                act = HeuristicDecision(hasTarget, h01);
            }
        }

        // Rule gates (threat assessment, safe-haven)
        bool danger = ThreatAssessor.IsDanger(h01, sensors.allyCount, sensors.enemyCount);
        if (danger && act != Act.Retreat && act != Act.Potion)
        {
            // If low HP and potion seen → prefer potion; else retreat
            act = (h01 < 0.6f && sensors.nearestPotion != null) ? Act.Potion : Act.Retreat;
        }

        if (sensors.inSafeHaven && (act == Act.Chase || act == Act.Attack))
        {
            // Avoid attacking while inside safe-haven (shouldn’t happen, but just in case)
            Debug.Log($"{name}: In safe-haven → overriding {act} to Search");
            act = Act.Search;
        }

        // If a target is visible but the brain chose to Search, prefer to chase
        if (hasTarget && act == Act.Search)
            act = Act.Chase;

        // Execute action
        switch (act)
        {
            case Act.Search:
                DoSearch();
                break;

            case Act.Chase:
                if (hasTarget && sensors.nearestTarget != null)
                {
                    float d = Vector3.Distance(transform.position, sensors.nearestTarget.position);

                    // Opportunistic attack even while "Chasing"
                    if (d <= combat.attackRange)
                    {
                        // Ensure we’re close enough to reliably land hits over multiple frames
                        agent.stoppingDistance = Mathf.Max(0.05f, Mathf.Min(combat.attackRange * 0.8f, chaseStopDistance));
                        combat.TryAttack(sensors.nearestTarget);
                    }
                    else
                    {
                        // Move to target
                        agent.stoppingDistance = Mathf.Max(0.05f, chaseStopDistance * 0.9f);

                        var targetPos = sensors.nearestTarget.position;
                        if (NavMesh.SamplePosition(targetPos, out var hit, 1.5f, NavMesh.AllAreas))
                            agent.SetDestination(hit.position);
                        else
                            agent.SetDestination(targetPos);
                    }
                }
                else
                {
                    DoSearch();
                }
                break;

            case Act.Attack:
                if (hasTarget && sensors.nearestTarget != null)
                {
                    float d = Vector3.Distance(transform.position, sensors.nearestTarget.position);

                    // Make sure we get slightly inside attackRange to truly connect
                    float stop = Mathf.Max(0.05f, Mathf.Min(combat.attackRange * 0.8f, chaseStopDistance));
                    if (d > combat.attackRange)
                    {
                        agent.stoppingDistance = stop;

                        var targetPos = sensors.nearestTarget.position;
                        if (NavMesh.SamplePosition(targetPos, out var hit, 1.5f, NavMesh.AllAreas))
                            agent.SetDestination(hit.position);
                        else
                            agent.SetDestination(targetPos);
                    }
                    else
                    {
                        // Helpful debug: check if target has any Damageable on its hierarchy
                        #if UNITY_EDITOR
                        var dmg =
                            sensors.nearestTarget.GetComponentInParent<Damageable>() ??
                            sensors.nearestTarget.GetComponentInChildren<Damageable>();
                        if (dmg == null)
                        {
                            Debug.LogWarning($"{name}: In attack range ({d:F2}) but target '{sensors.nearestTarget.name}' has no Damageable in parent/children.");
                        }
                        else
                        {
                            Debug.Log($"{name}: In attack range ({d:F2}), attacking '{sensors.nearestTarget.name}'.");
                        }
                        #endif

                        combat.TryAttack(sensors.nearestTarget);
                    }
                }
                else
                {
                    DoSearch();
                }
                break;

            case Act.Retreat:
                DoRetreat();
                break;

            case Act.Potion:
                if (sensors.nearestPotion != null)
                {
                    agent.stoppingDistance = 0.2f;

                    var potPos = sensors.nearestPotion.position;
                    if (NavMesh.SamplePosition(potPos, out var hit, 1.5f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);
                    else
                        agent.SetDestination(potPos);
                }
                else
                {
                    DoRetreat(); // fallback
                }
                break;
        }
    }

    private Act HeuristicDecision(bool hasTarget, float h01)
    {
        // Attack if already in range; otherwise chase if a target exists; else search
        if (hasTarget && sensors.nearestTarget != null)
        {
            float d = Vector3.Distance(transform.position, sensors.nearestTarget.position);
            if (d <= combat.attackRange) return Act.Attack;
            return Act.Chase;
        }
        return Act.Search;
    }

    void DoSearch()
    {
        // Small wander near current position
        Vector2 r = Random.insideUnitCircle * 6f;
        Vector3 want = transform.position + new Vector3(r.x, 0f, r.y);

        if (NavMesh.SamplePosition(want, out var hit, 4f, NavMesh.AllAreas))
        {
            agent.stoppingDistance = 0.1f;
            agent.SetDestination(hit.position);
        }
    }

    void DoRetreat()
    {
        // Move opposite of nearest target (or backward if none)
        Vector3 awayDir;
        if (sensors.nearestTarget != null)
            awayDir = (transform.position - sensors.nearestTarget.position).normalized;
        else
            awayDir = -transform.forward;

        Vector3 want = transform.position + awayDir * retreatDistance;

        if (NavMesh.SamplePosition(want, out var hit, retreatDistance, NavMesh.AllAreas))
        {
            agent.stoppingDistance = 0.1f;
            agent.SetDestination(hit.position);
        }
        else
        {
            // If sampling failed, try a small random nudge
            DoSearch();
        }
    }

    // Creates an input vector sized exactly to brain.layers[0], copying available features
    // and padding with zeros if the brain expects more than we provide.
    float[] PrepareInputsForBrain(float[] sensorInputs)
    {
        int want = (brain != null && brain.layers != null && brain.layers.Length > 0) ? brain.layers[0] : sensorInputs.Length;
        if (want == sensorInputs.Length) return sensorInputs;

        var arr = new float[want];
        int n = Mathf.Min(want, sensorInputs.Length);
        for (int i = 0; i < n; i++) arr[i] = sensorInputs[i];
        // Any extra inputs (if want > sensorInputs.Length) remain 0
        return arr;
    }
}