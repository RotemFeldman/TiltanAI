using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace _MainTask._Scripts.GoapCore.Agents
{
    public class VillagerAgent : MonoBehaviour, IGoapAgent
    {
        [Header("Agent Settings")]
        public string agentName = "Villager";
        
        [Header("Current State")]
        [SerializeField] private VillagerActions currentAction = VillagerActions.Idle;
        [SerializeField] private bool actionComplete = false;
        
        private IGoapAction assignedGoapAction;
        private bool isActionComplete;
        
        private VillagerSearchForResourcesAction searchForResourcesAction;
        private VillagerChopTreeAction chopTreeAction;
        private VillagerRefineCrystalsAction refineCrystalsAction;
        private VillagerCollectIronIngotAction collectIronIngotAction;
        private VillagerMarkResourceForPickupAction markResourceForPickupAction;
        private VillagerCarryResourceToBuildAction carryResourceToBuildAction;
        private VillagerIdleAction idleAction;

        private void Start()
        {
            searchForResourcesAction = new VillagerSearchForResourcesAction();
            chopTreeAction = new VillagerChopTreeAction();
            refineCrystalsAction = new VillagerRefineCrystalsAction();
            collectIronIngotAction = new VillagerCollectIronIngotAction();
            markResourceForPickupAction = new VillagerMarkResourceForPickupAction();
            carryResourceToBuildAction = new VillagerCarryResourceToBuildAction();
            idleAction = new VillagerIdleAction();
        }

        public string GetName()
        {
            return agentName;
        }

        public bool IsAvailable()
        {
            return assignedGoapAction == null && currentAction == VillagerActions.Idle;
        }

        public bool CanPerformAction(IGoapAction action)
        {
            return action.GetName().Contains("SearchForResources") 
                   || action.GetName().Contains("ChopTree")
                   || action.GetName().Contains("RefineCrystals")
                   || action.GetName().Contains("CollectIronIngot")
                   || action.GetName().Contains("MarkResourceForPickup")
                   || action.GetName().Contains("CarryResourceToBuild")
                   || action.GetName().Contains("Idle");
        }

        public void AssignAction(IGoapAction action)
        {
            if (IsAvailable())
            {
                assignedGoapAction = action;
                isActionComplete = false;
                actionComplete = false;
                
                if (action is VillagerSearchForResourcesAction searchAction)
                {
                    
                }
                
                Debug.Log($"[{agentName}] ASSIGNED GOAP ACTION: {action.GetName()}");
                
                // Execute the GOAP action (this will set our currentAction enum)
                action.Execute(new GoapState());
            }
        }

        public bool IsActionComplete()
        {
            return isActionComplete;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public List<IGoapAction> GetAvailableActions()
        {
            var actions = new List<IGoapAction>();
            
            // Add search action
            actions.Add(searchForResourcesAction);
            
            // Add other villager actions as needed
            // var chopTreeAction = new VillagerChopTreeAction();
            // actions.Add(chopTreeAction);
            
            return actions;
        }
        
        // Methods for the behavior graph to interact with
        public void SetCurrentAction(VillagerActions action)
        {
            currentAction = action;
            actionComplete = false;
            
            Debug.Log($"[{agentName}] Current action set to: {action}");
        }
        
        public VillagerActions GetCurrentAction()
        {
            return currentAction;
        }
        
        public bool HasCompletedCurrentAction()
        {
            return actionComplete;
        }
        
        public void CompleteCurrentAction()
        {
            actionComplete = true;
            isActionComplete = true;
            
            Debug.Log($"[{agentName}] COMPLETED: {currentAction}");
            
            // Reset to idle
            currentAction = VillagerActions.Idle;
            
            // Reset the GOAP action
            if (assignedGoapAction != null)
            {
                if (assignedGoapAction is VillagerSearchForResourcesAction searchAction)
                {
                    searchAction.Reset();
                }
                assignedGoapAction = null;
            }
        }
        
        // Debug visualization
        void OnDrawGizmosSelected()
        {
            // Draw search radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position,10f);
            
            // Draw current action status
            if (currentAction != VillagerActions.Idle)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            }
        }
    }
}