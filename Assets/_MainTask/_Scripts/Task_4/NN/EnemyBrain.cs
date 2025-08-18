using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(SimpleCombat))]
[RequireComponent(typeof(NavMovement))]
[RequireComponent(typeof(SimpleAwareness))]
public class EnemyBrain : MonoBehaviour
{
    public Action<float> OnEpisodeEnd;
    public Genome genome;

    // Ports
    public IMovement Move;
    public ICombat Combat;
    public IHealer Healer;
    public IAwareness Sense;

    float fitness, timeAlive;

    [Header("Decision Stability")]
    public float minActionHold = 0.35f;
    public float switchHysteresis = 0.2f;
    int lastAction = -1;
    float actionTimer;
    float lastActionScore;

    [Header("Fallback Net Shape")]
    public int defaultInputs = 12;
    public int defaultHidden = 16;
    public int defaultOutputs = 5; // [0:Search,1:Chase,2:Attack,3:Retreat,4:Drink]

    [Header("Patrol / Exploration")]
    [Tooltip("Force wandering for a short time after spawn.")]
    public bool patrolWhenNoTarget = true;
    [Tooltip("Seconds after spawn to always roam/search.")]
    public float forcedPatrolSeconds = 2.0f;
    [Tooltip("Extra score added to Search when no target is seen.")]
    public float extraSearchBiasNoTarget = 1.0f;
    [Tooltip("If idle with no target for this long, trigger a Search nudge.")]
    public float idleSearchNudgeInterval = 1.75f;

    NavMeshAgent agent;
    Health hp;
    bool wired, loggedMissing;
    float spawnTime, lastIdleNudge;

    public void LoadGenome(Genome g) { genome = g; }

    void Awake()
    {
        spawnTime = Time.time;
        WireComponents();
        EnsureGenomeReady();

        if (hp) hp.OnDied += _ => HandleDeath();
    }

    void Start()
    {
        if (!wired) WireComponents();
    }

    void WireComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        hp    = GetComponent<Health>();

        Move   = GetComponent<NavMovement>();
        Combat = GetComponent<SimpleCombat>();
        Healer = GetComponent<SimpleHealer>(); // optional
        Sense  = GetComponent<SimpleAwareness>();

