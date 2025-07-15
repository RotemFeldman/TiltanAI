using System.Collections.Generic;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class VillagerCollectIronIngotAction : GoapAction
{
    private VillagerAgent targetAgent;
    private bool actionAssigned = false;
    
    public VillagerCollectIronIngotAction() : base("CollectIronIngot", 2f)
    {
        
        // Preconditions: Need an available villager and iron ingot must be available
        AddPrecondition("AvailableVillagers", 1);
        AddPrecondition("IronIngotAvailable", true);
        
        // Effects: After collecting, we have iron ingot
        AddEffect("IronIngotCollected", true);
        AddEffect("HasIronIngot", true);
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
            targetAgent.SetCurrentAction(VillagerActions.CollectIronIngot);
            actionAssigned = true;
            
            Debug.Log($"[CollectIronIngot] Assigned CollectIronIngot action to {targetAgent.GetName()}");
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
