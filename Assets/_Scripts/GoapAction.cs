using System.Collections.Generic;
using UnityEngine;

public abstract class GoapAction
{
    private Dictionary<string, bool> preconditions;
    private Dictionary<string, bool> effects;
    // Removed action cost field
    
    public Dictionary<string, bool> Preconditions => preconditions;
    public Dictionary<string, bool> Effects => effects;
    // Removed action cost property
    
    public GoapAction()
    {
        preconditions = new Dictionary<string, bool>();
        effects = new Dictionary<string, bool>();
    }
    
    // Check if the action can be performed based on the current world state
    public virtual bool CheckPreconditions(Dictionary<string, bool> worldState)
    {
        foreach (var kvp in preconditions)
        {
            if (!worldState.TryGetValue(kvp.Key, out bool value) || value != kvp.Value)
            {
                return false;
            }
        }
        return true;
    }
    
    // Perform the action
    public abstract void Perform();
    
    // Check if the action is complete
    public abstract bool IsDone();
}