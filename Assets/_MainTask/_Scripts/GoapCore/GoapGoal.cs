using System.Collections.Generic;
using UnityEngine;

public class GoapGoal : IGoapGoal
{
    protected string goalName;
    protected float priority;
    protected Dictionary<string, object> conditions = new Dictionary<string, object>();
    
    public GoapGoal(string name, float goalPriority = 1f)
    {
        goalName = name;
        priority = goalPriority;
    }
    
    public virtual string GetName()
    {
        return goalName;
    }
    
    public virtual bool IsAchieved(IGoapState worldState)
    {
        foreach (var condition in conditions)
        {
            var worldValue = worldState.GetState(condition.Key);
            
            if (condition.Value is bool boolValue)
            {
                if (!(worldValue is bool worldBool) || worldBool != boolValue)
                    return false;
            }
            else if (condition.Value is int intValue)
            {
                if (!(worldValue is int worldInt) || worldInt < intValue)
                    return false;
            }
            else if (condition.Value is float floatValue)
            {
                if (!(worldValue is float worldFloat) || worldFloat < floatValue)
                    return false;
            }
            else if (worldValue == null || !condition.Value.Equals(worldValue))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public virtual Dictionary<string, object> GetConditions()
    {
        return new Dictionary<string, object>(conditions);
    }
    
    public virtual float GetPriority()
    {
        return priority;
    }
    
    public void AddCondition(string key, object value)
    {
        conditions[key] = value;
    }
}
