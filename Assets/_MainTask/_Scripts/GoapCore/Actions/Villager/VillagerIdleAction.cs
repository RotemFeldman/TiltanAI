using System.Collections.Generic;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class VillagerIdleAction : GoapAction
{
    private VillagerAgent targetAgent;
    private bool actionAssigned = false;
    
    public VillagerIdleAction() : base("Idle", 0.1f)
    {
        // Preconditions: Need an available villager
        AddPrecondition("AvailableVillagers", 1);
        
        // Effects: Agent is idle (fallback action)
        AddEffect("AgentIdle", true);
    }
    
    public void SetTargetAgent(VillagerAgent availableAgent)
    {
        targetAgent = availableAgent;
    }
    
    public override bool CanExecute(IGoapState worldState)
    {
        return !actionAssigned && base.CanExecute(worldState);
    }
    
    public override IGoapState Execute(IGoapState worldState)
    {
        if (targetAgent != null && !actionAssigned)
        {
            targetAgent.SetCurrentAction(AgentActions.Idle);
            actionAssigned = true;
            
            Debug.Log($"[Idle] Assigned Idle action to {targetAgent.GetName()}");
        }
        
        return base.Execute(worldState);
    }
    
    public bool IsActionAssigned()
    {
        return actionAssigned;
    }
    
    public bool IsComplete()
    {
        if (targetAgent == null) return false;
        return targetAgent.HasCompletedCurrentAction();
    }
    
    public void Reset()
    {
        actionAssigned = false;
        targetAgent = null;
    }
}
