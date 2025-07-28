using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Agents
{
	[RequireComponent(typeof(Rigidbody),typeof(NavMeshAgent))]
	public class VillagerAgent : GoapAgent
	{
		[Header("Sensors")]
		[SerializeField]
		private Sensor locateResourcesSensor;

		[Header("KnownLocations")]
		[SerializeField]
		private Transform buildSiteLocation;
		[SerializeField]
		private Transform PickupResourceLocation;
		[SerializeField] Transform ForestLocation;
		[SerializeField] Transform CrystalCaveLocation;
		[SerializeField] Transform BlacksmithLocation;

		protected override void Awake()
		{
			base.Awake();
			
			if(locateResourcesSensor)
				locateResourcesSensor.AddRelevantTag("ResourcePickup");
		}

		protected override void SetupBeliefs()
		{
			beliefs = new();
			BeliefFactory factory = new BeliefFactory(this, beliefs);
			
			factory.AddBelief("Nothing",(() => false));
			
			factory.AddBelief("AgentIdle",(() => !navMeshAgent.hasPath));
			factory.AddBelief("AgentMoving",(() => navMeshAgent.hasPath));
		}

		protected override void SetupActions()
		{
			actions = new();

			actions.Add(new AgentAction.Builder("Relax")
				.WithStrategy(new IdleStrategy(5))
				.AddEffect(beliefs["Nothing"])
				.Build());

			actions.Add(new AgentAction.Builder("Wander Around")
				.WithStrategy(new WanderStrategy(navMeshAgent, 10))
				.AddEffect(beliefs["AgentMoving"])
				.Build());
		}

		protected override void SetupGoals()
		{
			goals = new();
			goals.Add(new AgentGoal.Builder("Chill Out")
				.WithPriority(1)
				.WithDesiredEffect(beliefs["Nothing"])
				.Build());

			goals.Add(new AgentGoal.Builder("Wander")
				.WithPriority(1)
				.WithDesiredEffect(beliefs["AgentMoving"])
				.Build());
		}

		
	}
}
