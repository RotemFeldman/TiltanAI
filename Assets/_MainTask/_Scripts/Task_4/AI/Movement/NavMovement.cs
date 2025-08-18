using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMovement : MonoBehaviour, IMovement
{
    NavMeshAgent agent;
    float lastSetTime;
    Vector3 lastDest;

    // tune as needed
    const float updateInterval = 0.15f; // seconds between SetDestination calls
    const float destMoveThreshold = 0.4f; // how far target must move before we repath

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = true; // better stopping near the goal
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // some sensible minimums for snappier chase
        agent.angularSpeed = Mathf.Max(540f, agent.angularSpeed);
        agent.acceleration = Mathf.Max(12f, agent.acceleration);
        if (agent.stoppingDistance < 0.05f) agent.stoppingDistance = 0.05f;
    }

    float WeaponRange()
    {
        var w = GetComponent<SimpleCombat>()?.weapon;
        return w ? w.attackRange : 2.0f;
    }

    public void Search()
    {
        for (int i = 0; i < 8; i++)
        {
            var dir = (Random.insideUnitSphere * 10f); dir.y = 0f;
            if (NavMesh.SamplePosition(transform.position + dir, out var hit, 10f, NavMesh.AllAreas))
            {
                agent.stoppingDistance = 0.1f;
                agent.SetDestination(hit.position);
                lastDest = hit.position;
                lastSetTime = Time.time;
                return;
            }
        }
    }

    public void Chase(Transform target)
    {
        if (!target) return;

        // Don’t chase into the safe-haven core (e.g., your BuildLocation)
        var safe = GOAP.BuildLocation.Instance;
        if (safe && Vector3.Distance(target.position, safe.transform.position) < 3.5f)
            return;

        // --- planarity: compare on XZ so height won't keep us "out of range"
        Vector3 me = transform.position;   me.y = 0f;
        Vector3 tp = target.position;      tp.y = 0f;

        float range = WeaponRange();
        float dist  = Vector3.Distance(me, tp);

        // already inside swing range -> let the Combat system swing without path spam
        if (dist <= range - 0.05f) return;

        // always keep stopping distance *inside* weapon range so we overlap and connect
        agent.stoppingDistance = Mathf.Clamp(range * 0.5f, 0.05f, range - 0.1f);

        // throttle path updates and only if target moved meaningfully
        if ((Time.time - lastSetTime) < updateInterval) return;
        if ((lastDest - target.position).sqrMagnitude < destMoveThreshold * destMoveThreshold) return;

        // try to sample a navmesh point near the target to avoid unreachable exact positions
        Vector3 desired = target.position;
        if (NavMesh.SamplePosition(target.position, out var hit, 1.5f, NavMesh.AllAreas))
            desired = hit.position;

        agent.SetDestination(desired);
        lastDest   = desired;
        lastSetTime = Time.time;

        // inform combat we are not trying to enter safe zone anymore
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
