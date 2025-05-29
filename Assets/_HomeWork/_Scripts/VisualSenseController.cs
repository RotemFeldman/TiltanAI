using UnityEngine;
using System.Collections.Generic;

namespace AI.Sensing
{
    public class VisualSense : BaseSense
    {
        [Header("Detection Settings")]
        [SerializeField, Range(1f, 100f)] private float detectionRange = 10f;
        [SerializeField, Range(1f, 360f)] private float detectionAngle = 90f;
        [SerializeField] private LayerMask detectionLayers;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask obstacleLayer;
        
        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.2f;
        [SerializeField] private int maxDetectableObjects = 10;
        
        [Header("Intensity Settings")]
        [SerializeField] private bool useDistanceForIntensity = true;
        [SerializeField] private bool useAngleForIntensity = true;
        [SerializeField] private float minIntensityThreshold = 0.1f;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool useFieldOfViewShape = true;
        [SerializeField, Range(3, 20)] private int fovResolution = 10;
        
        private float nextUpdateTime;
        
        private void OnValidate()
        {
            senseType = StimuliType.Visual;
        }
        
        private void Start()
        {
            nextUpdateTime = Time.time;
            senseType = StimuliType.Visual;
        }
        
        private void Update()
        {
            if (Time.time >= nextUpdateTime)
            {
                UpdateSense();
                nextUpdateTime = Time.time + updateInterval;
            }
        }
        
        public override void UpdateSense()
        {
            ClearDetectedStimuli();
            
            // Find all potential objects within range
            Collider[] colliders = Physics.OverlapSphere(agentTransform.position, detectionRange, detectionLayers);
            
            int detectedCount = 0;
            foreach (Collider col in colliders)
            {
                // Skip self
                if (col.transform == agentTransform)
                    continue;
                
                // Check if we've reached the max detectable objects
                if (detectedCount >= maxDetectableObjects)
                    break;
                
                Vector3 directionToTarget = col.transform.position - agentTransform.position;
                float distanceToTarget = directionToTarget.magnitude;
                
                // Check if object is within our detection range
                if (distanceToTarget > detectionRange)
                    continue;
                
                // Check if object is within our field of view angle
                float angleToTarget = Vector3.Angle(agentTransform.forward, directionToTarget);
                if (angleToTarget > detectionAngle * 0.5f)
                    continue;
                
                // Check if line of sight is required and not blocked
                if (requireLineOfSight)
                {
                    if (Physics.Raycast(agentTransform.position, directionToTarget.normalized, distanceToTarget, obstacleLayer))
                        continue; // Line of sight is blocked
                }
                
                // Calculate intensity based on distance and angle
                float intensity = 1.0f;
                
                if (useDistanceForIntensity)
                {
                    intensity *= CalculateIntensity(distanceToTarget, detectionRange);
                }
                
                if (useAngleForIntensity)
                {
                    // Objects directly in front have higher intensity
                    float angleIntensity = 1.0f - (angleToTarget / (detectionAngle * 0.5f));
                    intensity *= angleIntensity;
                }
                
                // Only detect if intensity is above threshold
                if (intensity < minIntensityThreshold)
                    continue;
                
                // Create stimulus and add to detected list
                Vector3 detectionPoint = col.ClosestPoint(agentTransform.position);
                StimulusInfo stimulus = new StimulusInfo(col.gameObject, intensity, detectionPoint, senseType);
                detectedStimuli.Add(stimulus);
                detectedCount++;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showGizmos || !enabled)
                return;
            
            // Draw detection range and field of view
            Gizmos.color = gizmoColor;
            
            if (useFieldOfViewShape)
            {
                // Draw field of view cone
                DrawFieldOfViewCone();
            }
            else
            {
                // Draw simple wireframe sphere for range
                Gizmos.DrawWireSphere(transform.position, detectionRange);
                
                // Draw direction
                Gizmos.DrawRay(transform.position, transform.forward * detectionRange);
            }
            
            // Draw lines to detected objects with intensity-based color
            foreach (var stimulus in detectedStimuli)
            {
                if (stimulus.Source != null)
                {
                    // Use intensity to determine color (green = high intensity, yellow = medium, red = low)
                    Color intensityColor = Color.Lerp(Color.red, Color.green, stimulus.Intensity);
                    Gizmos.color = intensityColor;
                    
                    Gizmos.DrawLine(transform.position, stimulus.Location);
                    
                    // Draw sphere at the detection point
                    Gizmos.DrawSphere(stimulus.Location, 0.1f);
                }
            }
        }
        
