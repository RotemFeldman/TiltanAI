using System.Collections.Generic;
using UnityEngine;

public class GoapState : IGoapState
{
    private Dictionary<string, object> states = new Dictionary<string, object>();
    
    public void SetState(string key, object value)
    {
        states[key] = value;
    }
    
    public object GetState(string key)
    {
        return states.ContainsKey(key) ? states[key] : null;
    }
    
    public bool HasState(string key)
    {
        return states.ContainsKey(key);
    }
    
    public void RemoveState(string key)
    {
        if (states.ContainsKey(key))
        {
            states.Remove(key);
        }
    }
    
    public Dictionary<string, object> GetAllStates()
    {
        return new Dictionary<string, object>(states);
    }
    
    public IGoapState Clone()
    {
        var clone = new GoapState();
        foreach (var state in states)
        {
            clone.SetState(state.Key, state.Value);
        }
        return clone;
    }
    
    public bool Equals(IGoapState other)
    {
        if (other == null) return false;
        
        var otherStates = other.GetAllStates();
        if (states.Count != otherStates.Count) return false;
        
        foreach (var state in states)
        {
            if (!otherStates.ContainsKey(state.Key) || 
                !otherStates[state.Key].Equals(state.Value))
            {
                return false;
            }
        }
        
        return true;
    }
}
