using System.Collections.Generic;
using System.Linq;
using GOAP.Interfaces;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

namespace GOAP.Agents
{
	public class MessengerAgent : GoapAgent , IResourceCarrier
	{
	 	[field:SerializeField] public ResourcePickup TargetResourcePickup { get; set; }
		[field:SerializeField] public ResourcePickup CurrentResource { get; private set; }
		
		private const string NOTHING = "Nothing";
		private const string RESOURCE_PICKUP_AVAILABLE = "hasTargetResourcePickup";
		private const string HOLDING_RESOURCE = "holdingResource";
		
		// Individual resource beliefs
		private const string ENOUGH_OAK_LOGS_COLLECTED = "EnoughOakLogsCollected";
		private const string ENOUGH_CRYSTALS_COLLECTED = "EnoughCrystalsCollected";
		private const string ENOUGH_IRON_COLLECTED = "EnoughIronCollected";
		
		private const string ENOUGH_OAK_LOGS_FOUND = "EnoughOakLogsFound";
		private const string ENOUGH_CRYSTALS_FOUND = "EnoughCrystalsFound";
		private const string ENOUGH_IRON_FOUND = "EnoughIronFound";
		
		private const string NEAR_BUILD_LOCATION = "NearBuildLocation";
		private const string NEAR_PICKUP_LOCATION = "NearPickupLocation";
		
		[SerializeField] GoapResourceManager resourceManager;
		[SerializeField] Transform buildLocation;

		protected override void Awake()
		{
			base.Awake();
			navMeshAgent = GetComponent<NavMeshAgent>();
			resourceManager = GoapResourceManager.Instance;
			buildLocation = BuildLocation.Instance.transform;
		}
		
		public void PickupResource(ResourcePickup resource)
		{
			Debug.Log("msg pickedup");
			CurrentResource = resource;
			resource.gameObject.SetActive(false);
			TargetResourcePickup = null;
			CurrentResource.gameObject.SetActive(false);
		}

		protected override void Update()
		{
			base.Update();

			if (TargetResourcePickup != null)
			{
				if (Vector3.Distance(transform.position, TargetResourcePickup.Position) < 2f)
				{
					PickupResource(TargetResourcePickup);
				}
			}
			
			if (CurrentResource != null)
			{
				if (Vector3.Distance(transform.position, CurrentResource.Position) < 1f)
				{
					DropResource();
				}
			}
			
		}

		public void DropResource()
		{
			GoapResourceManager.Instance.AddGatheredResource(CurrentResource.ResourceType);
			Destroy(CurrentResource.gameObject);
			CurrentResource = null;
		}

		protected override void SetupBeliefs()
		{
			beliefs = new();
			BeliefFactory factory = new BeliefFactory(this, beliefs);
			
			factory.AddBelief(NOTHING, () => false);
			factory.AddBelief(RESOURCE_PICKUP_AVAILABLE, () => TargetResourcePickup != null);
			factory.AddBelief(HOLDING_RESOURCE, () => CurrentResource != null);;
			
			// Split into individual resource beliefs - Collection
			factory.AddBelief(ENOUGH_OAK_LOGS_COLLECTED, () => resourceManager.OakLogsGatheredCount >= 5);
			factory.AddBelief(ENOUGH_CRYSTALS_COLLECTED, () => resourceManager.CrystalShardsGatheredCount >= 4);
			factory.AddBelief(ENOUGH_IRON_COLLECTED, () => resourceManager.IronIngotsGatheredCount >= 5);
			
			// Split into individual resource beliefs - Finding
			factory.AddBelief(ENOUGH_OAK_LOGS_FOUND, () => resourceManager.OakLogsFoundCount >= 5);
			factory.AddBelief(ENOUGH_CRYSTALS_FOUND, () => resourceManager.CrystalsFoundCount >= 4);
			factory.AddBelief(ENOUGH_IRON_FOUND, () => resourceManager.IronIngotsFoundCount >= 5);
			
			factory.AddBelief(NEAR_BUILD_LOCATION, () => Vector3.Distance(buildLocation.position, transform.position) < 5f);
			factory.AddBelief(NEAR_PICKUP_LOCATION,(() => TargetResourcePickup != null && Vector3.Distance(TargetResourcePickup.transform.position, transform.position) < 2f) );
		}

