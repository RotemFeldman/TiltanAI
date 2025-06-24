using UnityEngine;

namespace UtilityAI {
    [CreateAssetMenu(menuName = "UtilityAI/Actions/WanderAction")]
    public class WanderAIAction : AIAction {
        [SerializeField] private float wanderRadius = 10f; // How far from the starting position the agent can wander
        [SerializeField] private float wanderDistance = 5f; // Distance to move in each wander step
        [SerializeField] private float waitTime = 2f; // Time to wait before choosing a new destination
        
        private Vector3 centerPoint;
        private float nextWanderTime = 0f;
        private bool hasInitializedCenter = false;

        public override void Initialize(Context context) {
            // Set the center point as the agent's starting position
            if (!hasInitializedCenter) {
                centerPoint = context.agent.transform.position;
                hasInitializedCenter = true;
            }
        }

        public override void Execute(Context context) {
            // Check if it's time to pick a new wander destination
            if (Time.time >= nextWanderTime || !context.navAgent.hasPath || context.navAgent.remainingDistance < 0.5f) {
                Vector3 newDestination = GetRandomWanderPoint();
                context.navAgent.SetDestination(newDestination);
                nextWanderTime = Time.time + waitTime;
            }
        }

        private Vector3 GetRandomWanderPoint() {
            // Generate a random point within the wander radius
            Vector3 randomDirection = Random.insideUnitSphere * wanderDistance;
            randomDirection += centerPoint;
            
            // Make sure the point stays within the wander radius
            Vector3 finalPosition = Vector3.ClampMagnitude(randomDirection - centerPoint, wanderRadius) + centerPoint;
            
            // Keep the Y position at the center point's Y level (assuming flat ground)
            finalPosition.y = centerPoint.y;
            
            return finalPosition;
        }

        // Optional: Reset the center point if needed
        public void ResetCenterPoint(Vector3 newCenter) {
            centerPoint = newCenter;
            hasInitializedCenter = true;
        }
    }
}