using UnityEngine;
using System.Linq;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class EnemySensors : MonoBehaviour
{
    [Header("Sense")]
    public float senseRadius = 10f;
    public LayerMask agentMask;
    public LayerMask potionMask;
    public string[] targetTags = { "Villager", "Mage", "Messenger" };

    [Header("Normalize")]
    public int maxCountClamp = 6;
    public float maxSenseDistance = 25f;

    [Header("Safe Haven")]
    public Collider safeHavenTrigger;

    [Header("Debug")]
    public bool logScan;
    public bool drawLines = true;

    // Outputs
    public Transform nearestTarget { get; private set; }
    public Transform nearestPotion { get; private set; }
    public int allyCount { get; private set; }   // other ENEMIES (same team)
    public int enemyCount { get; private set; }  // villagers/mages/messengers (opponents)
    public bool inSafeHaven { get; private set; }

    void Awake()
    {
        // Provide sane defaults if not set in Inspector
        if (agentMask == 0)  agentMask  = LayerMask.GetMask("Agents"); // ensure we see both enemies and opponents
        if (potionMask == 0) potionMask = LayerMask.GetMask("Potion");
    }

    public void Scan()
    {
        var pos = transform.position;
        nearestTarget = null; float bestT = float.PositiveInfinity;
        nearestPotion = null; float bestP = float.PositiveInfinity;
        allyCount = 0; enemyCount = 0;

        // agents
        var cols = Physics.OverlapSphere(pos, senseRadius, agentMask, QueryTriggerInteraction.Collide);
        foreach (var c in cols)
        {
            var t = c.transform;
            if (t == transform) continue;

            float d2 = (t.position - pos).sqrMagnitude;

            // ✅ Correct team counting:
            if (t.CompareTag("Enemy"))          allyCount++; // same team
            if (t.CompareTag("Villager") ||
                t.CompareTag("Mage")    ||
                t.CompareTag("Messenger"))      enemyCount++; // opponents

            // target selection:
            if (targetTags.Any(tg => t.CompareTag(tg)))
                if (d2 < bestT) { bestT = d2; nearestTarget = t; }

            // ... existing code ...
        }

        // potions
        var pcols = Physics.OverlapSphere(pos, senseRadius, potionMask, QueryTriggerInteraction.Collide);
        foreach (var pc in pcols)
        {
            float d2 = (pc.transform.position - pos).sqrMagnitude;
            if (d2 < bestP) { bestP = d2; nearestPotion = pc.transform; }
        }

        // safe haven?
        inSafeHaven = safeHavenTrigger != null && safeHavenTrigger.bounds.Contains(pos);

        if (logScan)
        {
            string tgt = nearestTarget ? nearestTarget.name : "NONE";
            string pot = nearestPotion ? nearestPotion.name : "NONE";
            Debug.Log($"{name} Scan: allies={allyCount} enemies={enemyCount} tgt={tgt} pot={pot} safe={inSafeHaven}");
        }
    }

    // Normalized features [0..1]; 1.0 when “missing” distance
    public void GetInputs(float health01, out float[] inputs, out bool hasTarget)
    {
        float ally01  = Mathf.Clamp01((float)allyCount / maxCountClamp);
        float enemy01 = Mathf.Clamp01((float)enemyCount / maxCountClamp);

        float distT01 = 1f;
        if (nearestTarget) distT01 = Mathf.Clamp01(Vector3.Distance(transform.position, nearestTarget.position) / maxSenseDistance);

        float distP01 = 1f;
        if (nearestPotion) distP01 = Mathf.Clamp01(Vector3.Distance(transform.position, nearestPotion.position) / maxSenseDistance);

        hasTarget = nearestTarget != null;

        inputs = new float[] {
            health01,
            ally01,
            enemy01,
            distT01,
            distP01,
            inSafeHaven ? 1f : 0f,
            hasTarget ? 1f : 0f
        };
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, senseRadius);

        if (!drawLines) return;

        if (nearestTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nearestTarget.position);
        }
        if (nearestPotion)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nearestPotion.position);
        }
    }
}