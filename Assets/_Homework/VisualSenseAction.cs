using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Linq;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Visual Sense", story: "[Self] checks for visual detections with [TargetTag]", category: "Action", id: "950ff92ii16667ghh8001g8i3g17661h")]
public partial class VisualSenseAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<string> TargetTag;
    [SerializeReference] public BlackboardVariable<bool> IsDetecting;
    [SerializeReference] public BlackboardVariable<Vector3> DetectionLocation;

    private VisualSensor visualSensor;

    protected override Status OnStart()
    {
        // Validate self
        if (Self?.Value == null)
        {
            Debug.LogError("VisualSenseAction: Self is null");
            return Status.Failure;
        }

        // Find the visual sensor
        visualSensor = Self.Value.GetComponentInChildren<VisualSensor>();
        if (visualSensor == null)
        {
            Debug.LogError("VisualSenseAction: No VisualSensor found on Self");
            return Status.Failure;
        }

        // Start continuous sensing
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self?.Value == null || visualSensor == null)
            return Status.Failure;

        // Perform scan and update blackboard
        PerformScan();
        
        // Continue running to keep sensing
        return Status.Running;
    }

    private void PerformScan()
    {
        // Get current visual detections
        var visualStimuli = visualSensor.GetCurrentStimuli()
            .Where(s => s.Type == StimulusType.Visual)
            .ToList();

        // Filter by tag if specified
        string targetTag = TargetTag?.Value;
        if (!string.IsNullOrEmpty(targetTag))
        {
            visualStimuli = visualStimuli
                .Where(s => s.Source != null && s.Source.CompareTag(targetTag))
                .ToList();
        }

        bool hasDetections = visualStimuli.Count > 0;
        
        IsDetecting.Value = hasDetections;

        // Update DetectionLocation (closest detection)
        if (DetectionLocation != null)
        {
            if (hasDetections)
            {
                // Find closest detection
                Vector3 selfPosition = Self.Value.transform.position;
                var closestStimulus = visualStimuli
                    .OrderBy(s => Vector3.Distance(selfPosition, s.Location))
                    .First();
                
                DetectionLocation.Value = closestStimulus.Location;
            }
            else
            {
                DetectionLocation.Value = Vector3.zero;
            }
        }
    }
}