using System.Collections.Generic;
using UnityEngine;

public class GenericGoapAction : GoapAction
{
    private bool done = false;
    public string ActionName { get; private set; }
    

    public GenericGoapAction(string actionName)
    {
        ActionName = actionName;
    }

    public override void Perform()
    {
        Debug.Log($"Performing action: {ActionName}");
        done = true;
    }

    public override bool IsDone()
    {
        return done;
    }
    
    public void Reset()
    {
        done = false;
    }
}