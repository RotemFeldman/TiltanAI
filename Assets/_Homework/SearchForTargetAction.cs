using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SearchForTarget", story: "[Agent] Search for [Target] and change [state] to ChaseTarget",
    category: "Action", id: "2db4cf6bc86dcd5eb532d95d0a752833")]
public partial class SearchForTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<EnemyAttackerActions> State;
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private float maxSearchDistance = 20f;

    private Vector3 searchDestination;
    private bool hasSearchDestination;
    private NavMeshAgent navAgent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;
        navAgent = Agent.Value.GetComponent<NavMeshAgent>();
        if (navAgent == null) return Status.Failure;

        SetRandomSearchDestination();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null || navAgent == null) return Status.Failure;
        
        // Search for targets
        Collider[] hitColliders = Physics.OverlapSphere(navAgent.transform.position, searchRadius, 
            LayerMask.GetMask("VillageForce"));
        
        if (hitColliders.Length > 0)
        {
            Debug.Log($"Found target: {hitColliders[0].gameObject.name}");
            Target.Value = hitColliders[0].gameObject;
            State.Value = EnemyAttackerActions.ChaseTarget;
            navAgent.ResetPath();
            return Status.Success;
        }

        // Move while searching
        if (!hasSearchDestination || !navAgent.hasPath)
        {
            SetRandomSearchDestination();
        }

        Target.Value = null;
        return Status.Running;
    }

    private void SetRandomSearchDestination()
    {
        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        float randomDistance = UnityEngine.Random.Range(5f, maxSearchDistance);

        Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
        Vector3 randomPoint = Agent.Value.transform.position + (randomDirection * randomDistance);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, maxSearchDistance, NavMesh.AllAreas))
        {
            searchDestination = hit.position;
            navAgent.SetDestination(searchDestination);
            hasSearchDestination = true;
        }
        else
        {
            hasSearchDestination = false;
        }
    }

    protected override void OnEnd()
    {
    }

    private void OnDrawGizmos()
    {
        if (Agent?.Value == null) return;

        // Draw search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(navAgent.transform.position, searchRadius);

        // Draw max search distance
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(navAgent.transform.position, maxSearchDistance);

        // Draw current destination and path
        if (hasSearchDestination)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(searchDestination, 1f);
            Gizmos.DrawLine(navAgent.transform.position, searchDestination);

            // Draw NavMeshAgent path if available
            if (navAgent != null && navAgent.hasPath)
            {
                Gizmos.color = Color.red;
                var path = navAgent.path;
                Vector3 prevCorner = Agent.Value.transform.position;
                foreach (Vector3 corner in path.corners)
                {
                    Gizmos.DrawLine(prevCorner, corner);
                    prevCorner = corner;
                }
            }
        }
    }
}