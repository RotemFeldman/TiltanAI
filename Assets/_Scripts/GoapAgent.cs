using System.Collections.Generic;
using UnityEngine;

public class GoapAgent : MonoBehaviour
{
    public List<GoapAction> actions = new List<GoapAction>();
    public List<GoapGoal> goals = new List<GoapGoal>();
    public Dictionary<string, bool> worldState = new Dictionary<string, bool>();

    private Queue<GoapAction> currentPlan = new Queue<GoapAction>();

    void Start()
    {
        // Example initial state of the world
        worldState["hasFood"] = true;
        worldState["Nothing"] = false;

        // Build the list of actions from components on this GameObject
        actions.Add(new EatFoodAction());

       
        goals.Add(new GoapGoal("EatFood", 1));
        goals.Add(new GoapGoal("Nothing", 2));
    }

    void Update()
    {
        if (currentPlan.Count == 0)
        {
            PlanActions();
        }

        if (currentPlan.Count > 0)
        {
            var action = currentPlan.Peek();
            if (action.IsDone())
            {
                currentPlan.Dequeue();
            }
            else
            {
                action.Perform();
            }
        }
    }

    void PlanActions()
    {
        GoapPlanner planner = new GoapPlanner();
        GoapGoal bestGoal = planner.SelectGoal(goals, worldState);
        currentPlan = planner.Plan(actions, bestGoal, worldState);
    }
}