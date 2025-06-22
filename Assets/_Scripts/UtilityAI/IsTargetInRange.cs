

using UnityEngine;
using UtilityAI;

namespace UtilityAI
{


    [CreateAssetMenu(menuName = "UtilityAI/Considerations/IsTargetInRange")]
    public class IsTargetInRange : Consideration
    {
        public float maxDistance = 10f;
        public string tag = "Target";

        
        // Maximum distance for the curve to be effective
        public override float Evaluate(Context context)
        {
            if (!context.sensor.targetTags.Contains(tag))
                context.sensor.targetTags.Add(tag);

            Transform nearTransform = context.sensor.GetClosestTarget(tag);
            if (nearTransform == null)
            {
                return 0f; // No target found, return minimum utility
            }

            // Calculate the distance to the target
            float distance = Vector3.Distance(context.navAgent.transform.position, nearTransform.position);
            return 1- Mathf.Clamp01(distance / maxDistance); // Normalize distance to a value between 0 and 1

        }
    }
}