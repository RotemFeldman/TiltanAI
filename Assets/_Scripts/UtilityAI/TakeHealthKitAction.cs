using UnityEngine;

namespace UtilityAI {
    [CreateAssetMenu(menuName = "UtilityAI/Actions/TakeHealthKitAction")]
    public class TakeHealthKit : AIAction {
        [SerializeField] private float healAmount = 25f;
        private bool isMovingToTarget = false;

        public override void Initialize(Context context) {
            context.sensor.targetTags.Add(targetTag);
        }

        public override void Execute(Context context) {
            var target = context.sensor.GetClosestTarget(targetTag);
            if (target == null) {
                // Reset state if no target is found
                isMovingToTarget = false;
                return;
            }

            // Set target in context
            context.target = target;
            
            if (!isMovingToTarget) {
                // Start moving toward the health kit
                context.navAgent.SetDestination(target.position);
                isMovingToTarget = true;
                return;
            }

            // Check if we've reached the destination
            if (isMovingToTarget && HasReachedDestination(context.navAgent)) {
                // We've reached the health kit, apply the healing effect
                Agent agentComponent = context.brain.GetComponent<Agent>();
                if (agentComponent != null) {
                    agentComponent.Heal(healAmount);
                    Debug.Log($"Agent healed for {healAmount} health!");
                }
                
                // Destroy the health kit
                if (target.gameObject.GetComponent<HealthItem>() != null) {
                    Object.Destroy(target.gameObject);
                }
                
                // Reset state
                isMovingToTarget = false;
                context.target = null;
            }
        }

        private bool HasReachedDestination(UnityEngine.AI.NavMeshAgent agent) {
            // Check if we're close enough to the destination
            if (!agent.pathPending) {
                if (agent.remainingDistance <= agent.stoppingDistance) {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}