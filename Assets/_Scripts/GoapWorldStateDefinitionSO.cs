using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldStateDefinition", menuName = "GOAP/World State Definition")]
public class GoapWorldStateDefinitionSO : ScriptableObject
{
    [System.Serializable]
    public class WorldStateEntry
    {
        public string key;
        public bool defaultValue;
    }
    
    public List<WorldStateEntry> stateDefinitions = new List<WorldStateEntry>();
    
    public Dictionary<string, bool> CreateInitialState()
    {
        Dictionary<string, bool> initialState = new Dictionary<string, bool>();
        foreach (var entry in stateDefinitions)
        {
            initialState[entry.key] = entry.defaultValue;
        }
        return initialState;
    }
    
    // Get all state keys defined in this definition
    public List<string> GetAllStateKeys()
    {
        List<string> keys = new List<string>();
        foreach (var entry in stateDefinitions)
        {
            keys.Add(entry.key);
        }
        return keys;
    }
    
    // Static method to get all possible world state keys from all definitions
    public static List<string> GetAllAvailableWorldStateKeys()
    {
        HashSet<string> allKeys = new HashSet<string>();
        
        // Find all world state definition assets in the project
        GoapWorldStateDefinitionSO[] allWorldStateDefs = Resources.FindObjectsOfTypeAll<GoapWorldStateDefinitionSO>();
        
        foreach (var def in allWorldStateDefs)
        {
            foreach (var entry in def.stateDefinitions)
            {
                allKeys.Add(entry.key);
            }
        }
        
        return new List<string>(allKeys);
    }
}