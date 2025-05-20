using UnityEngine;
using System.Collections.Generic;
using Unity.Behavior;

// Base abstract class for all senses
public abstract class BaseSensor : MonoBehaviour
{
    [Header("Base Sensor Settings")]
    [SerializeField] protected float updateFrequency = 0.2f;
    [SerializeField] protected LayerMask detectionLayers;
    [SerializeField] protected string[] detectableTags;
    [SerializeField] protected bool showDebugVisuals = true;
    [SerializeField] protected Color debugColor = Color.yellow;
    [SerializeField] protected BehaviorGraph graph;

    protected float sensorTimer;
    public List<StimulusInfo> DetectedStimuli { get; protected set; } = new List<StimulusInfo>();

    protected virtual void Update()
    {
        sensorTimer += Time.deltaTime;
        if (sensorTimer >= updateFrequency)
        {
            Sense();
            sensorTimer = 0f;
        }
    }

    protected abstract void Sense();
    protected abstract void DrawDebugVisualization();

    protected virtual void OnDrawGizmosSelected()
    {
        if (showDebugVisuals)
        {
            DrawDebugVisualization();
        }
    }

    public virtual List<StimulusInfo> GetCurrentStimuli()
    {
        return DetectedStimuli;
    }
}

// Class to hold information about detected stimuli

// Enum to categorize different types of stimuli

// Visual sensor implementation

// Audio sensor implementation

// Example of a central sensory system that can manage multiple sensors