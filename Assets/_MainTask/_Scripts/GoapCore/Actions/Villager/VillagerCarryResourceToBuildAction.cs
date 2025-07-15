using System.Collections.Generic;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class VillagerCarryResourceToBuildAction : GoapAction
{
    private VillagerAgent targetAgent;
    private bool actionAssigned = false;
    
    public VillagerCarryResourceToBuildAction() : base("CarryResourceToBuild", 8f)
    {
        
        // Preconditions: Need an available villager and resources ready for transport
        AddPrecondition("AvailableVillagers", 1);
        AddPrecondition("ReadyForTransport", true);
        AddPrecondition("BuildSiteAvailable", true);
        
        // Effects: After carrying, resources are delivered to build site
        AddEffect("ResourcesDelivered", true);
        AddEffect("BuildSiteSupplied", true);
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
            targetAgent.SetCurrentAction(VillagerActions.CarryResourceToBuild);
            actionAssigned = true;
            
            Debug.Log($"[CarryResourceToBuild] Assigned CarryResourceToBuild action to {targetAgent.GetName()}");
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
