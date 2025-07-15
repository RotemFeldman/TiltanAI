using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wander", story: "[Agent] wander around using [PathfindingStrategy]", category: "Action", id: "710bb58ef82223cbd4667c4e9c73227c")]
public partial class WanderAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    
    [SerializeReference] public BlackboardVariable<PathFindingStrategy> PathfindingStrategy;
    
    [Tooltip("Maximum distance to wander from starting position")]
    [SerializeReference] public BlackboardVariable<float> WanderRadius;
    
    [Tooltip("Minimum distance to move before selecting new target")]
    [SerializeReference] public BlackboardVariable<float> MinWanderDistance;
    
    [Tooltip("Time to wait at each destination before moving to next")]
    [SerializeReference] public BlackboardVariable<float> WaitTime;
    
    [Tooltip("Movement speed in units per second")]
    [SerializeReference] public BlackboardVariable<float> MovementSpeed;
    
    [Tooltip("Distance threshold to consider target reached")]
    [SerializeReference] public BlackboardVariable<float> ReachedThreshold;
    
    [Tooltip("Maximum time to spend trying to reach a single target")]
    [SerializeReference] public BlackboardVariable<float> MoveTimeout;
    
    [Tooltip("Total time to spend wandering (0 = infinite)")]
    [SerializeReference] public BlackboardVariable<float> WanderDuration;
    
    [Tooltip("Maximum attempts to find a valid wander target")]
    [SerializeReference] public BlackboardVariable<int> MaxPathAttempts;
    
    private PathNavAgent pathNavAgent;
    private GridManager gridManager;
    private float startTime;
    private float moveStartTime;
    private float waitStartTime;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private int pathAttempts;
    
    public enum WanderState
    {
        SelectingTarget,
        Moving,
        Waiting
    }
    
    private WanderState currentState;

    protected override Status OnStart()
    {
        // Validate inputs
        if (Agent?.Value == null)
        {
            Debug.LogError("WanderAction: Agent is null");
            return Status.Failure;
        }

        // Get PathNavAgent component
        pathNavAgent = Agent.Value.GetComponent<PathNavAgent>();
        if (pathNavAgent == null)
        {
            Debug.LogError("WanderAction: PathNavAgent component not found on agent");
            return Status.Failure;
        }

        // Find GridManager
        gridManager = GameObject.FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("WanderAction: GridManager not found in scene");
            return Status.Failure;
        }

        // Configure PathNavAgent with blackboard values
        pathNavAgent.SetMovementSpeed(MovementSpeed?.Value ?? 3f);
        pathNavAgent.SetReachedThreshold(ReachedThreshold?.Value ?? 0.1f);
        pathNavAgent.SetPathFindingStrategy(PathfindingStrategy?.Value ?? PathFindingStrategy.AStar);

        // Initialize wander state
        originalPosition = Agent.Value.transform.position;
        currentState = WanderState.SelectingTarget;
        startTime = Time.time;
        pathAttempts = 0;
        
        Debug.Log($"WanderAction: Started wandering from {originalPosition}");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null || pathNavAgent == null)
            return Status.Failure;

        // Check overall wander duration
        float wanderDurationValue = WanderDuration?.Value ?? 0f;
        if (wanderDurationValue > 0 && Time.time - startTime > wanderDurationValue)
        {
            Debug.Log("WanderAction: Wander duration completed");
            pathNavAgent.StopMovement();
            return Status.Success;
        }

        switch (currentState)
        {
            case WanderState.SelectingTarget:
                return HandleTargetSelection();
                
            case WanderState.Moving:
                return HandleMovement();
                
            case WanderState.Waiting:
                return HandleWaiting();
                
            default:
                return Status.Failure;
        }
    }

    private Status HandleTargetSelection()
    {
        // Try to find a valid wander target
        Vector3 randomTarget = GenerateRandomWanderTarget();
        
        // Use PathNavAgent to find and validate path
        PathFindingStrategy strategyValue = PathfindingStrategy?.Value ?? PathFindingStrategy.AStar;
        if (pathNavAgent.FindPathTo(randomTarget, strategyValue))
        {
            // Valid path found, start movement
            targetPosition = randomTarget;
            pathNavAgent.StartMovement();
            currentState = WanderState.Moving;
            moveStartTime = Time.time;
            pathAttempts = 0;
            
            Debug.Log($"WanderAction: Moving to {targetPosition}");
            return Status.Running;
        }
        else
        {
            // No path found, try again
            pathAttempts++;
            int maxAttemptsValue = MaxPathAttempts?.Value ?? 5;
            if (pathAttempts >= maxAttemptsValue)
            {
                Debug.LogWarning("WanderAction: Failed to find valid wander target after maximum attempts");
                return Status.Failure;
            }
            
            return Status.Running;
        }
    }

    private Status HandleMovement()
    {
        // Check movement timeout
        float moveTimeoutValue = MoveTimeout?.Value ?? 15f;
        if (Time.time - moveStartTime > moveTimeoutValue)
        {
            Debug.LogWarning("WanderAction: Movement timed out, selecting new target");
            pathNavAgent.StopMovement();
            currentState = WanderState.SelectingTarget;
            return Status.Running;
        }

        // Check if PathNavAgent has reached the target
        if (pathNavAgent.HasReachedTarget)
        {
            Debug.Log("WanderAction: Reached wander target, starting wait");
            currentState = WanderState.Waiting;
            waitStartTime = Time.time;
            return Status.Running;
        }

        // Check if PathNavAgent is still moving
        if (!pathNavAgent.IsMoving)
        {
            // Agent stopped moving but hasn't reached target - might be stuck
            Debug.LogWarning("WanderAction: Agent stopped moving without reaching target, selecting new target");
            currentState = WanderState.SelectingTarget;
            return Status.Running;
        }

        return Status.Running;
    }

    private Status HandleWaiting()
    {
        // Wait at the current position
        float waitTimeValue = WaitTime?.Value ?? 2f;
        if (Time.time - waitStartTime >= waitTimeValue)
        {
            Debug.Log("WanderAction: Wait completed, selecting new target");
            currentState = WanderState.SelectingTarget;
        }
        
        return Status.Running;
    }

    private Vector3 GenerateRandomWanderTarget()
    {
        // Get values from blackboard variables with fallbacks
        float wanderRadiusValue = WanderRadius?.Value ?? 5f;
        float minWanderDistanceValue = MinWanderDistance?.Value ?? 2f;
        
        // Generate random point within wander radius
        Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * wanderRadiusValue;
        
        // Ensure minimum distance from current position
        if (randomPoint.magnitude < minWanderDistanceValue)
        {
            randomPoint = randomPoint.normalized * minWanderDistanceValue;
        }
        
        // Convert to world position relative to original position
        Vector3 worldTarget = originalPosition + new Vector3(randomPoint.x, 0, randomPoint.y);
        
        // Clamp to grid bounds if possible
        if (gridManager != null)
        {
            worldTarget = ClampToGridBounds(worldTarget);
        }
        
        return worldTarget;
    }

    private Vector3 ClampToGridBounds(Vector3 position)
    {
        Vector3 gridCenter = gridManager.transform.position;
        Vector2 gridSize = gridManager.gridWorldSize;
        
        float minX = gridCenter.x - gridSize.x / 2;
        float maxX = gridCenter.x + gridSize.x / 2;
        float minZ = gridCenter.z - gridSize.y / 2;
        float maxZ = gridCenter.z + gridSize.y / 2;
        
        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            position.y,
            Mathf.Clamp(position.z, minZ, maxZ)
        );
    }

    protected override void OnEnd()
    {
        if (pathNavAgent != null)
        {
            pathNavAgent.StopMovement();
        }
        
        currentState = WanderState.SelectingTarget;
        Debug.Log("WanderAction: Ended");
    }

    // Public methods for runtime configuration using blackboard values
    public void UpdateWanderParameters()
    {
        if (pathNavAgent != null)
        {
            if (MovementSpeed?.Value != null)
                pathNavAgent.SetMovementSpeed(MovementSpeed.Value);
            
            if (ReachedThreshold?.Value != null)
                pathNavAgent.SetReachedThreshold(ReachedThreshold.Value);
            
            if (PathfindingStrategy?.Value != null)
                pathNavAgent.SetPathFindingStrategy(PathfindingStrategy.Value);
        }
    }

    // Public properties for monitoring
    public bool IsMoving => pathNavAgent != null && pathNavAgent.IsMoving;
    public WanderState CurrentState => currentState;
    public Vector3 CurrentTarget => targetPosition;
    public float GetProgress()
    {
        return pathNavAgent?.GetPathProgress() ?? 0f;
    }
}