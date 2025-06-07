using System.Collections.Generic;

public class GoapGoal
{
    public string goalKey;
    public float priority;
    public Dictionary<string, bool> Conditions;

    // Constructor for backward compatibility
    public GoapGoal(string goalKey, float priority)
    {
        this.goalKey = goalKey;
        this.priority = priority;
        this.Conditions = new Dictionary<string, bool> { { goalKey, true } };
    }

    // New constructor that accepts multiple conditions
    public GoapGoal(string goalKey, float priority, Dictionary<string, bool> conditions)
    {
        this.goalKey = goalKey;
        this.priority = priority;
        this.Conditions = conditions;
    }

    public override string ToString()
    {
        return $"Goal: {goalKey} (Priority: {priority})";
    }
}