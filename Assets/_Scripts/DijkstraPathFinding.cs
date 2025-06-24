using System.Collections.Generic;
using UnityEngine;

public class DijkstraPathFinding : IPathFindingStrategy
{
    public PathFindingResult FindPath(GridManager grid, Vector3 startPos, Vector3 targetPos)
    {
        return FindPath(grid, startPos, targetPos, false);
    }

    public PathFindingResult FindPath(GridManager grid, Vector3 startPos, Vector3 targetPos, bool allowDiagonals)
    {
        Node startNode = grid.GetNodeFromWorldPoint(startPos);
        Node targetNode = grid.GetNodeFromWorldPoint(targetPos);
        int nodesProcessed = 0;

        // Initialize data structures
        List<Node> path = new List<Node>();
        Dictionary<Node, float> distances = new Dictionary<Node, float>();
        Dictionary<Node, Node> parentMap = new Dictionary<Node, Node>();
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        // Set initial distances to infinity except start node
        foreach (Node node in grid.GetAllNodes())
        {
            distances[node] = float.MaxValue;
        }
        
        distances[startNode] = 0;
        
        // Add start node to open set
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Get node with smallest distance
            openSet.Sort((a, b) => distances[a].CompareTo(distances[b]));
            Node current = openSet[0];
            openSet.RemoveAt(0);
            
            nodesProcessed++;

            // If we reached the target, construct the path and return
            if (current == targetNode)
            {
                path = PathFindingUtils.RetracePath(startNode, targetNode, parentMap);
                return new PathFindingResult(path, nodesProcessed);
            }

            closedSet.Add(current);

            // Check all neighbors (now with diagonal support)
            foreach (Node neighbor in grid.GetNeighbors(current, allowDiagonals))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                // Calculate distance to neighbor
                float distance = distances[current] + CalculateDistance(current, neighbor);

                // If we found a better path to the neighbor
                if (distance < distances[neighbor])
                {
                    parentMap[neighbor] = current;
                    distances[neighbor] = distance;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // No path found
        return new PathFindingResult(new List<Node>(), nodesProcessed);
    }

    private float CalculateDistance(Node nodeA, Node nodeB)
    {
        // Calculate actual distance between nodes
        float dx = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        float dy = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        // If nodes are diagonal to each other, use diagonal distance
        if (dx == 1 && dy == 1)
        {
            return 1.4142f; // √2 for diagonal movement
        }

        // Otherwise straight line distance is one!
        return 1;
    }
}