using System.Collections.Generic;
using UnityEngine;

public class GoapAgent : MonoBehaviour
{
    public GoapAgentConfigSO agentConfig;
    
    [Header("Runtime Data - For Debugging")]
    public List<GoapAction> actions = new List<GoapAction>();
    public List<GoapGoal> goals = new List<GoapGoal>();
    public Dictionary<string, bool> worldState = new Dictionary<string, bool>();

    private Queue<GoapAction> currentPlan = new Queue<GoapAction>();
    private GoapPlanner planner;

    void Awake()
    {
        planner = new GoapPlanner();
    }

    void Start()
    {
        InitializeFromConfig();
    }

    public void InitializeFromConfig()
    {
        if (agentConfig == null)
        {
            Debug.LogError("GoapAgent is missing agent configuration!");
            return;
        }
        
        // Initialize world state
        worldState = agentConfig.initialWorldState.CreateInitialState();
        
        // Initialize actions
        actions.Clear();
        foreach (var actionDef in agentConfig.availableActions)
        {
            actions.Add(actionDef.CreateAction());
        }
        
        // Initialize goals
        goals.Clear();
        foreach (var goalDef in agentConfig.possibleGoals)
        {
            goals.Add(goalDef.CreateGoal());
        }
    }

    void Update()
    {
        if (currentPlan.Count == 0)
        {
            PlanActions();
        }

        if (currentPlan.Count > 0)
        {
            var action = currentPlan.Peek();
            if (action.IsDone())
            {
                // Apply effects to world state
                foreach (var effect in action.Effects)
                {
                    worldState[effect.Key] = effect.Value;
                }
                
                currentPlan.Dequeue();
                Debug.Log($"Action complete. Remaining actions: {currentPlan.Count}");
            }
            else
            {
                action.Perform();
            }
        }
    }

    void PlanActions()
    {
        GoapGoal bestGoal = planner.SelectGoal(goals, worldState);
        
        if (bestGoal != null)
        {
            Debug.Log($"Selected goal: {bestGoal.goalKey} with priority {bestGoal.priority}");
            currentPlan = planner.Plan(actions, bestGoal, new Dictionary<string, bool>(worldState));
            
            if (currentPlan.Count == 0)
            {
                Debug.LogWarning($"Could not find plan for goal {bestGoal.goalKey}");
            }
            else
            {
                Debug.Log($"Created plan with {currentPlan.Count} actions");
            }
        }
        else
        {
            Debug.Log("No valid goal found");
        }
    }
}