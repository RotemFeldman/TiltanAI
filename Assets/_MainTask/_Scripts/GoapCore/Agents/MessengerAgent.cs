using GOAP.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Agents
{
	public class MessengerAgent : GoapAgent , IResourceCarrier
	{
		public ResourcePickup TargetResourcePickup { get; set; }
		public ResourcePickup CurrentResource { get; private set; }
		
		private const string NOTHING = "Nothing";
		private const string RESOURCE_PICKUP_AVAILABLE = "hasTargetResourcePickup";
		
		// Individual resource beliefs
		private const string ENOUGH_OAK_LOGS_COLLECTED = "EnoughOakLogsCollected";
		private const string ENOUGH_CRYSTALS_COLLECTED = "EnoughCrystalsCollected";
		private const string ENOUGH_IRON_COLLECTED = "EnoughIronCollected";
		
		private const string ENOUGH_OAK_LOGS_FOUND = "EnoughOakLogsFound";
		private const string ENOUGH_CRYSTALS_FOUND = "EnoughCrystalsFound";
		private const string ENOUGH_IRON_FOUND = "EnoughIronFound";
		
		[SerializeField] GoapResourceManager resourceManager;

		protected override void Awake()
		{
			base.Awake();
			navMeshAgent = GetComponent<NavMeshAgent>();
			resourceManager = GoapResourceManager.Instance;
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
			factory.AddBelief(RESOURCE_PICKUP_AVAILABLE, () => resourceManager.HasUnreservedResource());
			
			// Split into individual resource beliefs - Collection
			factory.AddBelief(ENOUGH_OAK_LOGS_COLLECTED, () => resourceManager.OakLogsGatheredCount >= 5);
			factory.AddBelief(ENOUGH_CRYSTALS_COLLECTED, () => resourceManager.CrystalShardsGatheredCount >= 4);
			factory.AddBelief(ENOUGH_IRON_COLLECTED, () => resourceManager.IronIngotsGatheredCount >= 5);
			
			// Split into individual resource beliefs - Finding
			factory.AddBelief(ENOUGH_OAK_LOGS_FOUND, () => resourceManager.OakLogsFoundCount >= 5);
			factory.AddBelief(ENOUGH_CRYSTALS_FOUND, () => resourceManager.CrystalsFoundCount >= 4);
			factory.AddBelief(ENOUGH_IRON_FOUND, () => resourceManager.IronIngotsFoundCount >= 5);
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
				.WithCost(10)
				.AddEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
				.Build());
				
			actions.Add(new AgentAction.Builder("Search For Crystals")
				.WithStrategy(new WanderStrategy(navMeshAgent, 20))
				.WithCost(10)
				.AddEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
				.Build());
				
			actions.Add(new AgentAction.Builder("Search For Iron")
				.WithStrategy(new WanderStrategy(navMeshAgent, 20))
				.WithCost(10)
				.AddEffect(beliefs[ENOUGH_IRON_FOUND])
				.Build());
			
			// Delivery actions - Lower cost makes messengers preferred
			actions.Add(new AgentAction.Builder("Deliver Oak Logs")
				.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(TargetResourcePickup, this))
				.WithCost(1)
				.AddPrecondition(beliefs[RESOURCE_PICKUP_AVAILABLE]) 
				.AddEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
				.Build());
				
			actions.Add(new AgentAction.Builder("Deliver Crystals")
				.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(TargetResourcePickup, this))
				.WithCost(1)
				.AddPrecondition(beliefs[RESOURCE_PICKUP_AVAILABLE]) 
				.AddEffect(beliefs[ENOUGH_CRYSTALS_COLLECTED])
				.Build());
				
			actions.Add(new AgentAction.Builder("Deliver Iron")
				.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(TargetResourcePickup, this))
				.WithCost(1)
				.AddPrecondition(beliefs[RESOURCE_PICKUP_AVAILABLE]) 
				.AddEffect(beliefs[ENOUGH_IRON_COLLECTED])
				.Build());
		}

		protected override void SetupGoals()
		{
			goals = new();

			// Delivery goals - Higher priority for messengers
			goals.Add(new AgentGoal.Builder("Collect Oak Logs")
				.WithPriority(60)
				.WithDesiredEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Collect Crystals")
				.WithPriority(50)
				.WithDesiredEffect(beliefs[ENOUGH_CRYSTALS_COLLECTED])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Collect Iron")
				.WithPriority(40)
				.WithDesiredEffect(beliefs[ENOUGH_IRON_COLLECTED])
				.Build());
			
			// Finding goals - Lower priority for messengers
			goals.Add(new AgentGoal.Builder("Find Oak Logs")
				.WithPriority(20)
				.WithDesiredEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Find Crystals")
				.WithPriority(15)
				.WithDesiredEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Find Iron")
				.WithPriority(10)
				.WithDesiredEffect(beliefs[ENOUGH_IRON_FOUND])
				.Build());
			
			goals.Add(new AgentGoal.Builder("Do Nothing")
				.WithPriority(1)
				.WithDesiredEffect(beliefs[NOTHING])
				.Build());
		}
	}
}