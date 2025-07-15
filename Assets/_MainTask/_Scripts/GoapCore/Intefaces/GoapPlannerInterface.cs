using System.Collections.Generic;

public interface IGoapPlanner
{
    List<IGoapAction> CreatePlan(IGoapState worldState, IGoapGoal goal, List<IGoapAction> availableActions);
}
