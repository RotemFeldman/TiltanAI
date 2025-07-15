using System.Collections.Generic;
using UnityEngine;

public class GoapAction : IGoapAction
{
    protected string actionName;
    protected float cost = 1f;
    protected Dictionary<string, object> preconditions = new Dictionary<string, object>();
    protected Dictionary<string, object> effects = new Dictionary<string, object>();
    
    public GoapAction(string name, float actionCost = 1f)
    {
        actionName = name;
        cost = actionCost;
    }
    
    public virtual string GetName()
    {
        return actionName;
    }
    
    public virtual float GetCost()
    {
        return cost;
    }
    
    public virtual bool CanExecute(IGoapState worldState)
    {
        foreach (var precondition in preconditions)
        {
            var worldValue = worldState.GetState(precondition.Key);
            
            if (precondition.Value is bool boolValue)
            {
                if (!(worldValue is bool worldBool) || worldBool != boolValue)
                    return false;
            }
            else if (precondition.Value is int intValue)
            {
                if (!(worldValue is int worldInt) || worldInt < intValue)
                    return false;
            }
            else if (precondition.Value is float floatValue)
            {
                if (!(worldValue is float worldFloat) || worldFloat < floatValue)
                    return false;
            }
            else if (worldValue == null || !precondition.Value.Equals(worldValue))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public virtual IGoapState Execute(IGoapState worldState)
    {
        var newState = worldState.Clone();
        
        foreach (var effect in effects)
        {
            if (effect.Value is int intValue && newState.GetState(effect.Key) is int currentInt)
            {
                newState.SetState(effect.Key, currentInt + intValue);
            }
            else
            {
                newState.SetState(effect.Key, effect.Value);
            }
        }
        
        return newState;
    }
    
    public virtual Dictionary<string, object> GetPreconditions()
    {
        return new Dictionary<string, object>(preconditions);
    }
    
    public virtual Dictionary<string, object> GetEffects()
    {
        return new Dictionary<string, object>(effects);
    }
    
    public void AddPrecondition(string key, object value)
    {
        preconditions[key] = value;
    }
    
    public void AddEffect(string key, object value)
    {
        effects[key] = value;
    }
}