        wired = (Move != null && Combat != null && Sense != null && agent != null && hp != null);
    }

    void EnsureGenomeReady()
    {
        if (genome == null || genome.net == null)
            genome = new Genome(defaultInputs, defaultHidden, defaultOutputs, initRandom: true);

        if (genome.net != null)
            genome.net.EnsureAllocated();
    }

    void HandleDeath()
    {
        if (agent) { agent.isStopped = true; agent.ResetPath(); }
        enabled = false;
        OnEpisodeEnd?.Invoke(fitness);
    }

    void Update()
    {
        if (!wired) WireComponents();
        if (!wired)
        {
            if (!loggedMissing)
            {
                Debug.LogWarning($"{name}/EnemyBrain: missing components → " +
                                 $"Move:{(Move!=null)} Combat:{(Combat!=null)} Sense:{(Sense!=null)} " +
                                 $"Agent:{(agent!=null)} Health:{(hp!=null)}");
                loggedMissing = true;
            }
            return;
        }

        if (Combat != null && Combat.IsDead) { HandleDeath(); return; }
        EnsureGenomeReady();
        if (genome == null || genome.net == null) { Move.Search(); return; }

        // --- FORCED PATROL WINDOW RIGHT AFTER SPAWN ---
        if (patrolWhenNoTarget && (Time.time - spawnTime) < forcedPatrolSeconds)
        {
            Move.Search();
            IdleSearchNudge(); // ensure a destination early
            return;
        }

        var x = BuildInputsSized(genome.net.inSize);
        var logits = genome.net.Forward(x);
        if (logits == null || logits.Length == 0) { Move.Search(); IdleSearchNudge(); return; }

        int iSearch  = logits.Length > 0 ? 0 : -1;
        int iChase   = logits.Length > 1 ? 1 : -1;
        int iAttack  = logits.Length > 2 ? 2 : -1;
        int iRetreat = logits.Length > 3 ? 3 : -1;
        int iDrink   = logits.Length > 4 ? 4 : -1;

        // Prefer Attack when truly in range
        if (iAttack != -1 && Sense != null && Sense.InAttackRange) logits[iAttack] += 1.25f;

        // Add SEARCH bias if we currently have no target
        if (patrolWhenNoTarget && (Sense == null || !Sense.Target) && iSearch != -1)
            logits[iSearch] += Mathf.Max(0f, extraSearchBiasNoTarget);

        int cand = ArgMax(logits);
        float candScore = logits[cand];

        if (lastAction >= 0)
        {
            actionTimer += Time.deltaTime;
            bool canSwitch = actionTimer >= minActionHold && (candScore > lastActionScore + switchHysteresis);
            if (!canSwitch) cand = lastAction;
        }

        // ===== EXECUTION with robust fallbacks =====
        if (cand == iSearch && iSearch != -1)
        {
            Move.Search();
        }
        else if (cand == iChase && iChase != -1)
        {
            if (Sense != null && Sense.Target) Move.Chase(Sense.Target);
            else Move.Search();
        }
        else if (cand == iAttack && iAttack != -1)
        {
            if (Sense == null || !Sense.Target) { Move.Search(); }
            else if (Sense.InAttackRange) { fitness += 3f * Combat.Attack(Sense.Target); }
            else { Move.Chase(Sense.Target); } // keep closing the gap while ATTACK selected
        }
        else if (cand == iRetreat && iRetreat != -1)
        {
            Move.RetreatFrom(Sense != null ? Sense.ThreatCenter : transform.position - transform.forward * 3f);
        }
        else if (cand == iDrink && iDrink != -1)
        {
            if ((Sense != null && Sense.PotionInRange) || (Healer != null && Healer.HasPotion))
                if (Healer != null && Healer.Drink()) fitness += 6f;
        }
        else
        {
            if (iAttack != -1 && Sense != null && Sense.Target && Sense.InAttackRange) fitness += 3f * Combat.Attack(Sense.Target);
            else if (iChase != -1 && Sense != null && Sense.Target) Move.Chase(Sense.Target);
            else Move.Search();
        }

        if (cand != lastAction) { lastAction = cand; actionTimer = 0f; lastActionScore = candScore; }

        // small rewards/penalties (training)
        timeAlive += Time.deltaTime;
        fitness += 0.02f * Time.deltaTime;
        fitness -= 4f * Combat.LastDamageTaken;

        // --- IDLE SEARCH NUDGE: if no target and agent is idle, kick off a new Search ---
        if (patrolWhenNoTarget && (Sense == null || !Sense.Target))
            IdleSearchNudge();
    }

    void IdleSearchNudge()
    {
        if (!agent) return;
        bool idle = !agent.hasPath || agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.2f);
        if (idle && (Time.time - lastIdleNudge) >= idleSearchNudgeInterval)
        {
            Move.Search();
            lastIdleNudge = Time.time;
        }
    }

    public void ForceEndEpisode(string why, float penalty = 0f)
    {
        if (!enabled) return;
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent) { agent.isStopped = true; agent.ResetPath(); }
        fitness -= penalty;
        enabled = false;
        OnEpisodeEnd?.Invoke(fitness);
    }

    float[] BuildInputsSized(int expected)
    {
        float hp01 = Sense != null ? Sense.Health01 : 1f;
        float wounded = hp01 < 0.4f ? 1f : 0f;
        int allies = Sense != null ? Mathf.Clamp(Sense.AlliesNearby, 0, 6) : 0;
        int enemies = Sense != null ? Mathf.Clamp(Sense.EnemiesNearby, 0, 6) : 0;
        float threat = Mathf.Clamp01(enemies / (float)(allies + 1));
        float noise = UnityEngine.Random.Range(-0.03f, 0.03f);

        float distToTarget01 = Sense != null ? Sense.DistToTarget01 : 1f;
        float inRange = (Sense != null && Sense.InAttackRange) ? 1f : 0f;
        float canSee = (Sense != null && Sense.CanSeeTarget) ? 1f : 0f;
        float potionNear = (Sense != null && Sense.PotionInRange) ? 1f : 0f;
        float distPotion01 = Sense != null ? Sense.DistToPotion01 : 1f;
        float hasPotion = (Healer != null && Healer.HasPotion) ? 1f : 0f;

        var base12 = new float[] {
            hp01, wounded, hasPotion,
            allies/6f, enemies/6f, threat,
            distToTarget01, inRange, canSee,
            potionNear, distPotion01,
            noise
        };

        if (expected == base12.Length) return base12;

        var outArr = new float[expected];
        int n = Mathf.Min(expected, base12.Length);
        Array.Copy(base12, outArr, n);
        return outArr; // extras remain zero
    }

    int ArgMax(float[] v)
    {
        int m = 0;
        for (int i = 1; i < v.Length; i++) if (v[i] > v[m]) m = i;
        return m;
    }
}
