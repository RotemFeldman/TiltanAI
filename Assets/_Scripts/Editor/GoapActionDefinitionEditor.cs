using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GoapActionDefinitionSO))]
public class GoapActionDefinitionSOEditor : Editor
{
    private SerializedProperty actionNameProperty;
    private SerializedProperty preconditionsProperty;
    private SerializedProperty effectsProperty;
    
    private List<string> cachedWorldStateKeys = new List<string>();
    private GUIContent[] worldStateKeyOptions;
    
    private void OnEnable()
    {
        actionNameProperty = serializedObject.FindProperty("actionName");
        preconditionsProperty = serializedObject.FindProperty("preconditions");
        effectsProperty = serializedObject.FindProperty("effects");
        
        RefreshWorldStateKeys();
    }
    
    private void RefreshWorldStateKeys()
    {
        // Find all world state definitions in the project
        string[] guids = AssetDatabase.FindAssets("t:GoapWorldStateDefinitionSO");
        
        cachedWorldStateKeys.Clear();
        cachedWorldStateKeys.Add("[Custom]"); // Always add custom option
        
        HashSet<string> uniqueKeys = new HashSet<string>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoapWorldStateDefinitionSO worldStateDef = AssetDatabase.LoadAssetAtPath<GoapWorldStateDefinitionSO>(path);
            
            if (worldStateDef != null)
            {
                foreach (var state in worldStateDef.stateDefinitions)
                {
                    if (!string.IsNullOrEmpty(state.key) && !uniqueKeys.Contains(state.key))
                    {
                        uniqueKeys.Add(state.key);
                        cachedWorldStateKeys.Add(state.key);
                    }
                }
            }
        }
        
        // Create display options for the dropdown
        worldStateKeyOptions = new GUIContent[cachedWorldStateKeys.Count];
        for (int i = 0; i < cachedWorldStateKeys.Count; i++)
        {
            worldStateKeyOptions[i] = new GUIContent(cachedWorldStateKeys[i]);
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Store the old action name before modification
        string oldActionName = actionNameProperty.stringValue;
        
        // Draw the action name field
        EditorGUILayout.PropertyField(actionNameProperty);
        
        // Button to refresh world state keys
        if (GUILayout.Button("Refresh Available World States"))
        {
            RefreshWorldStateKeys();
        }
        
        // Preconditions section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preconditions", EditorStyles.boldLabel);
        
        DrawWorldStateList(preconditionsProperty);
        
        if (GUILayout.Button("Add Precondition"))
        {
            AddWorldStateEntry(preconditionsProperty);
        }
        
        // Effects section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        
        DrawWorldStateList(effectsProperty);
        
        if (GUILayout.Button("Add Effect"))
        {
            AddWorldStateEntry(effectsProperty);
        }
        
        // Check if action name changed
        if (oldActionName != actionNameProperty.stringValue)
        {
            RenameAssetToMatchActionName();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawWorldStateList(SerializedProperty listProperty)
    {
        EditorGUI.indentLevel++;
        
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty entryProp = listProperty.GetArrayElementAtIndex(i);
            SerializedProperty keyProp = entryProp.FindPropertyRelative("key");
            SerializedProperty valueProp = entryProp.FindPropertyRelative("value");
            
            EditorGUILayout.BeginHorizontal();
            
            // Find the current index in our cached keys
            int currentIndex = 0;
            for (int j = 0; j < cachedWorldStateKeys.Count; j++)
            {
                if (cachedWorldStateKeys[j] == keyProp.stringValue)
                {
                    currentIndex = j;
                    break;
                }
            }
            
            // State key dropdown
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(
                new GUIContent("State"), 
                currentIndex, 
                worldStateKeyOptions, 
                GUILayout.MinWidth(150)
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0) // [Custom] selected
                {
                    // Show input dialog for custom key
                    string result = EditorInputDialog("Custom State Key", "Enter key name:", keyProp.stringValue);
                    if (!string.IsNullOrEmpty(result))
                    {
                        keyProp.stringValue = result;
                    }
                }
                else
                {
                    keyProp.stringValue = cachedWorldStateKeys[newIndex];
                }
            }
            
            // Boolean value - explicit label and more space
            EditorGUILayout.LabelField("Value:", GUILayout.Width(55));
            valueProp.boolValue = EditorGUILayout.Toggle(
                valueProp.boolValue, 
                GUILayout.Width(30)
            );
            
            // Delete button
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                listProperty.DeleteArrayElementAtIndex(i);
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUI.indentLevel--;
    }
    
    private void AddWorldStateEntry(SerializedProperty listProperty)
    {
        int index = listProperty.arraySize;
        listProperty.arraySize++;
        
        SerializedProperty newEntry = listProperty.GetArrayElementAtIndex(index);
        newEntry.FindPropertyRelative("key").stringValue = "";
        newEntry.FindPropertyRelative("value").boolValue = true;
    }
    
    private void RenameAssetToMatchActionName()
    {
        GoapActionDefinitionSO actionDefinition = (GoapActionDefinitionSO)target;
        string newName = actionNameProperty.stringValue;
        
        if (string.IsNullOrEmpty(newName))
            return;
            
        // Add "Action" suffix if not already present
        if (!newName.EndsWith("Action"))
            newName += "Action";
            
        // Get the asset path
        string assetPath = AssetDatabase.GetAssetPath(actionDefinition);
        
        if (!string.IsNullOrEmpty(assetPath))
        {
            // Rename the asset
            AssetDatabase.RenameAsset(assetPath, newName);
            AssetDatabase.SaveAssets();
        }
    }
    
    // Simple input dialog for Unity Editor
    private string EditorInputDialog(string title, string message, string defaultValue)
    {
        string result = defaultValue;
        
        // Create window instance
        InputDialogWindow window = EditorWindow.GetWindow<InputDialogWindow>(true, title, true);
        window.Initialize(message, defaultValue, (value) => { result = value; });
        window.position = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 50, 300, 100);
        window.ShowModal();
        
        return result;
    }
}

// Helper class for input dialog
public class InputDialogWindow : EditorWindow
{
    private string message;
    private string inputValue;
    private System.Action<string> callback;
    private bool cancelled;
    
    public void Initialize(string message, string defaultValue, System.Action<string> callback)
    {
        this.message = message;
        this.inputValue = defaultValue;
        this.callback = callback;
        this.cancelled = false;
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField(message);
        inputValue = EditorGUILayout.TextField(inputValue);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("OK"))
        {
            callback?.Invoke(inputValue);
            Close();
        }
        
        if (GUILayout.Button("Cancel"))
        {
            cancelled = true;
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void OnDestroy()
    {
        if (!cancelled)
        {
            callback?.Invoke(inputValue);
        }
    }
}