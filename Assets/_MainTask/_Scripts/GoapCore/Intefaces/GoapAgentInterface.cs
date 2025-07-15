using System.Collections.Generic;
using UnityEngine;

public interface IGoapAgent
{
    string GetName();
    bool IsAvailable();
    bool CanPerformAction(IGoapAction action);
    void AssignAction(IGoapAction action);
    bool IsActionComplete();
    Vector3 GetPosition();
    List<IGoapAction> GetAvailableActions();
}
