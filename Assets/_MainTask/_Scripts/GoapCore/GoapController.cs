using System;
using System.Collections.Generic;
using System.Linq;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class GoapController : MonoBehaviour
{
    public static GoapController Instance;
    
    [Header("GOAP Settings")]
    public float planningInterval = 1f;
    public bool debugMode = true;
    
    private IGoapPlanner planner;
    private GoapWorldStateManager stateManager;
    private List<IGoapGoal> goals;
    private List<IGoapAgent> agents;
    private Dictionary<IGoapAgent, IGoapAction> agentActions;
    private List<IGoapAction> currentPlan;
    private IGoapGoal currentGoal;
    private float lastPlanTime;
    private float lastLogTime;
    
    public void RegisterItemLocation(ItemType itemType, Vector3 position)
    {
        stateManager.RegisterItemLocation(itemType, position);
    }

    public void ReserveItemLocation(ItemType itemType, Vector3 position, string agentName)
    {
        stateManager.ReserveItemLocation(itemType, position, agentName);
    }

    public void RemoveItemLocation(ItemType itemType, Vector3 position)
    {
        stateManager.RemoveItemLocation(itemType, position);
    }

    public List<ItemLocation> GetAvailableItemLocations(ItemType itemType)
    {
        return stateManager.GetAvailableItemLocations(itemType);
    }
    
    public void CollectItems(ItemType itemType, int amount = 1)
    {
        stateManager.CollectItems(itemType, amount);
    }
    
    public void RemoveItems(ItemType itemType, int amount = 1)
    {
        stateManager.RemoveItems(itemType, amount);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        InitializeGoap();
    }

    void Update()
    {
        UpdateWorldState();
        
        // Process completed actions
        ProcessCompletedActions();
        
        // Execute current plan
        ExecutePlan();
        
        // Create new plan if needed
        if ((currentPlan == null || currentPlan.Count == 0) && Time.time - lastPlanTime > planningInterval)
        {
            CreateNewPlan();
            lastPlanTime = Time.time;
        }
    }
    
    void InitializeGoap()
    {
        planner = new GoapPlanner();
        stateManager = new GoapWorldStateManager();
        goals = new List<IGoapGoal>();
        agents = new List<IGoapAgent>();
        agentActions = new Dictionary<IGoapAgent, IGoapAction>();
        
        // Find all agents in the scene
        var goapAgents = FindObjectsOfType<MonoBehaviour>().OfType<IGoapAgent>();
        agents.AddRange(goapAgents);
        
        Debug.Log($"[GOAP] Initialized - Found {agents.Count} agents");
    }
    
    void UpdateWorldState()
    {
        if (stateManager == null) return;
        
        int availableVillagers = agents.Count(a => a.IsAvailable() && !agentActions.ContainsKey(a) && a is TestVillagerAgent);
        int availableMessengers = agents.Count(a => a.IsAvailable() && !agentActions.ContainsKey(a) && a is TestMessengerAgent);
        int availableMages = agents.Count(a => a.IsAvailable() && !agentActions.ContainsKey(a) && a is TestMageAgent);
    
        // Update agent availability through state manager
        stateManager.UpdateAgentAvailability(availableVillagers, availableMessengers, availableMages);
    }
    
    void UpdateAgentActions()
    {
        var completedAgents = new List<IGoapAgent>();
        
        foreach (var kvp in agentActions)
        {
            var agent = kvp.Key;
            var action = kvp.Value;
            
            if (agent.IsActionComplete())
            {
                completedAgents.Add(agent);
                // Action completion is now logged by the agent itself
            }
        }
        
        // Remove completed actions
        foreach (var agent in completedAgents)
        {
            agentActions.Remove(agent);
        }
        
        // Assign new actions
        AssignActionsToAgents();
    }
    
    void AssignActionsToAgents()
    {
        if (currentPlan == null || currentPlan.Count == 0)
            return;
        
        var worldState = stateManager.GetWorldState();
        var availableAgents = agents.Where(a => a.IsAvailable() && !agentActions.ContainsKey(a)).ToList();
        var unassignedActions = currentPlan.Where(a => !agentActions.ContainsValue(a) && a.CanExecute(worldState)).ToList();
        
        foreach (var action in unassignedActions)
        {
            var suitableAgent = availableAgents.FirstOrDefault(a => a.CanPerformAction(action));
            if (suitableAgent != null)
            {
                agentActions[suitableAgent] = action;
                suitableAgent.AssignAction(action);
                availableAgents.Remove(suitableAgent);
                
                // Action start is now logged by the agent itself
            }
        }
    }
    
    void ExecutePlanning()
    {
        var worldState = stateManager.GetWorldState();
        if (currentGoal == null || currentGoal.IsAchieved(worldState))
        {
            Debug.Log("[GOAP] Goal achieved!");
            return;
        }
        
        var availableActions = GetAllAvailableActions();
        currentPlan = planner.CreatePlan(worldState, currentGoal, availableActions);
        
        if (currentPlan != null && currentPlan.Count > 0)
        {
            Debug.Log($"[GOAP] New plan created with {currentPlan.Count} actions:");
            for (int i = 0; i < currentPlan.Count; i++)
            {
                Debug.Log($"  {i + 1}. {currentPlan[i].GetName()}");
            }
        }
        else
        {
            Debug.LogWarning("[GOAP] No valid plan found!");
        }
    }
    
    List<IGoapAction> GetAllAvailableActions()
    {
        var allActions = new List<IGoapAction>();
        
        foreach (var agent in agents)
        {
            allActions.AddRange(agent.GetAvailableActions());
        }
        
        return allActions;
    }
    
    void LogGoapState()
    {
        Debug.Log($"[GOAP] Status - Active: {agents.Count}, Available: {agents.Count(a => a.IsAvailable())}, Assigned: {agentActions.Count}, Plan: {currentPlan?.Count ?? 0} actions");
    }

    void ProcessCompletedActions()
    {
        var completedAgents = new List<IGoapAgent>();
        
        foreach (var agentAction in agentActions)
        {
            if (agentAction.Key.IsActionComplete())
            {
                Debug.Log($"[GOAP] {agentAction.Key.GetName()} completed {agentAction.Value.GetName()}");
                completedAgents.Add(agentAction.Key);
            }
        }
        
        // Remove completed actions
        foreach (var agent in completedAgents)
        {
            agentActions.Remove(agent);
        }
    }

    void ExecutePlan()
    {
        if (currentPlan == null || currentPlan.Count == 0) return;
        
        // Try to assign actions to available agents
        for (int i = currentPlan.Count - 1; i >= 0; i--)
        {
            var action = currentPlan[i];
            var availableAgent = FindAvailableAgentForAction(action);
            
            if (availableAgent != null)
            {
                // Set the target agent for actions that need it
                if (action is VillagerSearchForResourcesAction searchAction)
                {
                    searchAction.SetTargetAgent(availableAgent as VillagerAgent);
                }
                else if (action is VillagerChopTreeAction chopAction)
                {
                    chopAction.SetTargetAgent(availableAgent as VillagerAgent);
                }
                // Add other action types as needed...
                
                // Assign the action
                availableAgent.AssignAction(action);
                agentActions[availableAgent] = action;
                
                // Remove from plan
                currentPlan.RemoveAt(i);
                
                Debug.Log($"[GOAP] Assigned {action.GetName()} to {availableAgent.GetName()}");
            }
        }
    }

    private IGoapAgent FindAvailableAgentForAction(IGoapAction action)
    {
        foreach (var agent in agents)
        {
            // Check if agent is available AND can perform the action
            if (agent.IsAvailable() && agent.CanPerformAction(action))
            {
                return agent;
            }
        }
        return null; 
    }
    
    private void CreateNewPlan()
    {
        if (goals.Count == 0) return;
        
        var worldState = stateManager.GetWorldState();
    
        // Find the highest priority goal that isn't achieved
        IGoapGoal targetGoal = null;
        float highestPriority = float.MinValue;
    
        foreach (var goal in goals)
        {
            if (!goal.IsAchieved(worldState) && goal.GetPriority() > highestPriority)
            {
                targetGoal = goal;
                highestPriority = goal.GetPriority();
            }
        }
    
        if (targetGoal == null)
        {
            Debug.Log("[GOAP] No goals to achieve");
            return;
        }
    
        // Get all available actions from all agents
        var availableActions = new List<IGoapAction>();
        foreach (var agent in agents)
        {
            if (agent.IsAvailable())
            {
                availableActions.AddRange(agent.GetAvailableActions());
            }
        }
    
        // Create a plan using the planner
        currentPlan = planner.CreatePlan(worldState, targetGoal, availableActions);
    
        if (currentPlan != null && currentPlan.Count > 0)
        {
            Debug.Log($"[GOAP] Created plan for goal '{targetGoal.GetName()}' with {currentPlan.Count} actions:");
            for (int i = 0; i < currentPlan.Count; i++)
            {
                Debug.Log($"[GOAP]   {i + 1}. {currentPlan[i].GetName()}");
            }
        }
        else
        {
            Debug.Log($"[GOAP] Failed to create plan for goal '{targetGoal.GetName()}'");
        }
    }
}