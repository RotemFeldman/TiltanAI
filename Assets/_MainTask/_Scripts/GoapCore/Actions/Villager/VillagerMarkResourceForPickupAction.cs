using System.Collections.Generic;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class VillagerMarkResourceForPickupAction : GoapAction
{
    private VillagerAgent targetAgent;
    private bool actionAssigned = false;
    
    public VillagerMarkResourceForPickupAction() : base("MarkResourceForPickup", 1f)
    {
        // Preconditions: Need an available villager and resources must be processed
        AddPrecondition("AvailableVillagers", 1);
        AddPrecondition("ResourcesProcessed", true);
        
        // Effects: After marking, resources are ready for pickup
        AddEffect("ResourcesMarkedForPickup", true);
        AddEffect("ReadyForTransport", true);
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
            targetAgent.SetCurrentAction(AgentActions.MarkResourceForPickup);
            actionAssigned = true;
            
            Debug.Log($"[MarkResourceForPickup] Assigned MarkResourceForPickup action to {targetAgent.GetName()}");
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
