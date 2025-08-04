using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Agents
{
	public class MageAgent : GoapAgent
	{
		private const string NOTHING = "Nothing";
		private const string CAN_BUILD_STAFF = "CanBuildStaff";
		private const string HAS_STAFF = "HasStaff";
		private const string CAN_BUILD_SHIELD = "CanBuildShield";
		private const string HAS_SHIELD = "HasShield";
		private const string HAS_ARTIFACT = "HasArtifact";
		
		// Individual resource finding beliefs
		private const string ENOUGH_OAK_LOGS_FOUND = "EnoughOakLogsFound";
		private const string ENOUGH_CRYSTALS_FOUND = "EnoughCrystalsFound";
		private const string ENOUGH_IRON_FOUND = "EnoughIronFound";
		
		private const string NEAR_BUILD_LOCATION = "NearBuildLocation";
		
		[SerializeField] GoapResourceManager resourceManager;
		[SerializeField] Transform buildLocation;

		protected override void Awake()
		{
			base.Awake();
			resourceManager = GoapResourceManager.Instance;
			buildLocation = BuildLocation.Instance.transform;
			navMeshAgent = GetComponent<NavMeshAgent>();
		}

		protected override void SetupBeliefs()
		{
			beliefs = new();
			BeliefFactory factory = new BeliefFactory(this, beliefs);
			
			factory.AddBelief(NOTHING, () => false);
			factory.AddBelief(CAN_BUILD_STAFF, () => resourceManager.OakLogsEffectiveCount >= 5 && resourceManager.IronIngotsEffectiveCount >= 2);
			factory.AddBelief(HAS_STAFF, () => resourceManager.EnchantedStaff);
			factory.AddBelief(CAN_BUILD_SHIELD, () => resourceManager.CrystalsEffectiveCount >= 4 && resourceManager.IronIngotsEffectiveCount >= 3);
			factory.AddBelief(HAS_SHIELD, () => resourceManager.RunedShield);
			factory.AddBelief(HAS_ARTIFACT, () => resourceManager.CombinedArtifact);
			
			// Split resource finding beliefs
			factory.AddBelief(ENOUGH_OAK_LOGS_FOUND, () => resourceManager.OakLogsFoundCount >= 5);
			factory.AddBelief(ENOUGH_CRYSTALS_FOUND, () => resourceManager.CrystalsFoundCount >= 4);
			factory.AddBelief(ENOUGH_IRON_FOUND, () => resourceManager.IronIngotsFoundCount >= 5);
			factory.AddBelief(NEAR_BUILD_LOCATION, () => Vector3.Distance(transform.position, buildLocation.position) < 5f);
		}

		protected override void SetupActions()
		{
			actions = new();
			
			actions.Add(new AgentAction.Builder("Relax")
				.WithStrategy(new IdleStrategy(5f))
				.AddEffect(beliefs[NOTHING])
				.Build());

			actions.Add(new AgentAction.Builder("Move To Build Location")
				.WithStrategy(new MoveToStrategy(navMeshAgent, buildLocation.position))
				.AddEffect(beliefs[NEAR_BUILD_LOCATION])
				.Build());
			
			// Search actions - Only when necessary
			actions.Add(new AgentAction.Builder("Search For Oak Logs")
				.WithStrategy(new WanderStrategy(navMeshAgent,20f))
				.AddEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
				.Build());
				
			actions.Add(new AgentAction.Builder("Search For Crystals")
				.WithStrategy(new WanderStrategy(navMeshAgent,20f))
				.AddEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
				.Build());
				
			actions.Add(new AgentAction.Builder("Search For Iron")
				.WithStrategy(new WanderStrategy(navMeshAgent,20f))
				.AddEffect(beliefs[ENOUGH_IRON_FOUND])
				.Build());

			// Building actions
			actions.Add(new AgentAction.Builder("Build Enchanted Staff")
				.WithStrategy(new CraftEnchantedStaffStrategy(resourceManager, 7f))
				.AddEffect(beliefs[HAS_STAFF])
				.AddPrecondition(beliefs[CAN_BUILD_STAFF])
				.AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
				.Build());
			
			actions.Add(new AgentAction.Builder("Build Runed Shield")
				.WithStrategy(new CraftRunedShieldStrategy(resourceManager, 10f))
				.AddEffect(beliefs[HAS_SHIELD])
				.AddPrecondition(beliefs[CAN_BUILD_SHIELD])
				.AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
				.Build());

			actions.Add(new AgentAction.Builder("Build Combined Artifact")
				.WithStrategy(new CraftCombinedArtifactStrategy(resourceManager, 15f))
				.AddEffect(beliefs[HAS_ARTIFACT])
				.AddPrecondition(beliefs[HAS_STAFF])
				.AddPrecondition(beliefs[HAS_SHIELD])
				.AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
				.Build());
		}

		protected override void SetupGoals()
		{
			goals = new();

			goals.Add(new AgentGoal.Builder("Build Combined Artifact")
				.WithPriority(999)
				.WithDesiredEffect(beliefs[HAS_ARTIFACT])
				.Build());

			// Resource finding goals - Very low priority for mage
			goals.Add(new AgentGoal.Builder("Find Oak Logs")
				.WithPriority(3)
				.WithDesiredEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Find Crystals")
				.WithPriority(2)
				.WithDesiredEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
				.Build());
				
			goals.Add(new AgentGoal.Builder("Find Iron")
				.WithPriority(1)
				.WithDesiredEffect(beliefs[ENOUGH_IRON_FOUND])
				.Build());
		}
	}
}