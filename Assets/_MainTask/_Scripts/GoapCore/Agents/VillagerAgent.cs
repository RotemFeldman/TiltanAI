using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using _MainTask._Scripts;
using GOAP.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Agents
{
	public class VillagerAgent : GoapAgent , IResourceCarrier
	{
		public ResourcePickup TargetResourcePickup { get; set; }
		public ResourcePickup CurrentResource { get; private set; }
		
		private const string NOTHING = "Nothing";
		private const string HAS_TARGET_RESOURCE_PICKUP = "hasTargetResourcePickup";
		
		// Individual resource beliefs
		private const string ENOUGH_OAK_LOGS_FOUND = "EnoughOakLogsFound";
		private const string ENOUGH_CRYSTALS_FOUND = "EnoughCrystalsFound";
		private const string ENOUGH_IRON_FOUND = "EnoughIronFound";
		
		private const string ENOUGH_OAK_LOGS_COLLECTED = "EnoughOakLogsCollected";
		private const string ENOUGH_CRYSTALS_COLLECTED = "EnoughCrystalsCollected";
		private const string ENOUGH_IRON_COLLECTED = "EnoughIronCollected";
		
		[SerializeField] GoapResourceManager resourceManager;

		protected override void Awake()
		{
			base.Awake();
			resourceManager = GoapResourceManager.Instance;
			navMeshAgent = GetComponent<NavMeshAgent>();
		}

		public override void CompleteTask()
		{
			TargetResourcePickup = null;
			base.CompleteTask();
		}

		public void PickupResource(ResourcePickup resource)
		{
			CurrentResource = resource;
		}

		public void DropResource()
		{
			CurrentResource = null;
		}

		protected override void SetupBeliefs()
		{
			beliefs = new();
			BeliefFactory factory = new BeliefFactory(this, beliefs);
			
			factory.AddBelief(NOTHING, () => false);
			factory.AddBelief(HAS_TARGET_RESOURCE_PICKUP, () => resourceManager.HasUnreservedResource()); 
			
			// Split into individual resource beliefs - Finding
			factory.AddBelief(ENOUGH_OAK_LOGS_FOUND, () => resourceManager.OakLogsFoundCount >= 5);
			factory.AddBelief(ENOUGH_CRYSTALS_FOUND, () => resourceManager.CrystalsFoundCount >= 4);
			factory.AddBelief(ENOUGH_IRON_FOUND, () => resourceManager.IronIngotsFoundCount >= 5);
			
			// Split into individual resource beliefs - Collection
			factory.AddBelief(ENOUGH_OAK_LOGS_COLLECTED, () => resourceManager.OakLogsGatheredCount >= 5);
			factory.AddBelief(ENOUGH_CRYSTALS_COLLECTED, () => resourceManager.CrystalShardsGatheredCount >= 4);
			factory.AddBelief(ENOUGH_IRON_COLLECTED, () => resourceManager.IronIngotsGatheredCount >= 5);
		}

		protected override void SetupActions()
		{
			actions = new();

			actions.Add(new AgentAction.Builder("Relax")
				.WithStrategy(new IdleStrategy(5))
				.WithCost(15)
				.AddEffect(beliefs[NOTHING])
				.Build());

			// Search actions
			actions.Add(new AgentAction.Builder("Search For Oak Logs")
				.WithStrategy(new WanderStrategy(navMeshAgent, 20))
				.AddEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
				.Build());
				
			actions.Add(new AgentAction.Builder("Search For Crystals")
				.WithStrategy(new WanderStrategy(navMeshAgent, 20))
				.AddEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
				.Build());
				
			actions.Add(new AgentAction.Builder("Search For Iron")
				.WithStrategy(new WanderStrategy(navMeshAgent, 20))
				.AddEffect(beliefs[ENOUGH_IRON_FOUND])
				.Build());

			// Delivery actions - Higher cost makes villagers less preferred
			actions.Add(new AgentAction.Builder("Deliver Oak Logs")
				.WithStrategy(new DeliverResourceStrategy<VillagerAgent>(TargetResourcePickup, this))
				.WithCost(10)
				.AddPrecondition(beliefs[HAS_TARGET_RESOURCE_PICKUP]) 
				.AddEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
				.Build());
				
			actions.Add(new AgentAction.Builder("Deliver Crystals")
				.WithStrategy(new DeliverResourceStrategy<VillagerAgent>(TargetResourcePickup, this))
				.WithCost(10)
				.AddPrecondition(beliefs[HAS_TARGET_RESOURCE_PICKUP]) 
				.AddEffect(beliefs[ENOUGH_CRYSTALS_COLLECTED])
				.Build());
				
			actions.Add(new AgentAction.Builder("Deliver Iron")
				.WithStrategy(new DeliverResourceStrategy<VillagerAgent>(TargetResourcePickup, this))
				.WithCost(10)
				.AddPrecondition(beliefs[HAS_TARGET_RESOURCE_PICKUP]) 
				.AddEffect(beliefs[ENOUGH_IRON_COLLECTED])
				.Build());
		}

		protected override void SetupGoals()
		{
			goals = new();

			// Delivery goals - Lower priority for villagers
			goals.Add(new AgentGoal.Builder("Collect Oak Logs")
				.WithPriority(20)
				.WithDesiredEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Collect Crystals")
				.WithPriority(15)
				.WithDesiredEffect(beliefs[ENOUGH_CRYSTALS_COLLECTED])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Collect Iron")
				.WithPriority(10)
				.WithDesiredEffect(beliefs[ENOUGH_IRON_COLLECTED])
				.Build());

			// Finding goals - Higher priority for villagers
			goals.Add(new AgentGoal.Builder("Find Oak Logs")
				.WithPriority(60)
				.WithDesiredEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Find Crystals")
				.WithPriority(50)
				.WithDesiredEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Find Iron")
				.WithPriority(40)
				.WithDesiredEffect(beliefs[ENOUGH_IRON_FOUND])
				.Build());

			goals.Add(new AgentGoal.Builder("Do Nothing")
				.WithPriority(1)
				.WithDesiredEffect(beliefs[NOTHING])
				.Build());
		}
	}
}