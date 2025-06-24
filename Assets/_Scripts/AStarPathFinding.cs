using System.Collections.Generic;
using UnityEngine;

public class AStarPathFinding : IPathFindingStrategy
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

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        Dictionary<Node, Node> parentMap = new Dictionary<Node, Node>();
        
        // Cost dictionaries
        Dictionary<Node, float> gCost = new Dictionary<Node, float>(); // Cost from start to node
        Dictionary<Node, float> fCost = new Dictionary<Node, float>(); // gCost + heuristic

        // Initialize start node
        openSet.Add(startNode);
        gCost[startNode] = 0;
        fCost[startNode] = CalculateHeuristic(startNode, targetNode, allowDiagonals);

        while (openSet.Count > 0)
        {
            Node current = GetLowestFCostNode(openSet, fCost);
            nodesProcessed++;

            if (current == targetNode)
            {
                List<Node> path = PathFindingUtils.RetracePath(startNode, targetNode, parentMap);
                return new PathFindingResult(path, nodesProcessed);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in grid.GetNeighbors(current, allowDiagonals))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = gCost[current] + CalculateDistance(current, neighbor);

                if (!gCost.ContainsKey(neighbor))
                    gCost[neighbor] = float.MaxValue;

                if (tentativeGCost < gCost[neighbor])
                {
                    parentMap[neighbor] = current;
                    gCost[neighbor] = tentativeGCost;
                    fCost[neighbor] = gCost[neighbor] + CalculateHeuristic(neighbor, targetNode, allowDiagonals);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return new PathFindingResult(new List<Node>(), nodesProcessed);
    }

    private float CalculateHeuristic(Node node, Node targetNode, bool allowDiagonals)
    {
        float dx = Mathf.Abs(node.gridX - targetNode.gridX);
        float dy = Mathf.Abs(node.gridY - targetNode.gridY);
        
        if (allowDiagonals)
        {
            // Octile distance for diagonal movement
            return Mathf.Max(dx, dy) + (1.4142f - 1) * Mathf.Min(dx, dy);
        }
        else
        {
            // Manhattan distance for orthogonal movement only
            return dx + dy;
        }
    }

    private float CalculateDistance(Node nodeA, Node nodeB)
    {
        float dx = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        float dy = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        // If diagonal movement
        if (dx == 1 && dy == 1)
        {
            return 1.4142f; // √2
        }
        
        // Straight movement
        return dx + dy;
    }

    private Node GetLowestFCostNode(List<Node> nodes, Dictionary<Node, float> fCost)
    {
        Node lowestFCostNode = nodes[0];
        foreach (Node node in nodes)
        {
            if (fCost[node] < fCost[lowestFCostNode])
            {
                lowestFCostNode = node;
            }
        }
        return lowestFCostNode;
    }
}