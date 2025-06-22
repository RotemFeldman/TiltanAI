using UnityEngine;

namespace UtilityAI {
    [CreateAssetMenu(menuName = "UtilityAI/Actions/IdleAction")]
    public class IdleAIAction : AIAction {
        public override void Execute(Context context) {
            context.navAgent.SetDestination(context.navAgent.transform.position);
        }
    }
}