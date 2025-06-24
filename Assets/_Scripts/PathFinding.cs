using System;
using UnityEngine;
using System.Collections.Generic;

public class PathFinding
{
    private readonly GridManager grid;
    private readonly IPathFindingStrategy bfsStrategy;
    private readonly IPathFindingStrategy dfsStrategy;
    private readonly IPathFindingStrategy aStarStrategy;
    private readonly IPathFindingStrategy greedyBestFirstStrategy;

    public PathFinding(GridManager gridManager)
    {
        grid = gridManager ?? throw new ArgumentNullException(nameof(gridManager));
        
        // Initialize the strategies
        bfsStrategy = new BFSPathFinding();
        dfsStrategy = new DFSPathFinding();
        aStarStrategy = new AStarPathFinding();
        greedyBestFirstStrategy = new GreedyBestFirstSearch();
    }

    public PathFindingResult FindPath(Vector3 startPos, Vector3 targetPos, PathFindingStrategy strategy)
    {
        return FindPath(startPos, targetPos, strategy, false);
    }

    public PathFindingResult FindPath(Vector3 startPos, Vector3 targetPos, PathFindingStrategy strategy, bool allowDiagonals)
    {
        // Select the appropriate strategy
        IPathFindingStrategy pathFinder = strategy switch
        {
            PathFindingStrategy.BFS => bfsStrategy,
            PathFindingStrategy.DFS => dfsStrategy,
            PathFindingStrategy.AStar => aStarStrategy,
            _ => throw new ArgumentException($"Unsupported pathfinding strategy: {strategy}")
        };

        // Use the selected strategy to find the path
        return pathFinder.FindPath(grid, startPos, targetPos, allowDiagonals);
    }
}