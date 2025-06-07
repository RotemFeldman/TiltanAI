using System.Collections.Generic;
using UnityEngine;

public class GoapPlanner
{
    public Queue<GoapAction> Plan(List<GoapAction> actions, GoapGoal goal, Dictionary<string, bool> worldState)
    {
        Queue<GoapAction> plan = new Queue<GoapAction>();

        foreach (GoapAction action in actions)
        {
            if (action.CheckProceduralPrecondition(worldState))
            {
                if (action.Effects.ContainsKey(goal.goalKey) && action.Effects[goal.goalKey] == false)
                {
                    continue;
                }

                // Add this action to the plan
                plan.Enqueue(action);

                // Apply effects to simulated world state
                foreach (var effect in action.Effects)
                {
                    worldState[effect.Key] = effect.Value;
                }

                
                break;
            }
        }

        return plan;
    }

    public GoapGoal SelectGoal(List<GoapGoal> goals, Dictionary<string, bool> worldState)
    {
        // Just pick the highest-priority goal that is not already satisfied
        goals.Sort((a, b) => b.priority.CompareTo(a.priority));
        foreach (var goal in goals)
        {
            if (!worldState.ContainsKey(goal.goalKey) || worldState[goal.goalKey] == false)
            {
                return goal;
            }
        }
        return null;
    }
}