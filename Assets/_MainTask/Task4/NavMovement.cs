using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class NavMovement : MonoBehaviour, IMovement
{
    [Header("General")]
    public bool respectSafeHavens = true;
    public float minStopDistance = 0.2f;

    [Header("Search/Wander")]
    public float wanderRadius = 18f;
    public float wanderRepathInterval = 2.0f;

    [Header("Chase")]
    public float chaseRepathInterval = 0.25f;
    public float chaseStopDistance = 1.9f;

    [Header("Retreat")]
    public float retreatRepathInterval = 0.35f;
    public float retreatSampleRadius = 8f;
    public float defaultRetreatDistance = 12f;

    NavMeshAgent agent;
    float lastRepath;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = minStopDistance;

        // If we spawned inside, step out immediately
        if (respectSafeHavens && SafeHaven.Any)
        {
            if (SafeHaven.IsInsideFor(gameObject, transform.position, out var _))
            {
                Vector3 outPos = SafeHaven.ClampDestinationFor(gameObject, transform.position);
                agent.Warp(outPos);
            }
        }
    }

    void KeepOutsideGuard()
    {
        if (!respectSafeHavens || !SafeHaven.Any) return;

        // If currently inside, issue an immediate step out command
        if (SafeHaven.IsInsideFor(gameObject, transform.position, out var _))
        {
            Vector3 outPos = SafeHaven.ClampDestinationFor(gameObject, transform.position);
            if (NavMesh.SamplePosition(outPos, out var hit, 4f, NavMesh.AllAreas))
            {
                agent.stoppingDistance = minStopDistance;
                agent.SetDestination(hit.position);
                lastRepath = Time.time;
            }
        }
    }

    public void Search()
    {
        KeepOutsideGuard();
        if (Time.time - lastRepath < wanderRepathInterval) return;

        Vector3 origin = transform.position;
        Vector3 dir = Random.insideUnitSphere; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-3f) dir = transform.right;
        dir.Normalize();

        Vector3 desired = origin + dir * Random.Range(wanderRadius * 0.5f, wanderRadius);

        if (respectSafeHavens && SafeHaven.Any)
            desired = SafeHaven.ClampDestinationFor(gameObject, desired);

        if (NavMesh.SamplePosition(desired, out var hit, 6f, NavMesh.AllAreas))
        {
            agent.stoppingDistance = minStopDistance;
            agent.SetDestination(hit.position);
            lastRepath = Time.time;
        }
    }

    public void Chase(Transform target)
    {
        KeepOutsideGuard();
        if (target == null) return;
        if (Time.time - lastRepath < chaseRepathInterval) return;

        Vector3 desired = target.position;
        desired.y = transform.position.y;

        if (respectSafeHavens && SafeHaven.Any)
        {
            // If target is inside a haven, chase to the perimeter point instead
            if (SafeHaven.TryGetEdgePointTowardsTarget(gameObject, desired, out var edge))
                desired = edge;

            desired = SafeHaven.ClampDestinationFor(gameObject, desired);
        }

        if (NavMesh.SamplePosition(desired, out var hit, 4f, NavMesh.AllAreas))
        {
            agent.stoppingDistance = Mathf.Max(chaseStopDistance, minStopDistance);
            agent.SetDestination(hit.position);
            lastRepath = Time.time;
        }
    }

    public void RetreatFrom(Vector3 threatCenter)
    {
        KeepOutsideGuard();
        if (Time.time - lastRepath < retreatRepathInterval) return;

        Vector3 me = transform.position; me.y = threatCenter.y;
        Vector3 dir = (me - threatCenter);
        if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
        dir.Normalize();

        Vector3 desired = me + dir * defaultRetreatDistance;

        if (respectSafeHavens && SafeHaven.Any)
            desired = SafeHaven.ClampDestinationFor(gameObject, desired);

        if (NavMesh.SamplePosition(desired, out var hit, retreatSampleRadius, NavMesh.AllAreas))
        {
            agent.stoppingDistance = minStopDistance;
            agent.SetDestination(hit.position);
            lastRepath = Time.time;
        }
        else
        {
            // Try slight random variation
            Vector2 rnd = Random.insideUnitCircle.normalized * defaultRetreatDistance;
            Vector3 alt = me + new Vector3(rnd.x, 0f, rnd.y);
            if (respectSafeHavens && SafeHaven.Any)
                alt = SafeHaven.ClampDestinationFor(gameObject, alt);

            if (NavMesh.SamplePosition(alt, out var hit2, retreatSampleRadius, NavMesh.AllAreas))
            {
                agent.stoppingDistance = minStopDistance;
                agent.SetDestination(hit2.position);
                lastRepath = Time.time;
            }
        }
    }
}
