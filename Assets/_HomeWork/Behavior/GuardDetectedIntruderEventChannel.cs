using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/Guard Detected Intruder")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "Guard Detected Intruder", message: "[Guard] has spotted [intruder] at [position]", category: "Events", id: "1ad3836039ffaddd90c070ebdbdd20ef")]
public partial class GuardDetectedIntruderEventChannel : EventChannelBase
{
    public delegate void GuardDetectedIntruderEventHandler(GameObject Guard, GameObject intruder, Vector3 position);
    public event GuardDetectedIntruderEventHandler Event; 

    public void SendEventMessage(GameObject Guard, GameObject intruder, Vector3 position)
    {
        Event?.Invoke(Guard, intruder, position);
    }

    public override void SendEventMessage(BlackboardVariable[] messageData)
    {
        BlackboardVariable<GameObject> GuardBlackboardVariable = messageData[0] as BlackboardVariable<GameObject>;
        var Guard = GuardBlackboardVariable != null ? GuardBlackboardVariable.Value : default(GameObject);

        BlackboardVariable<GameObject> intruderBlackboardVariable = messageData[1] as BlackboardVariable<GameObject>;
        var intruder = intruderBlackboardVariable != null ? intruderBlackboardVariable.Value : default(GameObject);

        BlackboardVariable<Vector3> positionBlackboardVariable = messageData[2] as BlackboardVariable<Vector3>;
        var position = positionBlackboardVariable != null ? positionBlackboardVariable.Value : default(Vector3);

        Event?.Invoke(Guard, intruder, position);
    }

    public override Delegate CreateEventHandler(BlackboardVariable[] vars, System.Action callback)
    {
        GuardDetectedIntruderEventHandler del = (Guard, intruder, position) =>
        {
            BlackboardVariable<GameObject> var0 = vars[0] as BlackboardVariable<GameObject>;
            if(var0 != null)
                var0.Value = Guard;

            BlackboardVariable<GameObject> var1 = vars[1] as BlackboardVariable<GameObject>;
            if(var1 != null)
                var1.Value = intruder;

            BlackboardVariable<Vector3> var2 = vars[2] as BlackboardVariable<Vector3>;
            if(var2 != null)
                var2.Value = position;

            callback();
        };
        return del;
    }

    public override void RegisterListener(Delegate del)
    {
        Event += del as GuardDetectedIntruderEventHandler;
    }

    public override void UnregisterListener(Delegate del)
    {
        Event -= del as GuardDetectedIntruderEventHandler;
    }
}

