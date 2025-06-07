using System.Collections.Generic;
using UnityEngine;

public abstract class GoapAction
{
    public Dictionary<string, bool> Preconditions = new Dictionary<string, bool>();
    public Dictionary<string, bool> Effects = new Dictionary<string, bool>();
    
    public abstract bool IsDone();
    public abstract void Perform();
    
    public virtual bool CheckProceduralPrecondition(Dictionary<string, bool> worldState)
    {
        // First check all dictionary preconditions
        foreach (var precondition in Preconditions)
        {
            if (!worldState.ContainsKey(precondition.Key))
            {
                Debug.Log($"[{GetType().Name}] Failed precondition check: '{precondition.Key}' state doesn't exist in world state");
                return false;
            }
            
            if (worldState[precondition.Key] != precondition.Value)
            {
                Debug.Log($"[{GetType().Name}] Failed precondition check: '{precondition.Key}' is {worldState[precondition.Key]}, but needs to be {precondition.Value}");
                return false;
            }
        }
        
        // If all dictionary preconditions are met, check custom conditions
        if (!CheckCustomPrecondition(worldState))
        {
            Debug.LogWarning($"[{GetType().Name}] Failed custom precondition check");
            return false;
        }
        
        return true;
    }
    
    protected virtual bool CheckCustomPrecondition(Dictionary<string, bool> worldState)
    {
        return true;
    }
}