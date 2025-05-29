using UnityEngine;
using System;

namespace AI.Sensing
{
    /// <summary>
    /// Defines different types of stimuli that can be detected by AI senses.
    /// </summary>
    public enum StimuliType
    {
        Visual,
        Audio,
        Touch,
        Smell,
        Thermal,
        Threat,
        Ally,
        Interest,
        Custom
    }
    
    /// <summary>
    /// Contains detailed information about a detected stimulus.
    /// </summary>
    [Serializable]
    public struct StimulusInfo
    {
        /// <summary>
        /// The GameObject that is the source of the stimulus.
        /// </summary>
        public GameObject Source;
        
        /// <summary>
        /// The intensity of the stimulus (0-1 range, with 1 being the strongest).
        /// </summary>
        public float Intensity;
        
        /// <summary>
        /// The world position where the stimulus was detected.
        /// </summary>
        public Vector3 Location;
        
        /// <summary>
        /// The time when the stimulus was first detected.
        /// </summary>
        public float TimeDetected;
        
        /// <summary>
        /// The type of stimulus.
        /// </summary>
        public StimuliType Type;
        
        /// <summary>
        /// How long the stimulus has been detected for.
        /// </summary>
        public float Age => Time.time - TimeDetected;
        
        /// <summary>
        /// Creates a new stimulus information struct.
        /// </summary>
        public StimulusInfo(GameObject source, float intensity, Vector3 location, StimuliType type)
        {
            Source = source;
            Intensity = Mathf.Clamp01(intensity);
            Location = location;
            TimeDetected = Time.time;
            Type = type;
        }
        
        public override string ToString()
        {
            return $"Stimulus [{Type}] from {Source.name} at {Location}, intensity: {Intensity:F2}, age: {Age:F2}s";
        }
    }
}