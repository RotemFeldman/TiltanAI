using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GoapAgentConfig", menuName = "GOAP/Agent Configuration")]
public class GoapAgentConfigSO : ScriptableObject
{
    public GoapWorldStateDefinitionSO initialWorldState;
    public List<GoapActionDefinitionSO> availableActions = new List<GoapActionDefinitionSO>();
    public List<GoapGoalDefinitionSO> possibleGoals = new List<GoapGoalDefinitionSO>();
}