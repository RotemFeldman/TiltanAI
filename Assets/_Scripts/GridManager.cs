using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public Vector2 gridWorldSize = new Vector2(10, 10); // Size of the world in Unity units
    public float nodeRadius = 0.5f;                      // Half the size of each node
    public LayerMask unwalkableMask;                    // Layer that marks obstacles

    Node[,] grid; // 2D array to store the grid

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    void Start()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    // Create the grid
    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];

        // Bottom-left corner of the grid in world space
        Vector3 worldBottomLeft = transform.position - 
            Vector3.right * gridWorldSize.x / 2 - 
            Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Calculate the world position of this node
                Vector3 worldPoint = worldBottomLeft + 
                                     Vector3.right * (x * nodeDiameter + nodeRadius) +
                                     Vector3.forward * (y * nodeDiameter + nodeRadius);

                // Check if this position is walkable
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }
    
    public IEnumerable<Node> GetAllNodes()
    {
        if (grid == null)
            yield break;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                if (grid[x, y] != null)
                    yield return grid[x, y];
            }
        }
    }

    public Node GetNodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = Mathf.Clamp01((worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    // Original method - only orthogonal neighbors (for backward compatibility)
    public List<Node> GetNeighbors(Node node)
    {
        return GetNeighbors(node, false);
    }

    // Overloaded method with diagonal movement option
    public List<Node> GetNeighbors(Node node, bool includeDiagonals)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Skip self
                if (x == 0 && y == 0) continue;

                // Skip diagonals if not allowed
                if (!includeDiagonals && Mathf.Abs(x) + Mathf.Abs(y) > 1) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    Node neighbor = grid[checkX, checkY];
                    
                    // For diagonal movement, check if both orthogonal paths are walkable
                    if (includeDiagonals && Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1)
                    {
                        // This is a diagonal neighbor
                        // Check the two orthogonal nodes that connect to this diagonal
                        Node horizontalNode = grid[node.gridX + x, node.gridY];
                        Node verticalNode = grid[node.gridX, node.gridY + y];
                        
                        // Only add diagonal neighbor if both orthogonal paths are walkable
                        if (horizontalNode.walkable && verticalNode.walkable)
                        {
                            neighbors.Add(neighbor);
                        }
                    }
                    else
                    {
                        // This is an orthogonal neighbor, add normally
                        neighbors.Add(neighbor);
                    }
                }
            }
        }

        return neighbors;
    }

    // Optional: Draw the grid in the editor
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw the overall grid boundary
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        // If we haven't created the grid yet, show the predicted grid layout
        if (grid == null)
        {
            float nodeDiameter = nodeRadius * 2;
            int gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            int gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

            Vector3 worldBottomLeft = transform.position - 
                Vector3.right * gridWorldSize.x / 2 - 
                Vector3.forward * gridWorldSize.y / 2;

            // Draw predicted grid layout
            Gizmos.color = new Color(1, 1, 1, 0.3f); // Semi-transparent white
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + 
                        Vector3.right * (x * nodeDiameter + nodeRadius) +
                        Vector3.forward * (y * nodeDiameter + nodeRadius);
                    Gizmos.DrawWireCube(worldPoint, Vector3.one * (nodeDiameter - 0.1f));
                }
            }
        }
        else
        {
            // Draw the actual grid nodes
            foreach (Node node in grid)
            {
                // Walkable nodes are white, obstacles are red
                Gizmos.color = node.walkable ? 
                    new Color(1, 1, 1, 0.3f) : // Semi-transparent white
                    new Color(1, 0, 0, 0.3f);  // Semi-transparent red

                // Draw filled cubes for each node
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
                
                // Draw wire frame for better visibility
                Gizmos.color = node.walkable ? 
                    new Color(1, 1, 1, 0.9f) : // More opaque white
                    new Color(1, 0, 0, 0.9f);  // More opaque red
                Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
            
           
        }
    }
#endif

    
}