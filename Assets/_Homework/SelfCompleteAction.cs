using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using _MainTask._Scripts.GoapCore.Agents;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SelfCompleteAction", story: "[Agent] complete task", category: "Action", id: "bf08d8c6e9f1446e2cff6cbdaf1e3e3a")]
public partial class SelfCompleteAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<AgentType> Type;
    private IGoapAgent _agent;

    protected override Status OnStart()
    {
        switch (Type.Value)
        {
            case AgentType.Villager:
                _agent = Agent.Value.GetComponent<VillagerAgent>();
                break;
            case AgentType.Mage:
                //_agent = Agent.Value.GetComponent<>() 
                break;
            case AgentType.Messenger:
                //_agent = Agent.Value.GetComponent<>()
                break;
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        switch (Type.Value)
        {
            case AgentType.Villager:
                if(_agent is VillagerAgent a)
                    a.CompleteCurrentAction();
                break;
            case AgentType.Mage:
                //if(_agent is MageAgent a)
                   // a.CompleteCurrentAction();
                break;
            case AgentType.Messenger:
               // if(_agent is MessengerAgent a)
                   // a.CompleteCurrentAction();
                break;
            default:
                return Status.Failure;
        }
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

