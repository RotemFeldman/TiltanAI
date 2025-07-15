using System.Collections.Generic;
using _MainTask._Scripts.GoapCore.Agents;
using UnityEngine;

public class VillagerRefineCrystalsAction : GoapAction
{
    private VillagerAgent targetAgent;
    private bool actionAssigned = false;
    
    public VillagerRefineCrystalsAction() : base("RefineCrystals", 4f)
    {
        
        // Preconditions: Need an available villager and raw crystals
        AddPrecondition("AvailableVillagers", 1);
        AddPrecondition("RawCrystalsAvailable", true);
        AddPrecondition("RefineryAvailable", true);
        
        // Effects: After refining, we have refined crystals
        AddEffect("RefinedCrystals", true);
        AddEffect("CrystalsRefined", true);
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
            targetAgent.SetCurrentAction(VillagerActions.RefineCrystals);
            actionAssigned = true;
            
            Debug.Log($"[RefineCrystals] Assigned RefineCrystals action to {targetAgent.GetName()}");
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
