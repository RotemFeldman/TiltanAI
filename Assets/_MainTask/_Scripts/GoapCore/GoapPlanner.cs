using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoapPlanner : IGoapPlanner
{
    private class PlanNode
    {
        public IGoapState state;
        public IGoapAction action;
        public PlanNode parent;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        
        public PlanNode(IGoapState nodeState, IGoapAction nodeAction, PlanNode nodeParent, float cost)
        {
            state = nodeState;
            action = nodeAction;
            parent = nodeParent;
            gCost = cost;
        }
    }
    
    public List<IGoapAction> CreatePlan(IGoapState worldState, IGoapGoal goal, List<IGoapAction> availableActions)
    {
        if (goal.IsAchieved(worldState))
        {
            return new List<IGoapAction>();
        }
        
        var openList = new List<PlanNode>();
        var closedList = new List<PlanNode>();
        
        var startNode = new PlanNode(worldState, null, null, 0);
        startNode.hCost = CalculateHeuristic(worldState, goal);
        openList.Add(startNode);
        
        while (openList.Count > 0)
        {
            var currentNode = openList.OrderBy(n => n.fCost).First();
            openList.Remove(currentNode);
            closedList.Add(currentNode);
            
            if (goal.IsAchieved(currentNode.state))
            {
                return ReconstructPlan(currentNode);
            }
            
            foreach (var action in availableActions)
            {
                if (!action.CanExecute(currentNode.state))
                    continue;
                
                var newState = action.Execute(currentNode.state);
                var newCost = currentNode.gCost + action.GetCost();
                
                if (closedList.Any(n => n.state.Equals(newState)))
                    continue;
                
                var existingNode = openList.FirstOrDefault(n => n.state.Equals(newState));
                if (existingNode == null)
                {
                    var newNode = new PlanNode(newState, action, currentNode, newCost);
                    newNode.hCost = CalculateHeuristic(newState, goal);
                    openList.Add(newNode);
                }
                else if (newCost < existingNode.gCost)
                {
                    existingNode.parent = currentNode;
                    existingNode.gCost = newCost;
                    existingNode.action = action;
                }
            }
        }
        
        return null; // No plan found
    }
    
    private List<IGoapAction> ReconstructPlan(PlanNode goalNode)
    {
        var plan = new List<IGoapAction>();
        var currentNode = goalNode;
        
        while (currentNode.parent != null)
        {
            plan.Insert(0, currentNode.action);
            currentNode = currentNode.parent;
        }
        
        return plan;
    }
    
    private float CalculateHeuristic(IGoapState state, IGoapGoal goal)
    {
        float heuristic = 0;
        var conditions = goal.GetConditions();
        
        foreach (var condition in conditions)
        {
            var stateValue = state.GetState(condition.Key);
            
            if (condition.Value is bool boolValue)
            {
                if (!(stateValue is bool stateBool) || stateBool != boolValue)
                    heuristic += 1f;
            }
            else if (condition.Value is int intValue)
            {
                int stateInt = (stateValue is int) ? (int)stateValue : 0;
                if (stateInt < intValue)
                    heuristic += (intValue - stateInt);

            }
            else if (stateValue == null || !condition.Value.Equals(stateValue))
            {
                heuristic += 1f;
            }
        }
        
        return heuristic;
    }
}
