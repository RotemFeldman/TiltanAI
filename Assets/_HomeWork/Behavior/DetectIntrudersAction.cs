using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Detect Intruders", story: "[Self] Detect [Intruder] at [Location] and update [GuardMode]", category: "Action", id: "55b86f0469597a719cfac960de21fb6a")]
public partial class DetectIntrudersAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Intruder;
    [SerializeReference] public BlackboardVariable<Vector3> Location;
    [SerializeReference] public BlackboardVariable<GuardMode> GuardMode;
    
    private VisualSensor _sensor;

    protected override Status OnStart()
    {
        _sensor = Self.Value.GetComponent<VisualSensor>();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var stim = _sensor.DetectedStimuli;
        if(stim.Count == 0)
        {
            Intruder.Value = null;
            return Status.Running;
        }
        var sens = stim[0];
        
        Intruder.Value = sens.Source;
        Location.Value = sens.Location;
        GuardMode.Value = global::GuardMode.Chase;
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

