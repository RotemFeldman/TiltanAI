using System.Collections.Generic;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class VillagerChopTreeAction : GoapAction
{
    private VillagerAgent targetAgent;
    private bool actionAssigned = false;
    
    public VillagerChopTreeAction() : base("ChopTree", 3f)
    {
        // Preconditions: Need an available villager and resources must be located
        AddPrecondition("AvailableVillagers", 1);
        AddPrecondition("ResourcesLocated", true);
        AddPrecondition("TreeAvailable", true);
        
        // Effects: After chopping, we have wood resources
        AddEffect("WoodGathered", true);
        AddEffect("TreeChopped", true);
    }
    
    public override bool CanExecute(IGoapState worldState)
    {
        return !actionAssigned && base.CanExecute(worldState);
    }
    
    public override IGoapState Execute(IGoapState worldState)
    {
        if (targetAgent != null && !actionAssigned)
        {
            targetAgent.SetCurrentAction(AgentActions.ChopTree);
            actionAssigned = true;
            
            Debug.Log($"[ChopTree] Assigned ChopTree action to {targetAgent.GetName()}");
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

    public void SetTargetAgent(VillagerAgent availableAgent)
    {
        targetAgent = availableAgent;
    }
}