		protected override void SetupActions()
		{
			actions = new();

			actions.Add(new AgentAction.Builder("Relax")
				.WithStrategy(new IdleStrategy(5f))
				.WithCost(15)
				.AddEffect(beliefs[NOTHING])
				.Build());
			
			// Search actions
			actions.Add(new AgentAction.Builder("Search For Oak Logs")
				.WithStrategy(new WanderStrategy(navMeshAgent,30f))
				.WithCost(10)
				.AddEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
				.Build());
				
			// actions.Add(new AgentAction.Builder("Search For Crystals")
			// 	.WithStrategy(new MessengerSearchForResourcesStrategy(this))
			// 	.WithCost(10)
			// 	.AddEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
			// 	.Build());
			// 	
			// actions.Add(new AgentAction.Builder("Search For Iron")
			// 	.WithStrategy(new MessengerSearchForResourcesStrategy(this))
			// 	.WithCost(10)
			// 	.AddEffect(beliefs[ENOUGH_IRON_FOUND])
			// 	.Build());
			
			// Delivery actions - Lower cost makes messengers preferred
			actions.Add(new AgentAction.Builder("Request Pickup Target")
				.WithStrategy(new RequestPickupStrategy<MessengerAgent>(this))
				.AddEffect(beliefs[RESOURCE_PICKUP_AVAILABLE])
				.Build());
			
			actions.Add(new AgentAction.Builder("Go To Pickup Location")
				.WithStrategy(new MoveToPickupStrategy<MessengerAgent>(navMeshAgent, this))
				.AddPrecondition(beliefs[RESOURCE_PICKUP_AVAILABLE])
				.AddEffect(beliefs[NEAR_PICKUP_LOCATION])
				.Build());
			
			actions.Add((new AgentAction.Builder("Pickup Resource"))
				.WithStrategy(new PickupResourceStrategy<MessengerAgent>(this))
				.AddPrecondition(beliefs[NEAR_PICKUP_LOCATION])
				.AddEffect(beliefs[HOLDING_RESOURCE])
				.Build());

			actions.Add(new AgentAction.Builder("Go To Build Location")
				.WithStrategy(new MoveToStrategy(navMeshAgent, buildLocation.position))
				.AddEffect(beliefs[NEAR_BUILD_LOCATION])
				.Build());
			
			actions.Add(new AgentAction.Builder("Deliver Resource")
				.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(this))
				.WithCost(1)
				.AddPrecondition(beliefs[NEAR_BUILD_LOCATION]) 
				.AddPrecondition(beliefs[HOLDING_RESOURCE])
				.AddEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
				.AddEffect(beliefs[ENOUGH_CRYSTALS_COLLECTED])
				.AddEffect(beliefs[ENOUGH_IRON_COLLECTED])
				.Build());
			

				
			// actions.Add(new AgentAction.Builder("Deliver Crystals")
			// 	.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(TargetResourcePickup, this))
			// 	.WithCost(1)
			// 	.AddPrecondition(beliefs[RESOURCE_PICKUP_AVAILABLE]) 
			// 	.AddEffect(beliefs[ENOUGH_CRYSTALS_COLLECTED])
			// 	.Build());
			// 	
			// actions.Add(new AgentAction.Builder("Deliver Iron")
			// 	.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(TargetResourcePickup, this))
			// 	.WithCost(1)
			// 	.AddPrecondition(beliefs[RESOURCE_PICKUP_AVAILABLE]) 
			// 	.AddEffect(beliefs[ENOUGH_IRON_COLLECTED])
			// 	.Build());
		}

		protected override void SetupGoals()
		{
			goals = new HashSet<AgentGoal>(); // Ensure it's initialized

			// Add null check for beliefs
			if (beliefs == null)
			{
				Debug.LogError($"{name}: Beliefs not initialized before SetupGoals");
				return;
			}


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
			
			Debug.Log($"{name}: Initialized {goals.Count} goals");
    
			// Debug each goal's current state
			foreach (var goal in goals)
			{
				bool isAlreadySatisfied = !goal.DesiredEffects.Any(b => !b.Evaluate());
				Debug.Log($"{name}: Goal '{goal.Name}' - Priority: {goal.Priority}, Already Satisfied: {isAlreadySatisfied}");
        
				foreach (var effect in goal.DesiredEffects)
				{
					Debug.Log($"{name}: - Effect '{effect.Name}': {effect.Evaluate()}");
				}
			}

		}
	}
}