using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace UtilityAI {
    [RequireComponent(typeof(NavMeshAgent), typeof(Sensor))]
    public class Brain : MonoBehaviour {
        public List<AIAction> actions;
        public Context context;
        private float timeOfTheDay = 0;
        
        
        void Awake() {
            
            context = new Context(this);
            

            foreach (var action in actions) {
                action.Initialize(context);
            }
        }

        void Update()
        {
            UpdateTimeOfDay();
            UpdateContext();
            
            AIAction bestAction = null;
            float highestUtility = float.MinValue;

            foreach (var action in actions) {
                float utility = action.CalculateUtility(context);
                if (utility > highestUtility) {
                    highestUtility = utility;
                    bestAction = action;
                }
            }

            if (bestAction != null) {
                bestAction.Execute(context);
            }
        }

        private void UpdateTimeOfDay()
        {
            timeOfTheDay+= 0.01f;
            // Simulate time passing
        }

        void UpdateContext()
        {
            // update context with current state
            Agent Agent = GetComponent<Agent>();
            if (Agent != null)
            {
                context.SetData("Health",Agent.CurrentHealth );
            }
        }
    }
}