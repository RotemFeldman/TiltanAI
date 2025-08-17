using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMovement : MonoBehaviour, IMovement
{
    NavMeshAgent agent;
    float lastSetTime;
    Vector3 lastDest;
    const float updateInterval = 0.15f; // seconds
    const float destMoveThreshold = 0.4f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = true;       // better stopping near target
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.angularSpeed = Mathf.Max(360f, agent.angularSpeed);
        agent.acceleration = Mathf.Max(12f, agent.acceleration);
        if (agent.stoppingDistance < 0.5f) agent.stoppingDistance = 0.5f;
    }

    float WeaponRange()
    {
        var w = GetComponent<SimpleCombat>()?.weapon;
        return w ? w.attackRange : 2.0f;
    }

    public void Search()
    {
        for (int i=0;i<8;i++)
        {
            var dir = (Random.insideUnitSphere * 10f); dir.y = 0;
            if (NavMesh.SamplePosition(transform.position + dir, out var hit, 10f, NavMesh.AllAreas))
            { agent.SetDestination(hit.position); lastDest = hit.position; lastSetTime = Time.time; return; }
        }
    }

    public void Chase(Transform target)
    {
        if (!target) return;

        // Don’t chase into safe-haven core
        var safe = GOAP.BuildLocation.Instance;
        if (safe && Vector3.Distance(target.position, safe.transform.position) < 3.5f)
            return;

        // If already in range to swing, don't keep resetting destination
        float range = WeaponRange();
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= range - 0.1f) return;

        // throttle updates, and only if target moved enough
        if ((Time.time - lastSetTime) < updateInterval) return;
        if ((lastDest - target.position).sqrMagnitude < destMoveThreshold * destMoveThreshold) return;

        // stop slightly inside swing range so we truly connect
        agent.stoppingDistance = Mathf.Max(0.1f, range * 0.6f);
        agent.SetDestination(target.position);
        lastDest = target.position;
        lastSetTime = Time.time;
        GetComponent<SimpleCombat>()?.SetTryingToEnterSafe(false);
    }

    public void RetreatFrom(Vector3 point)
    {
        var away = (transform.position - point).normalized * 6f;
        var dst = transform.position + away;
        if (NavMesh.SamplePosition(dst, out var hit, 8f, NavMesh.AllAreas))
        {
            agent.stoppingDistance = 0.1f;
            agent.SetDestination(hit.position);
            lastDest = hit.position;
            lastSetTime = Time.time;
        }
    }
}
