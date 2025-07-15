using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Patrol", story: "[Agent] patrol between [Waypoints] using pathfinding", category: "Action", id: "820cc69fg93334dce5778d5f0d84338d")]
public partial class PatrolAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    
    [Tooltip("List of waypoints to patrol between")]
    [SerializeReference] public BlackboardVariable<List<Vector3>> Waypoints;
    
    [Tooltip("Choose the pathfinding algorithm to use")]
    [SerializeReference] public BlackboardVariable<PathFindingStrategy> PathfindingStrategy;
    
    [Tooltip("Movement speed in units per second")]
    [SerializeReference] public BlackboardVariable<float> MovementSpeed;
    
    [Tooltip("Distance threshold to consider waypoint reached")]
    [SerializeReference] public BlackboardVariable<float> ReachedThreshold;
    
    [Tooltip("Time to wait at each waypoint before moving to next")]
    [SerializeReference] public BlackboardVariable<float> WaitTime;
    
    [Tooltip("Maximum time to spend trying to reach a single waypoint")]
    [SerializeReference] public BlackboardVariable<float> MoveTimeout;
    
    [Tooltip("Total time to spend patrolling (0 = infinite)")]
    [SerializeReference] public BlackboardVariable<float> PatrolDuration;
    
    [Tooltip("Start from the closest waypoint to current position")]
    [SerializeReference] public BlackboardVariable<bool> StartFromClosest;
    
    [Tooltip("Patrol in reverse order when reaching the end")]
    [SerializeReference] public BlackboardVariable<bool> PingPongMode;
    
    [Tooltip("Continue patrolling in a loop")]
    [SerializeReference] public BlackboardVariable<bool> LoopPatrol;
    
    [Tooltip("Maximum attempts to find a path to a waypoint")]
    [SerializeReference] public BlackboardVariable<int> MaxPathAttempts;
    
    private PathNavAgent pathNavAgent;
    private GridManager gridManager;
    private float startTime;
    private float moveStartTime;
    private float waitStartTime;
    private int currentWaypointIndex;
    private int pathAttempts;
    private bool isReversing; // For ping-pong mode
    private int patrolDirection; // 1 for forward, -1 for reverse
    
    public enum PatrolState
    {
        SelectingWaypoint,
        MovingToWaypoint,
        WaitingAtWaypoint,
        PatrolComplete
    }
    
    private PatrolState currentState;

    protected override Status OnStart()
    {
        // Validate inputs
        if (Agent?.Value == null)
        {
            Debug.LogError("PatrolAction: Agent is null");
            return Status.Failure;
        }

        if (Waypoints?.Value == null || Waypoints.Value.Count == 0)
        {
            Debug.LogError("PatrolAction: No waypoints provided for patrol");
            return Status.Failure;
        }

        // Get PathNavAgent component
        pathNavAgent = Agent.Value.GetComponent<PathNavAgent>();
        if (pathNavAgent == null)
        {
            Debug.LogError("PatrolAction: PathNavAgent component not found on agent");
            return Status.Failure;
        }

        // Find GridManager
        gridManager = GameObject.FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("PatrolAction: GridManager not found in scene");
            return Status.Failure;
        }

        // Configure PathNavAgent with blackboard values
        pathNavAgent.SetMovementSpeed(MovementSpeed?.Value ?? 3f);
        pathNavAgent.SetReachedThreshold(ReachedThreshold?.Value ?? 0.1f);
        pathNavAgent.SetPathFindingStrategy(PathfindingStrategy?.Value ?? PathFindingStrategy.AStar);

        // Initialize patrol state
        InitializePatrol();
        currentState = PatrolState.SelectingWaypoint;
        startTime = Time.time;
        pathAttempts = 0;
        patrolDirection = 1; // Start moving forward
        
        Debug.Log($"PatrolAction: Started patrolling with {Waypoints.Value.Count} waypoints, starting from index {currentWaypointIndex}");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null || pathNavAgent == null)
            return Status.Failure;

        // Check overall patrol duration
        float patrolDurationValue = PatrolDuration?.Value ?? 0f;
        if (patrolDurationValue > 0 && Time.time - startTime > patrolDurationValue)
        {
            Debug.Log("PatrolAction: Patrol duration completed");
            pathNavAgent.StopMovement();
            return Status.Success;
        }

        switch (currentState)
        {
            case PatrolState.SelectingWaypoint:
                return HandleWaypointSelection();
                
            case PatrolState.MovingToWaypoint:
                return HandleMovement();
                
            case PatrolState.WaitingAtWaypoint:
                return HandleWaiting();
                
            case PatrolState.PatrolComplete:
                return Status.Success;
                
            default:
                return Status.Failure;
        }
    }

    private void InitializePatrol()
    {
        bool startFromClosest = StartFromClosest?.Value ?? false;
        
        if (startFromClosest)
        {
            // Find the closest waypoint to current position
            Vector3 agentPosition = Agent.Value.transform.position;
            float closestDistance = float.MaxValue;
            int closestIndex = 0;
            
            for (int i = 0; i < Waypoints.Value.Count; i++)
            {
                float distance = Vector3.Distance(agentPosition, Waypoints.Value[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            
            currentWaypointIndex = closestIndex;
        }
        else
        {
            // Start from the first waypoint
            currentWaypointIndex = 0;
        }
    }

    private Status HandleWaypointSelection()
    {
        if (Waypoints?.Value == null || currentWaypointIndex >= Waypoints.Value.Count || currentWaypointIndex < 0)
        {
            Debug.LogError("PatrolAction: Invalid waypoint index");
            return Status.Failure;
        }

        Vector3 targetWaypoint = Waypoints.Value[currentWaypointIndex];
        
        // Use PathNavAgent to find and validate path
        PathFindingStrategy strategyValue = PathfindingStrategy?.Value ?? PathFindingStrategy.AStar;
        if (pathNavAgent.FindPathTo(targetWaypoint, strategyValue))
        {
            // Valid path found, start movement
            pathNavAgent.StartMovement();
            currentState = PatrolState.MovingToWaypoint;
            moveStartTime = Time.time;
            pathAttempts = 0;
            
            Debug.Log($"PatrolAction: Moving to waypoint {currentWaypointIndex} at {targetWaypoint}");
            return Status.Running;
        }
        else
        {
            // No path found, try again or skip waypoint
            pathAttempts++;
            int maxAttemptsValue = MaxPathAttempts?.Value ?? 3;
            if (pathAttempts >= maxAttemptsValue)
            {
                Debug.LogWarning($"PatrolAction: Failed to find path to waypoint {currentWaypointIndex} after maximum attempts, skipping to next");
                
                // Move to next waypoint
                if (!AdvanceToNextWaypoint())
                {
                    currentState = PatrolState.PatrolComplete;
                    return Status.Success;
                }
                
                pathAttempts = 0;
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
            Debug.LogWarning($"PatrolAction: Movement to waypoint {currentWaypointIndex} timed out, moving to next");
            pathNavAgent.StopMovement();
            
            // Move to next waypoint
            if (!AdvanceToNextWaypoint())
            {
                currentState = PatrolState.PatrolComplete;
                return Status.Success;
            }
            
            currentState = PatrolState.SelectingWaypoint;
            return Status.Running;
        }

        // Check if PathNavAgent has reached the waypoint
        if (pathNavAgent.HasReachedTarget)
        {
            Debug.Log($"PatrolAction: Reached waypoint {currentWaypointIndex}, starting wait");
            currentState = PatrolState.WaitingAtWaypoint;
            waitStartTime = Time.time;
            return Status.Running;
        }

        // Check if PathNavAgent is still moving
        if (!pathNavAgent.IsMoving)
        {
            // Agent stopped moving but hasn't reached target - might be stuck
            Debug.LogWarning($"PatrolAction: Agent stopped moving without reaching waypoint {currentWaypointIndex}, selecting next waypoint");
            
            // Move to next waypoint
            if (!AdvanceToNextWaypoint())
            {
                currentState = PatrolState.PatrolComplete;
                return Status.Success;
            }
            
            currentState = PatrolState.SelectingWaypoint;
            return Status.Running;
        }

        return Status.Running;
    }

    private Status HandleWaiting()
    {
        // Wait at the current waypoint
        float waitTimeValue = WaitTime?.Value ?? 1f;
        if (Time.time - waitStartTime >= waitTimeValue)
        {
            Debug.Log($"PatrolAction: Wait completed at waypoint {currentWaypointIndex}, moving to next");
            
            // Move to next waypoint
            if (!AdvanceToNextWaypoint())
            {
                currentState = PatrolState.PatrolComplete;
                return Status.Success;
            }
            
            currentState = PatrolState.SelectingWaypoint;
        }
        
        return Status.Running;
    }

    private bool AdvanceToNextWaypoint()
    {
        bool pingPongMode = PingPongMode?.Value ?? false;
        bool loopPatrol = LoopPatrol?.Value ?? true;
        
        if (pingPongMode)
        {
            return AdvancePingPong();
        }
        else if (loopPatrol)
        {
            return AdvanceLoop();
        }
        else
        {
            return AdvanceLinear();
        }
    }

    private bool AdvancePingPong()
    {
        currentWaypointIndex += patrolDirection;
        
        // Check if we've reached the end and need to reverse
        if (currentWaypointIndex >= Waypoints.Value.Count)
        {
            currentWaypointIndex = Waypoints.Value.Count - 2; // Go back to second-to-last
            patrolDirection = -1; // Reverse direction
        }
        else if (currentWaypointIndex < 0)
        {
            currentWaypointIndex = 1; // Go to second waypoint
            patrolDirection = 1; // Forward direction
        }
        
        // Continue patrolling indefinitely in ping-pong mode
        return true;
    }

    private bool AdvanceLoop()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % Waypoints.Value.Count;
        return true; // Continue patrolling indefinitely
    }

    private bool AdvanceLinear()
    {
        currentWaypointIndex++;
        return currentWaypointIndex < Waypoints.Value.Count; // Stop when we reach the end
    }

    protected override void OnEnd()
    {
        if (pathNavAgent != null)
        {
            pathNavAgent.StopMovement();
        }
        
        currentState = PatrolState.PatrolComplete;
        Debug.Log("PatrolAction: Ended");
    }

    // Public methods for runtime configuration
    public void UpdatePatrolParameters()
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
    public PatrolState CurrentState => currentState;
    public int CurrentWaypointIndex => currentWaypointIndex;
    public Vector3 CurrentTarget => 
        Waypoints?.Value != null && currentWaypointIndex >= 0 && currentWaypointIndex < Waypoints.Value.Count 
            ? Waypoints.Value[currentWaypointIndex] 
            : Vector3.zero;
    
    public float GetProgress()
    {
        return pathNavAgent?.GetPathProgress() ?? 0f;
    }

    public float GetOverallPatrolProgress()
    {
        if (Waypoints?.Value == null || Waypoints.Value.Count == 0)
            return 0f;
            
        return (float)currentWaypointIndex / Waypoints.Value.Count;
    }
}