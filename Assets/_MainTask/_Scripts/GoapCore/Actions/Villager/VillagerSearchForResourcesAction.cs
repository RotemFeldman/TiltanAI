using System.Collections.Generic;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class VillagerSearchForResourcesAction : GoapAction
{
    private VillagerAgent targetAgent;
    private bool actionAssigned = false;
    
    public VillagerSearchForResourcesAction() : base("SearchForResources", 2f)
    {
        // Preconditions: Need an available villager and not already have resources
        AddPrecondition("AvailableVillagers", 1);
        AddPrecondition("HasResources", false);
        
        // Effects: After searching, we know if resources are available
        AddEffect("ResourcesLocated", true);
        AddEffect("SearchComplete", true);
    }
    
    public override bool CanExecute(IGoapState worldState)
    {
        // Can execute if base conditions are met and action hasn't been assigned yet
        return !actionAssigned && base.CanExecute(worldState);
    }
    
    public override IGoapState Execute(IGoapState worldState)
    {
        // The main job of this GOAP action is to tell the agent what to do
        // The actual behavior implementation is handled by the agent's behavior graph
        
        if (targetAgent != null && !actionAssigned)
        {
            // Set the agent's current action enum
            targetAgent.SetCurrentAction(VillagerActions.SearchForResources);
            actionAssigned = true;
            
            Debug.Log($"[SearchForResources] Assigned SearchForResources action to {targetAgent.GetName()}");
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
        
        // Check if the agent has completed the search action
        // This depends on your agent's behavior graph implementation
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