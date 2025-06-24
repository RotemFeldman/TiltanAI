using System.Collections.Generic;
using UnityEngine;

public class GreedyBestFirstSearch : IPathFindingStrategy
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

        // Priority queue implementation using List
        List<Node> openList = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        Dictionary<Node, Node> parentMap = new Dictionary<Node, Node>();
        Dictionary<Node, float> heuristics = new Dictionary<Node, float>();

        // Initialize start
        openList.Add(startNode);
        heuristics[startNode] = CalculateHeuristic(startNode, targetNode, allowDiagonals);

        while (openList.Count > 0)
        {
            // Get node with lowest heuristic value
            Node current = GetNodeWithLowestHeuristic(openList, heuristics);
            nodesProcessed++;

            if (current == targetNode)
            {
                return new PathFindingResult(PathFindingUtils.RetracePath(startNode, targetNode, parentMap), nodesProcessed);
            }

            openList.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in grid.GetNeighbors(current, allowDiagonals))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                if (!openList.Contains(neighbor))
                {
                    heuristics[neighbor] = CalculateHeuristic(neighbor, targetNode, allowDiagonals);
                    parentMap[neighbor] = current;
                    openList.Add(neighbor);
                }
            }
        }

        return new PathFindingResult(new List<Node>(), nodesProcessed);
    }

    protected virtual float CalculateHeuristic(Node node, Node targetNode, bool allowDiagonals)
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

    // Overloaded method for backward compatibility
    protected virtual float CalculateHeuristic(Node node, Node targetNode)
    {
        return CalculateHeuristic(node, targetNode, false);
    }

    private Node GetNodeWithLowestHeuristic(List<Node> nodes, Dictionary<Node, float> heuristics)
    {
        Node lowestNode = nodes[0];
        float lowestHeuristic = heuristics[lowestNode];

        for (int i = 1; i < nodes.Count; i++)
        {
            float h = heuristics[nodes[i]];
            if (h < lowestHeuristic)
            {
                lowestNode = nodes[i];
                lowestHeuristic = h;
            }
        }

        return lowestNode;
    }
}