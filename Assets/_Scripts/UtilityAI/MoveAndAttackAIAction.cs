using UnityEngine;

namespace UtilityAI {
    [CreateAssetMenu(menuName = "UtilityAI/Actions/MoveAndAttackAction")]
    public class MoveAndAttackAIAction : AIAction {
        [SerializeField] private float attackRange = 2f; // Distance at which the agent can attack

        
        private float nextAttackTime = 0f;
        private Attacker attacker;
        public override void Initialize(Context context) {
            context.sensor.targetTags.Add(targetTag);
            attacker = context.agent.GetComponent<Attacker>();
        }

        public override void Execute(Context context) {
            var target = context.sensor.GetClosestTarget(targetTag);
            if (target == null) return;

            context.target = target;
            
            // Move to target
            context.navAgent.SetDestination(target.position);
            
            // Check if we're in range to attack
            float distanceToTarget = Vector3.Distance(context.navAgent.transform.position, target.position);
            
            if (distanceToTarget <= attackRange && Time.time >= nextAttackTime) {
                // Get the attacker interface from our agent
                
                if (attacker != null) {
                    // Try to attack the target agent
                    var targetAgent = target.GetComponent<Agent>();
                    if (targetAgent != null) {
                        bool attackSuccess = attacker.TryAttack(targetAgent);
                        if (attackSuccess) {
                            nextAttackTime = Time.time + attacker.attackCooldown;
                        }
                    }
                }
            }
        }
    }
}
