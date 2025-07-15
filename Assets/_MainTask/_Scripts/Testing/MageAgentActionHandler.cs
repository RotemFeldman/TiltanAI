using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMageAgent : MonoBehaviour, IGoapAgent
{
    [Header("Agent Settings")]
    public string agentName = "TestMage";
    public float actionDuration = 3f;
    
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
        // This mage can perform crafting and magical actions
        return action.GetName().Contains("Craft") || action.GetName().Contains("Build") || action.GetName().Contains("Magic") || action.GetName().Contains("Complete");
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
        
        // Create a craft action
        var craftAction = new GoapAction("CraftArtifact", 3f);
        craftAction.AddPrecondition("AvailableAgents", 1);
        craftAction.AddPrecondition("ItemsDelivered", true);
        craftAction.AddEffect("ArtifactCrafted", true);
        actions.Add(craftAction);
        
        // Create a final completion action
        var completeAction = new GoapAction("CompleteTest", 1f);
        completeAction.AddPrecondition("ArtifactCrafted", true);
        completeAction.AddEffect("TestCompleted", true);
        actions.Add(completeAction);
        
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