using UnityEngine;

public class VisualSensor : BaseSensor
{
    [Header("Vision Settings")]
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private bool requiresLineOfSight = true;
    [SerializeField] private float minVisualIntensity = 0.1f;

    protected override void Sense()
    {
        DetectedStimuli.Clear();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewRadius, detectionLayers);

        foreach (Collider hit in hitColliders)
        {
            if (hit.gameObject == gameObject) continue;

            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle <= viewAngle * 0.5f)
            {
                if (requiresLineOfSight)
                {
                    // Ignore the layer that your sensor is on
                    int layerMask = ~(1 << gameObject.layer);
                    if (!Physics.Raycast(transform.position, directionToTarget, out RaycastHit rayHit, viewRadius, layerMask))
                        continue;

                    if (rayHit.collider != hit)
                        continue;
                }

                // Calculate intensity based on distance
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float intensity = 1f - (distance / viewRadius);
                if (intensity >= minVisualIntensity)
                {
                    DetectedStimuli.Add(new StimulusInfo(
                        hit.gameObject,
                        StimulusType.Visual,
                        intensity,
                        hit.transform.position
                    ));
                }
            }
        }
    }

    protected override void DrawDebugVisualization()
    {
        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftDirection = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        
        Gizmos.DrawLine(transform.position, transform.position + leftDirection * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDirection * viewRadius);
    }
}