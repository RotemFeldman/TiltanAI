using UnityEngine;
using System.Collections.Generic;

public class PathNavAgent : MonoBehaviour
{
    private PathFinding pathFinder;
    private PathFindingResult currentPathResult = new PathFindingResult(new List<Node>(), 0);
    
    [Tooltip("Current target position for pathfinding")]
    public Vector3 someTargetPosition;
    
    [Tooltip("Choose between BFS or DFS pathfinding strategy")]
    [SerializeField] private PathFindingStrategy pathFindingStrategy = PathFindingStrategy.BFS;
    [Tooltip("Layer mask to filter grid objects for pathfinding")]
    [SerializeField] private LayerMask gridLayerMask;

    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    [SerializeField] private float movementSpeed = 5f;
    [Tooltip("Distance threshold to consider target reached")]
    [SerializeField] private float reachedThreshold = 0.1f;
    [Tooltip("Whether to automatically move along the path")]
    [SerializeField] private bool autoMove = false;
    [Tooltip("Allow diagonal movement when pathfinding")]
    [SerializeField] private bool canMoveDiagonally = true;
    
    [Header("Directional Acceleration")]
    [Tooltip("Acceleration multiplier when moving in the same direction")]
    [SerializeField] private float directionAcceleration = 1.2f;
    [Tooltip("Number of frames to consider for direction consistency")]
    [SerializeField] private int directionSampleFrames = 5;

    // Add these new variables for path visualization
    [Header("Path Visualization")]
    [Tooltip("Color of the path visualization")]
    [SerializeField] private Color pathColor = Color.yellow;
    [Tooltip("Width of the path lines and node markers")]
    [SerializeField] private float lineWidth = 0.2f;
    [Tooltip("Toggle path visualization on/off")]
    [SerializeField] private bool showPathGizmos = true;

    // Movement state
    private int currentPathIndex = 0;
    private bool isMoving = false;
    private bool hasReachedTarget = false;
    
    // Direction tracking for acceleration
    private Queue<Vector3> recentDirections = new Queue<Vector3>();
    private Vector3 lastDirection = Vector3.zero;
    private float currentSpeedMultiplier = 1f;

    void Awake()
    {
        var gridManager = FindFirstObjectByType<GridManager>();
        pathFinder = new PathFinding(gridManager);
    }

    void Update()
    {
        HandleMouseInput();
        
        if (autoMove && isMoving)
        {
            UpdateMovement();
        }
    }

