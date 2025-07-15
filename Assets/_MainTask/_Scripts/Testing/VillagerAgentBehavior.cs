using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVillagerAgent : MonoBehaviour, IGoapAgent
{
    [Header("Agent Settings")]
    public string agentName = "TestVillager";
    public float actionDuration = 2f;
    
    private IGoapAction currentAction;
    private bool isActionComplete;
    private bool isExecuting;
    
    public string GetName()
    {
        return agentName;
    }
    
    public bool IsAvailable()
    {
        return currentAction == null && !isExecuting;
    }
    
    public bool CanPerformAction(IGoapAction action)
    {
        // This villager can perform work actions
        return action.GetName().Contains("Work") || action.GetName().Contains("Gather");
    }
    
    public void AssignAction(IGoapAction action)
    {
        if (IsAvailable())
        {
            currentAction = action;
            isActionComplete = false;
            StartCoroutine(ExecuteAction());
        }
    }
    
    public bool IsActionComplete()
    {
        return isActionComplete;
    }
    
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public List<IGoapAction> GetAvailableActions()
    {
        var actions = new List<IGoapAction>();
        
        // Create a simple work action
        var workAction = new GoapAction("VillagerWork", 1f);
        workAction.AddPrecondition("AvailableAgents", 1);
        workAction.AddEffect("WorkDone", true);
        actions.Add(workAction);
        
        // Create a gather action
        var gatherAction = new GoapAction("GatherResources", 2f);
        gatherAction.AddPrecondition("AvailableAgents", 1);
        gatherAction.AddEffect("ResourcesGathered", true);
        actions.Add(gatherAction);
        
        return actions;
    }
    
    private IEnumerator ExecuteAction()
    {
        isExecuting = true;
        Debug.Log($"[{agentName}] STARTED: {currentAction.GetName()}");
        
        // Simulate action execution
        yield return new WaitForSeconds(actionDuration);
        
        Debug.Log($"[{agentName}] COMPLETED: {currentAction.GetName()}");
        isActionComplete = true;
        isExecuting = false;
        
        // Clear action after a short delay
        yield return new WaitForSeconds(0.1f);
        currentAction = null;
        isActionComplete = false;
    }
}