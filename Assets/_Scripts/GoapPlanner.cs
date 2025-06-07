using System.Collections.Generic;
using UnityEngine;

public class GoapPlanner
{
    public Queue<GoapAction> Plan(List<GoapAction> availableActions, GoapGoal goal, Dictionary<string, bool> worldState)
    {
        // Create a deep copy of the world state to avoid modifying the original
        Dictionary<string, bool> workingState = new Dictionary<string, bool>(worldState);
        List<GoapAction> usableActions = new List<GoapAction>(availableActions);
        
        Queue<GoapAction> plan = new Queue<GoapAction>();
        
        // Check if the goal is already satisfied
        if (workingState.ContainsKey(goal.goalKey) && workingState[goal.goalKey])
        {
            Debug.Log($"Goal {goal.goalKey} is already satisfied");
            return plan; // Return empty plan
        }
        
        // Find a sequence of actions that achieve the goal
        if (BuildPlan(usableActions, plan, goal.goalKey, workingState))
        {
            return plan;
        }
        
        // If no plan was found, return an empty plan
        Debug.LogWarning($"Could not find a plan for goal {goal.goalKey}");
        return new Queue<GoapAction>();
    }
    
    private bool BuildPlan(List<GoapAction> actions, Queue<GoapAction> plan, string goalKey, Dictionary<string, bool> state)
    {
        // Base case: if the goal state is already satisfied
        if (state.ContainsKey(goalKey) && state[goalKey])
            return true;
            
        // Try each action that could satisfy the goal
        foreach (GoapAction action in actions)
        {
            // Skip actions that don't provide the goal effect
            if (!action.Effects.ContainsKey(goalKey) || action.Effects[goalKey] != true)
                continue;
                
            // Check if the action's preconditions can be met
            bool preconditionsMet = true;
            Dictionary<string, bool> precondsToSatisfy = new Dictionary<string, bool>();
            
            foreach (var precond in action.Preconditions)
            {
                if (!state.ContainsKey(precond.Key) || state[precond.Key] != precond.Value)
                {
                    precondsToSatisfy.Add(precond.Key, precond.Value);
                    preconditionsMet = false;
                }
            }
            
            // If all preconditions are met, we can use this action
            if (preconditionsMet)
            {
                // Apply the action's effects to the state
                Dictionary<string, bool> newState = new Dictionary<string, bool>(state);
                foreach (var effect in action.Effects)
                {
                    newState[effect.Key] = effect.Value;
                }
                
                // Add the action to the plan
                plan.Enqueue(action);
                return true;
            }
            else
            {
                // Try to satisfy each precondition recursively
                bool allPrecondsMet = true;
                
                foreach (var precond in precondsToSatisfy)
                {
                    Queue<GoapAction> subPlan = new Queue<GoapAction>();
                    if (!BuildPlan(actions, subPlan, precond.Key, state))
                    {
                        allPrecondsMet = false;
                        break;
                    }
                    
                    // Add the sub-plan actions
                    while (subPlan.Count > 0)
                    {
                        GoapAction subAction = subPlan.Dequeue();
                        plan.Enqueue(subAction);
                        
                        // Apply the action's effects to our working state
                        foreach (var effect in subAction.Effects)
                        {
                            state[effect.Key] = effect.Value;
                        }
                    }
                }
                
                if (allPrecondsMet)
                {
                    // Now we should be able to add this action
                    plan.Enqueue(action);
                    return true;
                }
            }
        }
        
        return false;
    }

    public GoapGoal SelectGoal(List<GoapGoal> goals, Dictionary<string, bool> worldState)
    {
        if (goals == null || goals.Count == 0)
        {
            Debug.LogWarning("No goals available to select from");
            return null;
        }
        
        // Sort by priority (highest first)
        List<GoapGoal> sortedGoals = new List<GoapGoal>(goals);
        sortedGoals.Sort((a, b) => b.priority.CompareTo(a.priority));
        
        foreach (var goal in sortedGoals)
        {
            // A goal is valid if either:
            // 1. The state doesn't exist yet (we need to achieve it)
            // 2. The state exists but is false (we need to make it true)
            if (!worldState.ContainsKey(goal.goalKey) || worldState[goal.goalKey] == false)
            {
                return goal;
            }
        }
        
        return null;
    }
}