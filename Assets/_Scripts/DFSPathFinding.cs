using System.Collections.Generic;
using UnityEngine;

public class DFSPathFinding : IPathFindingStrategy
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

        List<Node> path = new List<Node>();
        HashSet<Node> visited = new HashSet<Node>();
        Dictionary<Node, Node> parentMap = new Dictionary<Node, Node>();
        Stack<Node> stack = new Stack<Node>();

        stack.Push(startNode);
        visited.Add(startNode);

        while (stack.Count > 0)
        {
            Node current = stack.Pop();
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
                    stack.Push(neighbor);
                }
            }
        }
        return new PathFindingResult(path, nodesProcessed);
    }
}