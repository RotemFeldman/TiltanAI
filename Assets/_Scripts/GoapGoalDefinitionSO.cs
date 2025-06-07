using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGoalDefinition", menuName = "GOAP/Goal Definition")]
public class GoapGoalDefinitionSO : ScriptableObject
{
    [System.Serializable]
    public class GoalState
    {
        public string key;
        public bool value;
    }
    
    public string goalName;
    public int priority = 1;
    
    [Tooltip("These are the world states that must be achieved to satisfy this goal")]
    public List<GoalState> desiredStates = new List<GoalState>();
    
    public GoapGoal CreateGoal()
    {
        // For backward compatibility, if no desired states, use the goal name as the key
        if (desiredStates.Count == 0)
        {
            return new GoapGoal(goalName, priority);
        }
        
        // Use the first desired state as the main goal key for identification
        string mainKey = desiredStates[0].key;
        
        // Create a dictionary of all desired states
        Dictionary<string, bool> goalConditions = new Dictionary<string, bool>();
        foreach (var state in desiredStates)
        {
            goalConditions[state.key] = state.value;
        }
        
        return new GoapGoal(mainKey, priority, goalConditions);
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