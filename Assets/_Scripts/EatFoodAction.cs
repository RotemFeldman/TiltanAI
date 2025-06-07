using System.Collections.Generic;
using UnityEngine;

public class EatFoodAction : GoapAction
{
    private bool done = false;

    public EatFoodAction()
    {
        Preconditions.Add("hasFood", true);
        Effects.Add("EatFood", true);
    }

    // Override only if you need custom procedural checks beyond the dictionary preconditions
    

    public override void Perform()
    {
        Debug.Log("Eating food...");
        done = true;
    }

    public override bool IsDone()
    {
        return done;
    }
}