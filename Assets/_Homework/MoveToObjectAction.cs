using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move To Object", story: "[Agent] Move To [TargetObject] using [PathfindingStrategy]", category: "Action", id: "710bb58ef93334dce5778d5f1e84449f")]
public partial class MoveToObjectAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> TargetObject;
    
    [Tooltip("Choose the pathfinding algorithm to use")]
    [SerializeReference] public BlackboardVariable<PathFindingStrategy> PathfindingStrategy;
    
    [Tooltip("Movement speed in units per second")]
    [SerializeReference] public BlackboardVariable<float> MovementSpeed;
    
    [Tooltip("Distance threshold to consider target reached")]
    [SerializeReference] public BlackboardVariable<float> ReachedThreshold;
    
    [Tooltip("Maximum time to spend trying to reach target")]
    [SerializeReference] public BlackboardVariable<float> TimeoutDuration;
    
    [Tooltip("Update target position if the object moves")]
    [SerializeReference] public BlackboardVariable<bool> TrackMovingTarget;
    
    [Tooltip("How often to recalculate path when tracking (seconds)")]
    [SerializeReference] public BlackboardVariable<float> PathUpdateInterval;
    
    [Tooltip("Minimum distance target must move to trigger path recalculation")]
    [SerializeReference] public BlackboardVariable<float> TargetMovementThreshold;
    
    [Tooltip("Offset from target object position")]
    [SerializeReference] public BlackboardVariable<Vector3> PositionOffset;
    
    [Tooltip("Stop early when within this distance of target")]
    [SerializeReference] public BlackboardVariable<float> StoppingDistance;
    
    [Tooltip("Use the target object's bounds to determine stopping position")]
    [SerializeReference] public BlackboardVariable<bool> UseTargetBounds;
    
    private PathNavAgent pathNavAgent;
    private GridManager gridManager;
    private float startTime;
    private float lastPathUpdateTime;
    private Vector3 lastTargetPosition;
    private Vector3 currentTargetPosition;
    private bool isMoving;
    private bool hasReachedTarget;

    protected override Status OnStart()
    {
        // Validate inputs
        if (Agent?.Value == null)
        {
            Debug.LogError("MoveToObjectAction: Agent is null");
            return Status.Failure;
        }

        if (TargetObject?.Value == null)
        {
            Debug.LogError("MoveToObjectAction: TargetObject is null");
            return Status.Failure;
        }

        // Get PathNavAgent component
        pathNavAgent = Agent.Value.GetComponent<PathNavAgent>();
        if (pathNavAgent == null)
        {
            Debug.LogError("MoveToObjectAction: PathNavAgent component not found on agent");
            return Status.Failure;
        }

        // Find GridManager
        gridManager = GameObject.FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("MoveToObjectAction: GridManager not found in scene");
            return Status.Failure;
        }

        // Configure PathNavAgent with blackboard values
        pathNavAgent.SetMovementSpeed(MovementSpeed?.Value ?? 5f);
        pathNavAgent.SetReachedThreshold(ReachedThreshold?.Value ?? 0.1f);
        pathNavAgent.SetPathFindingStrategy(PathfindingStrategy?.Value ?? PathFindingStrategy.AStar);

        // Initialize movement state
        startTime = Time.time;
        lastPathUpdateTime = 0f;
        isMoving = false;
        hasReachedTarget = false;
        
        // Calculate initial target position
        if (!UpdateTargetPosition())
        {
            return Status.Failure;
        }

        // Start pathfinding to target
        if (!StartMovementToTarget())
        {
            return Status.Failure;
        }

        Debug.Log($"MoveToObjectAction: Started moving to {TargetObject.Value.name} at {currentTargetPosition}");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null || TargetObject?.Value == null || pathNavAgent == null)
            return Status.Failure;

        // Check timeout
        float timeoutDuration = TimeoutDuration?.Value ?? 30f;
        if (Time.time - startTime > timeoutDuration)
        {
            Debug.LogWarning("MoveToObjectAction: Movement timed out");
            pathNavAgent.StopMovement();
            return Status.Failure;
        }

        // Update target position if tracking is enabled
        bool trackMovingTarget = TrackMovingTarget?.Value ?? false;
        if (trackMovingTarget)
        {
            UpdateMovingTarget();
        }

        // Check if we've reached the target
        if (CheckIfReachedTarget())
        {
            Debug.Log($"MoveToObjectAction: Successfully reached {TargetObject.Value.name}");
            pathNavAgent.StopMovement();
            return Status.Success;
        }

        // Check if PathNavAgent is still moving
        if (isMoving && !pathNavAgent.IsMoving)
        {
            // Agent stopped moving - check if it's because it reached the target
            if (pathNavAgent.HasReachedTarget)
            {
                // Path completed, but check actual distance to target
                if (CheckIfReachedTarget())
                {
                    Debug.Log($"MoveToObjectAction: Reached target via pathfinding");
                    return Status.Success;
                }
                else
                {
                    // Path completed but target not reached - try to recalculate
                    Debug.LogWarning("MoveToObjectAction: Path completed but target not reached, recalculating");
                    if (!StartMovementToTarget())
                    {
                        return Status.Failure;
                    }
                }
            }
            else
            {
                // Agent stopped without reaching path target - might be stuck
                Debug.LogWarning("MoveToObjectAction: Agent stopped moving without reaching target");
                return Status.Failure;
            }
        }

        return Status.Running;
    }

    private bool UpdateTargetPosition()
    {
        if (TargetObject?.Value == null)
            return false;

        Vector3 basePosition = TargetObject.Value.transform.position;
        Vector3 offset = PositionOffset?.Value ?? Vector3.zero;
        
        // Calculate position considering bounds if enabled
        bool useTargetBounds = UseTargetBounds?.Value ?? false;
        if (useTargetBounds)
        {
            Collider targetCollider = TargetObject.Value.GetComponent<Collider>();
            if (targetCollider != null)
            {
                // Get the closest point on the target's bounds to the agent
                Vector3 agentPosition = Agent.Value.transform.position;
                Vector3 closestPoint = targetCollider.ClosestPoint(agentPosition);
                
                // Apply stopping distance from the closest point
                float stoppingDistance = StoppingDistance?.Value ?? 0f;
                if (stoppingDistance > 0f)
                {
                    Vector3 directionFromTarget = (agentPosition - closestPoint).normalized;
                    basePosition = closestPoint + directionFromTarget * stoppingDistance;
                }
                else
                {
                    basePosition = closestPoint;
                }
            }
        }
        else
        {
            // Apply stopping distance from center position
            float stoppingDistance = StoppingDistance?.Value ?? 0f;
            if (stoppingDistance > 0f && Agent?.Value != null)
            {
                Vector3 directionFromTarget = (Agent.Value.transform.position - basePosition).normalized;
                basePosition += directionFromTarget * stoppingDistance;
            }
        }

        currentTargetPosition = basePosition + offset;
        return true;
    }

    private void UpdateMovingTarget()
    {
        float currentTime = Time.time;
        float updateInterval = PathUpdateInterval?.Value ?? 1f;
        
        // Check if it's time for an update
        if (currentTime - lastPathUpdateTime < updateInterval)
            return;

        // Calculate new target position
        Vector3 newTargetPosition;
        Vector3 basePosition = TargetObject.Value.transform.position;
        Vector3 offset = PositionOffset?.Value ?? Vector3.zero;
        newTargetPosition = basePosition + offset;

        // Check if target has moved significantly
        float movementThreshold = TargetMovementThreshold?.Value ?? 1f;
        float distanceMoved = Vector3.Distance(lastTargetPosition, newTargetPosition);
        
        if (distanceMoved >= movementThreshold)
        {
            Debug.Log($"MoveToObjectAction: Target moved {distanceMoved:F2} units, recalculating path");
            
            // Update target position
            UpdateTargetPosition();
            
            // Recalculate path
            if (StartMovementToTarget())
            {
                lastTargetPosition = newTargetPosition;
                lastPathUpdateTime = currentTime;
            }
        }
    }

    private bool StartMovementToTarget()
    {
        PathFindingStrategy strategy = PathfindingStrategy?.Value ?? PathFindingStrategy.AStar;
        
        if (pathNavAgent.FindPathTo(currentTargetPosition, strategy))
        {
            pathNavAgent.StartMovement();
            isMoving = true;
            lastTargetPosition = currentTargetPosition;
            return true;
        }
        else
        {
            Debug.LogWarning($"MoveToObjectAction: Failed to find path to {TargetObject.Value.name}");
            return false;
        }
    }

    private bool CheckIfReachedTarget()
    {
        if (Agent?.Value == null || TargetObject?.Value == null)
            return false;

        Vector3 agentPosition = Agent.Value.transform.position;
        float reachedThreshold = ReachedThreshold?.Value ?? 0.1f;
        
        // Check distance to actual target object
        bool useTargetBounds = UseTargetBounds?.Value ?? false;
        float distanceToTarget;
        
        if (useTargetBounds)
        {
            Collider targetCollider = TargetObject.Value.GetComponent<Collider>();
            if (targetCollider != null)
            {
                Vector3 closestPoint = targetCollider.ClosestPoint(agentPosition);
                distanceToTarget = Vector3.Distance(agentPosition, closestPoint);
            }
            else
            {
                distanceToTarget = Vector3.Distance(agentPosition, TargetObject.Value.transform.position);
            }
        }
        else
        {
            distanceToTarget = Vector3.Distance(agentPosition, TargetObject.Value.transform.position);
        }

        // Apply stopping distance
        float stoppingDistance = StoppingDistance?.Value ?? 0f;
        float effectiveThreshold = reachedThreshold + stoppingDistance;
        
        return distanceToTarget <= effectiveThreshold;
    }

    protected override void OnEnd()
    {
        if (pathNavAgent != null)
        {
            pathNavAgent.StopMovement();
        }
        
        isMoving = false;
        hasReachedTarget = false;
        Debug.Log("MoveToObjectAction: Ended");
    }

    // Public methods for runtime configuration
    public void UpdateMovementParameters()
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
    public bool IsMoving => isMoving && pathNavAgent != null && pathNavAgent.IsMoving;
    public GameObject CurrentTarget => TargetObject?.Value;
    public Vector3 CurrentTargetPosition => currentTargetPosition;
    public float GetDistanceToTarget()
    {
        if (Agent?.Value == null || TargetObject?.Value == null)
            return float.MaxValue;
            
        return Vector3.Distance(Agent.Value.transform.position, TargetObject.Value.transform.position);
    }
    
    public float GetProgress()
    {
        return pathNavAgent?.GetPathProgress() ?? 0f;
    }
    
    public bool HasValidTarget => TargetObject?.Value != null;
}