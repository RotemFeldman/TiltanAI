using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(GoapGoalDefinitionSO))]
public class GoapGoalDefinitionSOEditor : Editor
{
    private SerializedProperty goalNameProperty;
    private SerializedProperty priorityProperty;
    private SerializedProperty desiredStatesProperty;
    
    // Cache for dropdown options
    private List<string> cachedWorldStateKeys = new List<string>();
    private GUIContent[] worldStateKeyOptions;
    
    private void OnEnable()
    {
        goalNameProperty = serializedObject.FindProperty("goalName");
        priorityProperty = serializedObject.FindProperty("priority");
        desiredStatesProperty = serializedObject.FindProperty("desiredStates");
        
        RefreshWorldStateKeys();
    }
    
    private void RefreshWorldStateKeys()
    {
        // Implementation as before...
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Store the old goal name before modification
        string oldGoalName = goalNameProperty.stringValue;
        
        // Basic properties
        EditorGUILayout.PropertyField(goalNameProperty);
        EditorGUILayout.PropertyField(priorityProperty);
        EditorGUILayout.PropertyField(desiredStatesProperty);
        
        // Check if goal name changed
        if (oldGoalName != goalNameProperty.stringValue)
        {
            RenameAssetToMatchGoalName();
        }
        
        // Rest of the inspector implementation as before...
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void RenameAssetToMatchGoalName()
    {
        GoapGoalDefinitionSO goalDefinition = (GoapGoalDefinitionSO)target;
        string newName = goalNameProperty.stringValue;
        
        if (string.IsNullOrEmpty(newName))
            return;
            
        // Add "GoalDefinition" suffix if not already present
        if (!newName.EndsWith("GoalDefinition"))
            newName += " GoalDefinition";
            
        // Get the asset path
        string assetPath = AssetDatabase.GetAssetPath(goalDefinition);
        
        if (!string.IsNullOrEmpty(assetPath))
        {
            // Rename the asset
            AssetDatabase.RenameAsset(assetPath, newName);
            AssetDatabase.SaveAssets();
        }
    }
    
    // Rest of the implementation as before...
}
#endif