using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Guard Sensed", story: "[Self] is in proximity to a Guard, out [Guard]", category: "Conditions", id: "b39a5cbe518185ab8477dff28e8b90db")]
public partial class GuardSensedCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Guard;
    
    private ProximitySensor _sensor;

    public override bool IsTrue()
    {
        var stim = _sensor.DetectedStimuli;
        Debug.Log(stim.Count);
        if (stim.Count == 0)
        {
            return false;
        }
        var sens = stim[0];
        if (sens.Source != Guard.Value)
        {
            Guard.Value = sens.Source;
        }
        return true;
    }

    public override void OnStart()
    {
        _sensor = Self.Value.GetComponent<ProximitySensor>();
    }

    public override void OnEnd()
    {
    }
}