        private void DrawFieldOfViewCone()
        {
            float halfAngle = detectionAngle * 0.5f;
            
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            
            // Draw main forward ray
            Gizmos.DrawRay(transform.position, forward * detectionRange);
            
            // Draw the cone
            for (int i = 0; i < fovResolution; i++)
            {
                float angle = (i / (float)(fovResolution - 1)) * halfAngle;
                
                // Right side
                Quaternion rightRot = Quaternion.AngleAxis(angle, up);
                Vector3 rightDir = rightRot * forward;
                Gizmos.DrawRay(transform.position, rightDir * detectionRange);
                
                // Left side
                Quaternion leftRot = Quaternion.AngleAxis(-angle, up);
                Vector3 leftDir = leftRot * forward;
                Gizmos.DrawRay(transform.position, leftDir * detectionRange);
                
                // Top and bottom if we want a full 3D cone
                Quaternion topRot = Quaternion.AngleAxis(angle, right);
                Vector3 topDir = topRot * forward;
                Gizmos.DrawRay(transform.position, topDir * detectionRange);
                
                Quaternion bottomRot = Quaternion.AngleAxis(-angle, right);
                Vector3 bottomDir = bottomRot * forward;
                Gizmos.DrawRay(transform.position, bottomDir * detectionRange);
                
                // Draw some connections between rays to make it look more like a cone
                if (i > 0)
                {
                    float prevAngle = ((i - 1) / (float)(fovResolution - 1)) * halfAngle;
                    
                    // Connect horizontal rays
                    Vector3 prevRightPos = transform.position + (Quaternion.AngleAxis(prevAngle, up) * forward) * detectionRange;
                    Vector3 currRightPos = transform.position + rightDir * detectionRange;
                    Gizmos.DrawLine(prevRightPos, currRightPos);
                    
                    Vector3 prevLeftPos = transform.position + (Quaternion.AngleAxis(-prevAngle, up) * forward) * detectionRange;
                    Vector3 currLeftPos = transform.position + leftDir * detectionRange;
                    Gizmos.DrawLine(prevLeftPos, currLeftPos);
                    
                    // Connect vertical rays
                    Vector3 prevTopPos = transform.position + (Quaternion.AngleAxis(prevAngle, right) * forward) * detectionRange;
                    Vector3 currTopPos = transform.position + topDir * detectionRange;
                    Gizmos.DrawLine(prevTopPos, currTopPos);
                    
                    Vector3 prevBottomPos = transform.position + (Quaternion.AngleAxis(-prevAngle, right) * forward) * detectionRange;
                    Vector3 currBottomPos = transform.position + bottomDir * detectionRange;
                    Gizmos.DrawLine(prevBottomPos, currBottomPos);
                }
            }
            
            // Draw connecting arc at the end of the cone
            float arcStep = halfAngle * 2 / (fovResolution - 1);
            for (int i = 0; i < fovResolution - 1; i++)
            {
                float angle1 = -halfAngle + i * arcStep;
                float angle2 = -halfAngle + (i + 1) * arcStep;
                
                Vector3 pos1 = transform.position + (Quaternion.AngleAxis(angle1, up) * forward) * detectionRange;
                Vector3 pos2 = transform.position + (Quaternion.AngleAxis(angle2, up) * forward) * detectionRange;
                
                Gizmos.DrawLine(pos1, pos2);
            }
        }
        
        #region Public Accessors
        
        public float DetectionRange 
        { 
            get => detectionRange; 
            set => detectionRange = Mathf.Clamp(value, 1f, 100f); 
        }
        
        public float DetectionAngle 
        { 
            get => detectionAngle; 
            set => detectionAngle = Mathf.Clamp(value, 1f, 360f); 
        }
        
        public LayerMask DetectionLayers 
        { 
            get => detectionLayers; 
            set => detectionLayers = value; 
        }
        
        public bool RequireLineOfSight 
        { 
            get => requireLineOfSight; 
            set => requireLineOfSight = value; 
        }
        
        #endregion
    }
}