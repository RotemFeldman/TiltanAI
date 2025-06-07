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
        bool goalAlreadySatisfied = true;
        foreach (var condition in goal.Conditions)
        {
            if (!workingState.ContainsKey(condition.Key) || 
                workingState[condition.Key] != condition.Value)
            {
                goalAlreadySatisfied = false;
                break;
            }
        }
        
        if (goalAlreadySatisfied)
        {
            Debug.Log($"Goal {goal.goalKey} is already satisfied");
            return plan; // Return empty plan
        }
        
        // Plan for each condition that is not satisfied
        foreach (var condition in goal.Conditions)
        {
            // Skip conditions that are already satisfied
            if (workingState.ContainsKey(condition.Key) && 
                workingState[condition.Key] == condition.Value)
            {
                continue;
            }
            
            // Find a plan for this specific condition
            if (!BuildPlan(usableActions, plan, condition.Key, condition.Value, workingState))
            {
                // If we can't plan for any condition, return empty plan
                Debug.LogWarning($"Could not find a plan for condition {condition.Key}={condition.Value}");
                return new Queue<GoapAction>();
            }
        }
        
        Debug.Log($"Plan found for goal {goal.goalKey} with {plan.Count} actions");
        return plan;
    }
    
    private bool BuildPlan(List<GoapAction> actions, Queue<GoapAction> plan, string goalKey, bool goalValue, Dictionary<string, bool> state)
    {
        // Base case: if the goal state is already satisfied
        if (state.ContainsKey(goalKey) && state[goalKey] == goalValue)
            return true;
            
        // Try each action that could satisfy the goal
        foreach (GoapAction action in actions)
        {
            // Skip actions that don't provide the goal effect
            if (!action.Effects.ContainsKey(goalKey) || action.Effects[goalKey] != goalValue)
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
                    if (!BuildPlan(actions, subPlan, precond.Key, precond.Value, state))
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
        
        // Sort by priority (lowest number = highest priority)
        List<GoapGoal> sortedGoals = new List<GoapGoal>(goals);
        sortedGoals.Sort((a, b) => a.priority.CompareTo(b.priority));

        
        foreach (var goal in sortedGoals)
        {
            // Check if any condition is not satisfied
            bool needsToBeAchieved = false;
            
            foreach (var condition in goal.Conditions)
            {
                // A goal condition needs to be achieved if:
                // 1. The state doesn't exist yet, or
                // 2. The state exists but has a different value than desired
                if (!worldState.ContainsKey(condition.Key) || 
                    worldState[condition.Key] != condition.Value)
                {
                    needsToBeAchieved = true;
                    Debug.Log($"Goal {goal.goalKey} needs to be achieved: {condition.Key}={condition.Value}");
                    break;
                }
            }
            
            if (needsToBeAchieved)
            {
                return goal;
            }
        }
        
        return null;
    }
}