 using UnityEngine;

public class ProximitySensor : BaseSensor
{
    [Header("Proximity Settings")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float minProximityIntensity = 0.1f;

    protected override void Sense()
    {
        DetectedStimuli.Clear();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayers);

        foreach (Collider hit in hitColliders)
        {
            if (hit.gameObject == gameObject) continue;

            // Check if the object has one of the detectable tags (if specified)
            if (detectableTags.Length > 0)
            {
                bool hasDetectableTag = false;
                foreach (string tag in detectableTags)
                {
                    if (hit.CompareTag(tag))
                    {
                        hasDetectableTag = true;
                        break;
                    }
                }
                if (!hasDetectableTag) continue;
            }

            // Calculate intensity based on distance - closer objects have higher intensity
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            float intensity = 1f - (distance / detectionRadius);
            
            if (intensity >= minProximityIntensity)
            {
                DetectedStimuli.Add(new StimulusInfo(
                    hit.gameObject,
                    StimulusType.Touch, // Using Touch as the stimulus type for proximity
                    intensity,
                    hit.transform.position
                ));
            }
        }
    }

    protected override void DrawDebugVisualization()
    {
        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw lines to detected objects
        if (Application.isPlaying && DetectedStimuli.Count > 0)
        {
            foreach (var stimulus in DetectedStimuli)
            {
                Gizmos.DrawLine(transform.position, stimulus.Location);
            }
        }
    }
}