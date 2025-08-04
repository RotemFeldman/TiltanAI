using System;
using GOAP;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using GOAP.Agents;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SelfCompleteAction", story: "[Agent] complete task", category: "Action", id: "bf08d8c6e9f1446e2cff6cbdaf1e3e3a")]
public partial class SelfCompleteAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<AgentType> Type;
    private GoapAgent _agent;

    protected override Status OnStart()
    {
        switch (Type.Value)
        {
            case AgentType.Villager:
                _agent = Agent.Value.GetComponent<VillagerAgent>();
                break;
            case AgentType.Mage:
                _agent = Agent.Value.GetComponent<MageAgent>();
                break;
            case AgentType.Messenger:
                _agent = Agent.Value.GetComponent<MessengerAgent>();
                break;
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
       // _agent.CompleteTask();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

