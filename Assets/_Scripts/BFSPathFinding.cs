using System.Collections.Generic;
using UnityEngine;

public class BFSPathFinding : IPathFindingStrategy
{
    // the function is virtual so it can be overridden in derived pathfinding strategies
    public virtual PathFindingResult FindPath(GridManager grid, Vector3 startPos, Vector3 targetPos)
    {
        return FindPath(grid, startPos, targetPos, false);
    }

    public virtual PathFindingResult FindPath(GridManager grid, Vector3 startPos, Vector3 targetPos, bool allowDiagonals)
    {
        Node startNode = grid.GetNodeFromWorldPoint(startPos);
        Node targetNode = grid.GetNodeFromWorldPoint(targetPos);
        int nodesProcessed = 0;

        List<Node> path = new List<Node>();
        HashSet<Node> visited = new HashSet<Node>();
        Dictionary<Node, Node> parentMap = new Dictionary<Node, Node>();
        Queue<Node> queue = new Queue<Node>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();
            nodesProcessed++;

            if (current == targetNode)
            {
                path = PathFindingUtils.RetracePath(startNode, targetNode, parentMap);
                return new PathFindingResult(path, nodesProcessed);
            }

            foreach (Node neighbor in grid.GetNeighbors(current, allowDiagonals))
            {
                if (!visited.Contains(neighbor) && neighbor.walkable)
                {
                    visited.Add(neighbor);
                    parentMap[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return new PathFindingResult(path, nodesProcessed);
    }
}