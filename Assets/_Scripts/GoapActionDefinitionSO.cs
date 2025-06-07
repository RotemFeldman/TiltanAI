using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAction", menuName = "GOAP/Action Definition")]
public class GoapActionDefinitionSO : ScriptableObject
{
    [System.Serializable]
    public class WorldStateEntry
    {
        public string key;
        public bool value;
    }
    
    public string actionName;
    
    public List<WorldStateEntry> preconditions = new List<WorldStateEntry>();
    public List<WorldStateEntry> effects = new List<WorldStateEntry>();
    
    // Optional: Set a default name for new instances
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(actionName))
        {
            // Extract name from asset name if possible
            string assetName = name;
            if (assetName.EndsWith("Action"))
            {
                assetName = assetName.Substring(0, assetName.Length - "Action".Length);
            }
            
            actionName = assetName;
        }
    }
    
    // Add this method back to resolve the error
    public GoapAction CreateAction()
    {
        // Create a generic action from this definition
        GenericGoapAction action = new GenericGoapAction(actionName);
        
        // Add all preconditions
        foreach (var precond in preconditions)
        {
            action.Preconditions.Add(precond.key, precond.value);
        }
        
        // Add all effects
        foreach (var effect in effects)
        {
            action.Effects.Add(effect.key, effect.value);
        }
        
        return action;
    }
    
    public Dictionary<string, bool> GetPreconditions()
    {
        Dictionary<string, bool> conditions = new Dictionary<string, bool>();
        foreach (var entry in preconditions)
        {
            conditions[entry.key] = entry.value;
        }
        return conditions;
    }
    
    public Dictionary<string, bool> GetEffects()
    {
        Dictionary<string, bool> results = new Dictionary<string, bool>();
        foreach (var entry in effects)
        {
            results[entry.key] = entry.value;
        }
        return results;
    }
}