using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveTo", story: "[Agent] Move To [Location] using [PathfindingStrategy]", category: "Action", id: "610aa47df71112bac3556b3d8b62116b")]
public partial class MoveToAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Vector3> Location;

    [Tooltip("Choose the pathfinding algorithm to use")] 
    [SerializeReference] public BlackboardVariable<PathFindingStrategy> PathfindingStrategy;
    
    [Tooltip("Movement speed in units per second")]
    [SerializeReference] public BlackboardVariable<float> MovementSpeed;
    
    [Tooltip("Distance threshold to consider target reached")]
    [SerializeReference] public BlackboardVariable<float> ReachedThreshold;
    
    [Tooltip("Maximum time to spend trying to reach target")]
    [SerializeReference] public BlackboardVariable<float> TimeoutDuration;
    
    private PathNavAgent pathNavAgent;
    private float startTime;

    protected override Status OnStart()
    {
        // Validate inputs
        if (Agent?.Value == null)
        {
            Debug.LogError("MoveToAction: Agent is null");
            return Status.Failure;
        }

        if (Location?.Value == null)
        {
            Debug.LogError("MoveToAction: Location is null");
            return Status.Failure;
        }

        // Get PathNavAgent component
        pathNavAgent = Agent.Value.GetComponent<PathNavAgent>();
        if (pathNavAgent == null)
        {
            Debug.LogError("MoveToAction: PathNavAgent component not found on agent");
            return Status.Failure;
        }

        // Configure PathNavAgent with blackboard values or defaults
        pathNavAgent.SetMovementSpeed(MovementSpeed?.Value ?? 5f);
        pathNavAgent.SetReachedThreshold(ReachedThreshold?.Value ?? 0.1f);
        pathNavAgent.SetPathFindingStrategy(PathfindingStrategy?.Value ?? PathFindingStrategy.AStar);

        // Start movement using PathNavAgent
        Vector3 targetPosition = Location.Value;
        PathFindingStrategy strategy = PathfindingStrategy?.Value ?? PathFindingStrategy.AStar;
        
        if (!pathNavAgent.MoveToPosition(targetPosition, strategy))
        {
            Debug.LogWarning($"MoveToAction: Failed to find path to {targetPosition}");
            return Status.Failure;
        }

        startTime = Time.time;
        Debug.Log($"MoveToAction: Started moving to {targetPosition} using {strategy} strategy");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null || pathNavAgent == null)
            return Status.Failure;

        // Check timeout
        float timeoutDuration = TimeoutDuration?.Value ?? 30f;
        if (Time.time - startTime > timeoutDuration)
        {
            Debug.LogWarning("MoveToAction: Movement timed out");
            pathNavAgent.StopMovement();
            return Status.Failure;
        }

        // Check if we've reached the target
        if (pathNavAgent.HasReachedTarget)
        {
            Debug.Log("MoveToAction: Successfully reached target");
            return Status.Success;
        }

        // Check if PathNavAgent is still moving
        if (!pathNavAgent.IsMoving)
        {
            Debug.LogWarning("MoveToAction: Agent stopped moving without reaching target");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (pathNavAgent != null)
        {
            pathNavAgent.StopMovement();
        }
        
        Debug.Log("MoveToAction: Ended");
    }
}