using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMessengerAgent : MonoBehaviour, IGoapAgent
{
    [Header("Agent Settings")]
    public string agentName = "TestMessenger";
    public float actionDuration = 1.5f;
    
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
        // This messenger can perform delivery and movement actions
        return action.GetName().Contains("Deliver") || action.GetName().Contains("Move") || action.GetName().Contains("Fly");
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
        
        // Create a delivery action
        var deliverAction = new GoapAction("DeliverItems", 1f);
        deliverAction.AddPrecondition("AvailableAgents", 1);
        deliverAction.AddPrecondition("ResourcesGathered", true);
        deliverAction.AddEffect("ItemsDelivered", true);
        actions.Add(deliverAction);
        
        // Create a move action
        var moveAction = new GoapAction("MoveToLocation", 0.5f);
        moveAction.AddPrecondition("AvailableAgents", 1);
        moveAction.AddEffect("AtLocation", true);
        actions.Add(moveAction);
        
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