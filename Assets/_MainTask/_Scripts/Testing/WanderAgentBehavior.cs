using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WanderAgent : MonoBehaviour, IGoapAgent
{
    [Header("Agent Settings")]
    public string agentName = "WanderAgent";
    public float wanderRadius = 15f;
    
    private NavMeshAgent navAgent;
    private IGoapAction currentAction;
    private NewWanderAction wanderAction;
    private bool isActionComplete;
    
    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        wanderAction = new NewWanderAction(navAgent, wanderRadius);
    }
    
    public string GetName()
    {
        return agentName;
    }
    
    public bool IsAvailable()
    {
        return currentAction == null;
    }
    
    public bool CanPerformAction(IGoapAction action)
    {
        // This agent can perform wander and movement actions
        return action.GetName().Contains("Wander") || action.GetName().Contains("Move");
    }
    
    public void AssignAction(IGoapAction action)
    {
        if (IsAvailable())
        {
            currentAction = action;
            isActionComplete = false;
            
            Debug.Log($"[{agentName}] STARTED: {action.GetName()}");
            
            // Execute the action
            action.Execute(new GoapState());
        }
    }
    
    public bool IsActionComplete()
    {
        if (currentAction == null) return false;
        
        // Check if it's a WanderAction and if it's complete
        if (currentAction is NewWanderAction wanderAction)
        {
            if (wanderAction.IsComplete())
            {
                Debug.Log($"[{agentName}] COMPLETED: {currentAction.GetName()}");
                wanderAction.Reset();
                currentAction = null;
                return true;
            }
        }
        else
        {
            // For other actions, use a simple timer or immediate completion
            if (!isActionComplete)
            {
                StartCoroutine(CompleteActionAfterDelay(2f));
            }
            return isActionComplete;
        }
        
        return false;
    }
    
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public List<IGoapAction> GetAvailableActions()
    {
        var actions = new List<IGoapAction>();
        
        // Add the wander action
        actions.Add(wanderAction);
        
        // Add a simple idle action as alternative
        var idleAction = new GoapAction("Idle", 0.1f);
        idleAction.AddPrecondition("AvailableAgents", 1);
        idleAction.AddEffect("IsIdle", true);
        actions.Add(idleAction);
        
        return actions;
    }
    
    private IEnumerator CompleteActionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[{agentName}] COMPLETED: {currentAction.GetName()}");
        isActionComplete = true;
        
        // Clear action after a short delay
        yield return new WaitForSeconds(0.1f);
        currentAction = null;
        isActionComplete = false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw wander radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        
        // Draw current destination if moving
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(navAgent.destination, 0.5f);
            
            // Draw path
            Gizmos.color = Color.blue;
            var path = navAgent.path;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
            }
        }
    }
}