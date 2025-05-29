using UnityEngine;
using System.Collections.Generic;

namespace AI.Sensing
{
    public abstract class BaseSense : MonoBehaviour
    {
        [SerializeField] protected bool showGizmos = true;
        [SerializeField] protected Color gizmoColor = Color.yellow;
        [SerializeField] protected StimuliType senseType;
        
        protected Transform agentTransform;
        protected List<StimulusInfo> detectedStimuli = new List<StimulusInfo>();
        
        protected virtual void Awake()
        {
            agentTransform = transform;
        }
        
        /// <summary>
        /// Updates the sense and detects stimuli in the environment.
        /// </summary>
        public abstract void UpdateSense();
        
        /// <summary>
        /// Returns all currently detected stimuli.
        /// </summary>
        public List<StimulusInfo> GetDetectedStimuli()
        {
            return detectedStimuli;
        }
        
        /// <summary>
        /// Returns the type of this sense.
        /// </summary>
        public StimuliType GetSenseType()
        {
            return senseType;
        }
        
        /// <summary>
        /// Checks if a specific object is currently detected by this sense.
        /// </summary>
        public bool IsDetected(GameObject obj)
        {
            foreach (var stimulus in detectedStimuli)
            {
                if (stimulus.Source == obj)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gets stimulus information for a specific object if detected.
        /// </summary>
        public bool TryGetStimulusInfo(GameObject obj, out StimulusInfo info)
        {
            foreach (var stimulus in detectedStimuli)
            {
                if (stimulus.Source == obj)
                {
                    info = stimulus;
                    return true;
                }
            }
            
            info = default;
            return false;
        }
        
        /// <summary>
        /// Clears all currently detected stimuli.
        /// </summary>
        public void ClearDetectedStimuli()
        {
            detectedStimuli.Clear();
        }
        
        /// <summary>
        /// Calculate intensity based on distance (closer = more intense)
        /// </summary>
        protected float CalculateIntensity(float distance, float maxDistance)
        {
            return Mathf.Clamp01(1f - (distance / maxDistance));
        }
    }
}