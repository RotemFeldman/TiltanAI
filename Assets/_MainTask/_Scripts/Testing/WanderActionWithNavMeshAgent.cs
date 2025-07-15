using UnityEngine;
using UnityEngine.AI;

public class NewWanderAction : GoapAction
{
    private NavMeshAgent navAgent;
    private Vector3 targetPosition;
    private float wanderRadius;
    private bool hasStarted;
    private bool hasReachedDestination;
    
    public NewWanderAction(NavMeshAgent agent, float radius = 10f) : base("Wander", 0.5f)
    {
        navAgent = agent;
        wanderRadius = radius;
        hasStarted = false;
        hasReachedDestination = false;
        
        // Simple precondition - agent should be available
        AddPrecondition("AvailableAgents", 1);
        
        // Effect - agent is now wandering/moving
        AddEffect("IsWandering", true);
        AddEffect("IsIdle", false);
    }
    
    public override bool CanExecute(IGoapState worldState)
    {
        // Can always wander if basic preconditions are met
        return base.CanExecute(worldState) && navAgent != null && navAgent.enabled;
    }
    
    public override IGoapState Execute(IGoapState worldState)
    {
        if (!hasStarted)
        {
            StartWandering();
            hasStarted = true;
        }
        
        return base.Execute(worldState);
    }
    
    private void StartWandering()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += navAgent.transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            navAgent.SetDestination(targetPosition);
        }
    }
    
    public bool IsComplete()
    {
        if (!hasStarted) return false;
        
        // Check if we've reached the destination
        if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            hasReachedDestination = true;
            return true;
        }
        
        return false;
    }
    
    public void Reset()
    {
        hasStarted = false;
        hasReachedDestination = false;
    }
}