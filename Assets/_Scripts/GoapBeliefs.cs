using System.Collections.Generic;
using UnityEngine;

public class GoapBeliefs : MonoBehaviour
{
    private Dictionary<string, bool> beliefs = new Dictionary<string, bool>();

    // Add or update a belief
    public void SetBelief(string key, bool value)
    {
        beliefs[key] = value;
    }

    // Remove a belief
    public void RemoveBelief(string key)
    {
        if (beliefs.ContainsKey(key))
            beliefs.Remove(key);
    }

    // Check if the agent believes something
    public bool HasBelief(string key)
    {
        return beliefs.ContainsKey(key) && beliefs[key];
    }

    // Get the full belief state
    public Dictionary<string, bool> GetBeliefs()
    {
        return beliefs;
    }

    // Optional: Clear all beliefs
    public void ClearBeliefs()
    {
        beliefs.Clear();
    }
}