#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using GOAP;

[CustomEditor(typeof(GoapAgentViewer))]
public class GoapAgentViewerEditor : Editor
{
    private GoapAgent goapAgent;
    private bool showBeliefs = true;
    private bool showActions = true;
    private bool showGoals = true;
    private bool showPlanStack = true;

    private void OnEnable()
    {
        goapAgent = ((GoapAgentViewer)target).GetComponent<GoapAgent>();
    }

    public override void OnInspectorGUI()
    {
        if (goapAgent == null)
        {
            EditorGUILayout.HelpBox("No GoapAgent component found on this GameObject!", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("GOAP Agent Debug Viewer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Current Goal Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Current Goal", EditorStyles.boldLabel);
        if (goapAgent.currentGoal != null)
        {
            EditorGUILayout.LabelField("Name:", goapAgent.currentGoal.Name);
            EditorGUILayout.LabelField("Priority:", goapAgent.currentGoal.Priority.ToString());
        }
        else
        {
            EditorGUILayout.LabelField("No current goal");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Current Action Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Current Action", EditorStyles.boldLabel);
        if (goapAgent.currentAction != null)
        {
            EditorGUILayout.LabelField("Name:", goapAgent.currentAction.Name);
            EditorGUILayout.LabelField("Cost:", goapAgent.currentAction.Cost.ToString());
            EditorGUILayout.LabelField("Complete:", goapAgent.currentAction.Complete.ToString());
        }
        else
        {
            EditorGUILayout.LabelField("No current action");
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Plan Stack Section
        showPlanStack = EditorGUILayout.Foldout(showPlanStack, "Action Plan Stack");
        if (showPlanStack)
        {
            EditorGUILayout.BeginVertical("box");
            if (goapAgent.actionPlan?.Actions != null && goapAgent.actionPlan.Actions.Count > 0)
            {
                EditorGUILayout.LabelField($"Total Cost: {goapAgent.actionPlan.TotalCost}");
                EditorGUILayout.LabelField($"Actions Remaining: {goapAgent.actionPlan.Actions.Count}");
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Upcoming Actions:", EditorStyles.miniBoldLabel);
                
                var actionArray = goapAgent.actionPlan.Actions.ToArray();
                for (int i = actionArray.Length - 1; i >= 0; i--)
                {
                    var action = actionArray[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{actionArray.Length - i}.", GUILayout.Width(20));
                    EditorGUILayout.LabelField(action.Name);
                    EditorGUILayout.LabelField($"Cost: {action.Cost}", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No action plan");
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        // Beliefs Section
        showBeliefs = EditorGUILayout.Foldout(showBeliefs, "Beliefs");
        if (showBeliefs)
        {
            EditorGUILayout.BeginVertical("box");
            if (goapAgent.beliefs != null && goapAgent.beliefs.Count > 0)
            {
                foreach (var belief in goapAgent.beliefs)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isTrue = belief.Value.Evaluate();
                    var color = isTrue ? Color.green : Color.red;
                    var prevColor = GUI.color;
                    GUI.color = color;
                    
                    EditorGUILayout.LabelField("●", GUILayout.Width(15));
                    GUI.color = prevColor;
                    
                    EditorGUILayout.LabelField(belief.Key);
                    EditorGUILayout.LabelField(isTrue ? "TRUE" : "FALSE", GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No beliefs initialized");
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        // Available Goals Section
        showGoals = EditorGUILayout.Foldout(showGoals, "Available Goals");
        if (showGoals)
        {
            EditorGUILayout.BeginVertical("box");
            if (goapAgent.goals != null && goapAgent.goals.Count > 0)
            {
                var sortedGoals = goapAgent.goals.OrderByDescending(g => g.Priority);
                foreach (var goal in sortedGoals)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isCurrent = goapAgent.currentGoal == goal;
                    if (isCurrent)
                    {
                        var prevColor = GUI.color;
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField("►", GUILayout.Width(15));
                        GUI.color = prevColor;
                    }
                    else
                    {
                        EditorGUILayout.LabelField("", GUILayout.Width(15));
                    }
                    
                    EditorGUILayout.LabelField(goal.Name);
                    EditorGUILayout.LabelField($"Priority: {goal.Priority}", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No goals initialized");
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        // Available Actions Section
        showActions = EditorGUILayout.Foldout(showActions, "Available Actions");
        if (showActions)
        {
            EditorGUILayout.BeginVertical("box");
            if (goapAgent.actions != null && goapAgent.actions.Count > 0)
            {
                var sortedActions = goapAgent.actions.OrderBy(a => a.Cost);
                foreach (var action in sortedActions)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isCurrent = goapAgent.currentAction == action;
                    if (isCurrent)
                    {
                        var prevColor = GUI.color;
                        GUI.color = Color.cyan;
                        EditorGUILayout.LabelField("►", GUILayout.Width(15));
                        GUI.color = prevColor;
                    }
                    else
                    {
                        EditorGUILayout.LabelField("", GUILayout.Width(15));
                    }
                    
                    EditorGUILayout.LabelField(action.Name);
                    EditorGUILayout.LabelField($"Cost: {action.Cost}", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No actions initialized");
            }
            EditorGUILayout.EndVertical();
        }

        // Auto-refresh during play mode
        if (Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
            Repaint();
        }
    }
}
#endif
