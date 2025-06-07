using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGoalDefinition", menuName = "GOAP/Goal Definition")]
public class GoapGoalDefinitionSO : ScriptableObject
{
    [System.Serializable]
    public class GoalState
    {
        // Removed the attribute since we'll handle it in an editor script
        public string key;
        public bool value;
    }
    
    public string goalName;
    public int priority = 1;
    
    [Tooltip("These are the world states that must be achieved to satisfy this goal")]
    public List<GoalState> desiredStates = new List<GoalState>();
    
    public GoapGoal CreateGoal()
    {
        // For backward compatibility, use the first desired state as the main goal key
        string mainKey = desiredStates.Count > 0 ? desiredStates[0].key : goalName;
        return new GoapGoal(mainKey, priority);
    }
    
    public Dictionary<string, bool> GetDesiredStates()
    {
        Dictionary<string, bool> states = new Dictionary<string, bool>();
        foreach (var state in desiredStates)
        {
            states[state.key] = state.value;
        }
        return states;
    }
    
    // Optional: Set a default name for new instances
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(goalName))
        {
            // Extract name from asset name if possible
            string assetName = name;
            if (assetName.EndsWith("GoalDefinition"))
            {
                assetName = assetName.Substring(0, assetName.Length - "GoalDefinition".Length);
            }
            
            goalName = assetName;
        }
    }
}

// Custom attribute for the dropdown - this can stay here since it's not editor-specific
public class StateKeyDropdownAttribute : PropertyAttribute
{
}