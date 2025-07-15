using System.Collections.Generic;

public interface IGoapGoal
{
    string GetName();
    bool IsAchieved(IGoapState worldState);
    Dictionary<string, object> GetConditions();
    float GetPriority();
}