    private void HandleMouseInput()
    {
        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // If we hit something on the grid layer
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayerMask))
            {
                // Update target position and find path
                someTargetPosition = hit.point;
                FindPath();
                if (autoMove)
                {
                    StartMovement();
                }
            }
        }
    }

    public void FindPath()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = someTargetPosition;

        // Pass the canMoveDiagonally setting to the pathfinding
        currentPathResult = pathFinder.FindPath(startPosition, targetPosition, pathFindingStrategy, canMoveDiagonally);

        if (currentPathResult.Path.Count > 0)
        {
            PathFindLogger.LogPath(startPosition, targetPosition, currentPathResult);
            currentPathIndex = 0;
            hasReachedTarget = false;
        }
        else
        {
            PathFindLogger.LogError(startPosition, targetPosition);
        }
    }

    public bool FindPathTo(Vector3 targetPosition, PathFindingStrategy strategy = PathFindingStrategy.AStar)
    {
        someTargetPosition = targetPosition;
        pathFindingStrategy = strategy;
        
        Vector3 startPosition = transform.position;
        // Pass the canMoveDiagonally setting to the pathfinding
        currentPathResult = pathFinder.FindPath(startPosition, targetPosition, pathFindingStrategy, canMoveDiagonally);

        if (currentPathResult.Path.Count > 0)
        {
            currentPathIndex = 0;
            hasReachedTarget = false;
            return true;
        }
        
        return false;
    }

    public void StartMovement()
    {
        if (currentPathResult.Path != null && currentPathResult.Path.Count > 0)
        {
            isMoving = true;
            hasReachedTarget = false;
            currentPathIndex = 0;
            // Reset direction tracking
            recentDirections.Clear();
            lastDirection = Vector3.zero;
            currentSpeedMultiplier = 1f;
        }
    }

    public void StopMovement()
    {
        isMoving = false;
        // Reset direction tracking
        recentDirections.Clear();
        lastDirection = Vector3.zero;
        currentSpeedMultiplier = 1f;
    }

    private void UpdateMovement()
    {
        if (currentPathResult.Path == null || currentPathResult.Path.Count == 0)
        {
            isMoving = false;
            return;
        }

        // Check if we've reached the final destination
        float distanceToTarget = Vector3.Distance(transform.position, someTargetPosition);
        if (distanceToTarget <= reachedThreshold)
        {
            isMoving = false;
            hasReachedTarget = true;
            return;
        }

        // Move along the path
        if (currentPathIndex < currentPathResult.Path.Count)
        {
            Vector3 currentWaypoint = currentPathResult.Path[currentPathIndex].worldPosition;
            Vector3 agentPosition = transform.position;
            
            // Move towards current waypoint
            Vector3 direction = (currentWaypoint - agentPosition).normalized;
            
            // Update directional acceleration
            UpdateDirectionalAcceleration(direction);
            
            float effectiveSpeed = movementSpeed * currentSpeedMultiplier;
            float moveDistance = effectiveSpeed * Time.deltaTime;
            
            transform.position = Vector3.MoveTowards(agentPosition, currentWaypoint, moveDistance);
            
            // Rotate to face movement direction
            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // Check if we've reached the current waypoint
            if (Vector3.Distance(transform.position, currentWaypoint) <= reachedThreshold)
            {
                currentPathIndex++;
                
                // If we've reached the last waypoint
                if (currentPathIndex >= currentPathResult.Path.Count)
                {
                    isMoving = false;
                    hasReachedTarget = true;
                }
            }
        }
    }

    private void UpdateDirectionalAcceleration(Vector3 currentDirection)
    {
        if (currentDirection.magnitude < 0.1f)
            return;

        // Check for significant direction change
        if (lastDirection.magnitude > 0.1f)
        {
            float directionChange = Vector3.Dot(lastDirection.normalized, currentDirection.normalized);
            
            // If direction changed significantly (dot product < 0.7 means > ~45 degree change)
            if (directionChange < 0.7f)
            {
                // Reset speed multiplier and clear direction history
                currentSpeedMultiplier = 1f;
                recentDirections.Clear();
            }
        }

        // Add current direction to recent directions queue
        recentDirections.Enqueue(currentDirection);
        
        // Keep only the specified number of recent directions
        while (recentDirections.Count > directionSampleFrames)
        {
            recentDirections.Dequeue();
        }

        // Calculate direction consistency only if we have enough samples and no recent direction change
        if (recentDirections.Count >= 2)
        {
            float consistency = CalculateDirectionConsistency();
            
            // Apply acceleration based on consistency
            float targetMultiplier = Mathf.Lerp(1f, directionAcceleration, consistency);
            currentSpeedMultiplier = Mathf.Lerp(currentSpeedMultiplier, targetMultiplier, Time.deltaTime * 5f);
        }

        lastDirection = currentDirection;
    }

    private float CalculateDirectionConsistency()
    {
        if (recentDirections.Count < 2)
            return 0f;

        Vector3[] directions = new Vector3[recentDirections.Count];
        recentDirections.CopyTo(directions, 0);

        float totalSimilarity = 0f;
        int comparisons = 0;

        // Compare each direction with the most recent one
        Vector3 mostRecentDirection = directions[directions.Length - 1];
        
        for (int i = 0; i < directions.Length - 1; i++)
        {
            float dotProduct = Vector3.Dot(directions[i].normalized, mostRecentDirection.normalized);
            totalSimilarity += Mathf.Max(0f, dotProduct); // Only positive correlations
            comparisons++;
        }

        return comparisons > 0 ? totalSimilarity / comparisons : 0f;
    }

    public bool MoveToPosition(Vector3 targetPosition, PathFindingStrategy strategy = PathFindingStrategy.AStar)
    {
        if (FindPathTo(targetPosition, strategy))
        {
            StartMovement();
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!showPathGizmos || pathFinder == null || currentPathResult.Path == null) return;
        
        var currentPath = currentPathResult.Path;
        if (currentPath == null || currentPath.Count == 0) return;

        // Set the color for the path
        Gizmos.color = pathColor;

        // Draw lines between each node in the path
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 startPos = currentPath[i].worldPosition;
            Vector3 endPos = currentPath[i + 1].worldPosition;
            
            // Draw line between nodes
            Gizmos.DrawLine(startPos, endPos);
            
            // Draw sphere at each node position
            Gizmos.DrawSphere(startPos, lineWidth * 0.5f);
        }
        
        // Draw sphere at the last node
        if (currentPath.Count > 0)
        {
            Gizmos.DrawSphere(currentPath[currentPath.Count - 1].worldPosition, lineWidth * 0.5f);
        }

        // Highlight current target waypoint
        if (isMoving && currentPathIndex < currentPath.Count)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentPath[currentPathIndex].worldPosition, lineWidth * 0.7f);
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20; 
        style.normal.textColor = Color.white; 
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(10, 10, 400, 170), ""); 

        if (currentPathResult.Path == null || currentPathResult.Path.Count == 0)
        {
            GUI.Label(new Rect(20, 20, 380, 150), "No path found.", style);
            return;
        }

        string statusText = isMoving ? "Moving" : (hasReachedTarget ? "Target Reached" : "Stopped");
        
        GUI.Label(new Rect(20, 20, 380, 150), 
            $"Path length: {currentPathResult.Path.Count}\n" +
            $"Nodes processed: {currentPathResult.NodesProcessed}\n" +
            $"Strategy: {pathFindingStrategy}\n" +
            $"Status: {statusText}\n" +
            $"Speed Multiplier: {currentSpeedMultiplier:F2}", 
            style);
    }

    public void SetPathFindingStrategy(PathFindingStrategy newStrategy)
    {
        pathFindingStrategy = newStrategy;
    }

    public void SetMovementSpeed(float speed)
    {
        movementSpeed = speed;
    }

    public void SetReachedThreshold(float threshold)
    {
        reachedThreshold = threshold;
    }

    public void SetCanMoveDiagonally(bool canMove)
    {
        canMoveDiagonally = canMove;
    }

    public void SetDirectionAcceleration(float acceleration)
    {
        directionAcceleration = acceleration;
    }

    // Public properties for external access
    public bool IsMoving => isMoving;
    public bool HasReachedTarget => hasReachedTarget;
    public List<Node> CurrentPath => currentPathResult.Path;
    public float MovementSpeed => movementSpeed;
    public PathFindingStrategy CurrentStrategy => pathFindingStrategy;
    public bool CanMoveDiagonally => canMoveDiagonally;
    public float DirectionAcceleration => directionAcceleration;
    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    
    public float GetPathProgress()
    {
        if (currentPathResult.Path == null || currentPathResult.Path.Count == 0)
            return 0f;
        return (float)currentPathIndex / currentPathResult.Path.Count;
    }
}