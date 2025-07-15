using System.Collections.Generic;

public interface IGoapAction
{
    string GetName();
    float GetCost();
    bool CanExecute(IGoapState worldState);
    IGoapState Execute(IGoapState worldState);
    Dictionary<string, object> GetPreconditions();
    Dictionary<string, object> GetEffects();
}
