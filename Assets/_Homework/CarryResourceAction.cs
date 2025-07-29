using System;
using GOAP.Agents;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CarryResource", story: "[Agent] moves to carry resource [location]", category: "Action", id: "b2ed15fed025ce1c1d42a7547a800bf9")]
public partial class CarryResourceAction : Action
{
    [SerializeReference] public BlackboardVariable<AgentType> Type;
    [SerializeReference] public BlackboardVariable<Vector3> Location;
    [SerializeReference] public BlackboardVariable<Vector3> BulidLocation;
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    private SimpleAgent _agent;

    protected override Status OnStart()
    {
        switch (Type.Value)
        {
            case AgentType.Villager:
                _agent = Agent.Value.GetComponent<VillagerAgent>();
                if (_agent is VillagerAgent a)
                {
                    //Location = (BlackboardVariable<Vector3>) a.GetCarryResourceLocation();
                }
                break;
            case AgentType.Mage:
                _agent = Agent.Value.GetComponent<VillagerAgent>();
                if (_agent is VillagerAgent b)
                {
                    //Location = (BlackboardVariable<Vector3>) b.GetCarryResourceLocation();
                }
                break;
            case AgentType.Messenger:
                _agent = Agent.Value.GetComponent<VillagerAgent>();
                if (_agent is VillagerAgent c)
                {
                  //  Location = (BlackboardVariable<Vector3>) c.GetCarryResourceLocation();
                }
                break;
        }
        return Status.Success;
    }

   
}

