using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
	public abstract class GoapAgent : MonoBehaviour
	{
		public bool IsAvailable => currentAction == null || currentAction.Name == "Relax";
		
		protected NavMeshAgent navMeshAgent;
		protected Rigidbody rb;

		protected GameObject target;
		protected Vector3 destination;

		protected AgentGoal lastGoal;
		public AgentGoal currentGoal;
		public ActionPlan actionPlan;
		public AgentAction currentAction;
		
		public Dictionary<string, AgentBelief> beliefs;
		public HashSet<AgentAction> actions;
		public HashSet<AgentGoal> goals;
		
		protected IGoapPlanner gPlanner;
		
		protected virtual void Awake()
		{
			// navMeshAgent = GetComponent<NavMeshAgent>();
			// rb = GetComponent<Rigidbody>();
			// if (navMeshAgent == null)
			// 	Debug.LogWarning("Goap agent requires a NavMeshAgent component");
			// if (rb == null)
			// 	Debug.LogWarning("Goap agent requires a Rigidbody component");
			//
			// rb.freezeRotation = true;
			gPlanner = new GoapPlanner();
		}
		
		protected virtual void Start()
		{
			SetupBeliefs();
			SetupActions();
			SetupGoals();
		}

		protected abstract void SetupBeliefs();
		protected abstract void SetupActions();
		protected abstract void SetupGoals();
		
		protected virtual void Update()
		{
			if (currentAction == null)
			{
				Debug.Log($"{name}: Calculating any potential new plan");
				CalculatePlane();

				if (actionPlan != null && actionPlan.Actions.Count > 0)
				{
					//navMeshAgent.ResetPath();

					currentGoal = actionPlan.AgentGoal;
					currentAction = actionPlan.Actions.Pop();
					currentAction.Start();
				}
			}

			if (actionPlan != null && currentAction != null)
			{
				currentAction.Update(Time.deltaTime);

				if (currentAction.Complete)
				{
					Debug.Log($"{name}: Action complete");
					currentAction.Stop();
					currentAction = null;

					if (actionPlan.Actions.Count == 0)
					{
						Debug.Log($"{name}: Plan complete");
						lastGoal = currentGoal;
						currentGoal = null;
					}
				}
			}
		}

		// private void CalculatePlane()
		// {
		// 	var priorityLevel = currentGoal?.Priority ?? 0;
		//
		// 	HashSet<AgentGoal> goalsToCheck = goals;
		//
		// 	if (currentGoal != null)
		// 	{
		// 		Debug.Log($"{name}: Current goal exists, cheking goals with higher priority");
		// 		goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
		// 	}
		// 	
		// 	var potentialPlan = gPlanner.Plan(this, goalsToCheck, lastGoal);
		// 	if (potentialPlan != null)
		// 	{
		// 		actionPlan = potentialPlan;
		// 	}
		// }
		
		private void CalculatePlane()
		{
			var priorityLevel = currentGoal?.Priority ?? 0;

			HashSet<AgentGoal> goalsToCheck = goals;

			if (currentGoal != null)
			{
				Debug.Log($"{name}: Current goal exists, checking goals with higher priority");
				var higherPriorityGoals = goals.Where(g => g.Priority > priorityLevel).ToHashSet();
        
				// If no higher priority goals exist, use all goals
				goalsToCheck = higherPriorityGoals.Count > 0 ? higherPriorityGoals : goals;
			}
    
			// Add null check to prevent the ArgumentNullException
			if (goalsToCheck != null && goalsToCheck.Count > 0)
			{
				var potentialPlan = gPlanner.Plan(this, goalsToCheck, lastGoal);
				if (potentialPlan != null)
				{
					actionPlan = potentialPlan;
				}
			}
			else
			{
				Debug.LogWarning($"{name}: No goals available for planning");
			}
		}

	}
}