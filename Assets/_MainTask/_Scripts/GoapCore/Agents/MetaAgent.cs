// using System.Collections.Generic;
// using System.Linq;
// using System.Resources;
// using _MainTask._Scripts;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// namespace GOAP.Agents
// {
// 	// OLD - DONT USE
// 	public class MetaAgent : GoapAgent
// 	{
// 		[Header("Agents")]
// 		[SerializeField] private List<VillagerAgent> villagers = new();
// 		[SerializeField] private List<MessengerAgent> messengers = new();
// 		[SerializeField] private MageAgent mage;
// 		
// 		[SerializeField] GoapResourceManager resourceManager;
// 		[SerializeField] Transform buildSiteLocation;
// 		
// 		// Belief consts
// 		private const string COMBINED_ARTIFACT_BUILT = "Combined Artifact Built";
// 		private const string ENCHANTED_STAFF_BUILT = "Enchanted Staff Built";
// 		private const string RUNED_SHIELD_BUILT = "Runed Shield Built";
// 		
// 		// Resources collection beliefs
// 		private const string ENOUGH_OAK_LOGS_COLLECTED = "Enough Oak Logs Collected";
// 		private const string ENOUGH_CRYSTAL_SHARDS_COLLECTED = "Enough Crystal Shards Collected";
// 		private const string IRON_INGOTS_FOR_STAFF_COLLECTED = "Iron Ingots For Staff Delivered";
// 		private const string IRON_INGOTS_FOR_SHIELD_COLLECTED = "Iron Ingots For Shield Delivered";
//
// 		// Agent availability beliefs
// 		private const string VILLAGERS_AVAILABLE = "Villagers Available";
// 		private const string MESSENGERS_AVAILABLE = "Messengers Available";
// 		private const string MAGE_AVAILABLE = "Mage Available";
//
// 		// Resource preparation beliefs
// 		private const string UNRESERVED_OAK_LOG_PICKUP = "Unreserved Oak Log Pickup";
// 		private const string UNRESERVED_CRYSTAL_SHARD_PICKUP= "Unreserved Crystal Shards Pickup";
// 		private const string UNRESERVED_IRON_INGOT_PICKUP = "Unreserved Iron Ingots Pickup";
// 		
// 		protected override void SetupBeliefs()
// 		{
// 			beliefs = new();
// 			BeliefFactory factory = new BeliefFactory(this, beliefs);
// 			
// 			factory.AddBelief(COMBINED_ARTIFACT_BUILT, () => resourceManager.CombinedArtifact);
// 			factory.AddBelief(ENCHANTED_STAFF_BUILT, () => resourceManager.EnchantedStaff);
// 			factory.AddBelief(RUNED_SHIELD_BUILT, () => resourceManager.RunedShield);
// 			
// 			factory.AddBelief(ENOUGH_OAK_LOGS_COLLECTED, () => resourceManager.OakLogsGatheredCount >= 5);
// 			factory.AddBelief(ENOUGH_CRYSTAL_SHARDS_COLLECTED, () => resourceManager.CrystalShardsGatheredCount >= 4);
// 			factory.AddBelief(IRON_INGOTS_FOR_STAFF_COLLECTED, () => resourceManager.IronIngotsEffectiveCount >= 3);
// 			factory.AddBelief(IRON_INGOTS_FOR_SHIELD_COLLECTED, () => resourceManager.IronIngotsEffectiveCount >= 2);
// 			
// 			factory.AddBelief(VILLAGERS_AVAILABLE, () => GetAvailableVillagers().Count > 0);
// 			//factory.AddBelief(MESSENGERS_AVAILABLE, () => GetAvailableMessengers().Count > 0);
// 			factory.AddBelief(MAGE_AVAILABLE, () => mage.IsAvailable);
// 			
// 			factory.AddBelief(UNRESERVED_OAK_LOG_PICKUP,() => resourceManager.HasUnreservedResource(ResourceType.OakLog));
// 			factory.AddBelief(UNRESERVED_CRYSTAL_SHARD_PICKUP,(() => resourceManager.HasUnreservedResource(ResourceType.CrystalShard)));
// 			factory.AddBelief(UNRESERVED_IRON_INGOT_PICKUP,(() => resourceManager.HasUnreservedResource(ResourceType.IronIngot)));
// 		}
//
// 		protected override void SetupActions()
// 		{
// 			actions = new();
//
// 			// Artifacts
// 			actions.Add(new AgentAction.Builder("Craft Combined Artifact")
// 				.WithStrategy(new CraftCombinedArtifactStrategy(resourceManager,15f))
// 				.AddPrecondition(beliefs[COMBINED_ARTIFACT_BUILT])
// 				.AddPrecondition(beliefs[RUNED_SHIELD_BUILT])
// 				.AddPrecondition(beliefs[MAGE_AVAILABLE])
// 				.AddEffect(beliefs[COMBINED_ARTIFACT_BUILT])
// 				.Build());
//
// 			actions.Add(new AgentAction.Builder("Craft Enchanted Staff")
// 				.WithStrategy(new CraftEnchantedStaffStrategy(resourceManager, 7f))
// 				.AddPrecondition(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
// 				.AddPrecondition(beliefs[IRON_INGOTS_FOR_STAFF_COLLECTED])
// 				.AddPrecondition(beliefs[MAGE_AVAILABLE])
// 				.AddEffect(beliefs[ENCHANTED_STAFF_BUILT])
// 				.Build());
//
// 			actions.Add(new AgentAction.Builder("Craft Runed Shield")
// 				.WithStrategy(new CraftRunedShieldStrategy(resourceManager, 10f))
// 				.AddPrecondition(beliefs[ENOUGH_CRYSTAL_SHARDS_COLLECTED])
// 				.AddPrecondition(beliefs[IRON_INGOTS_FOR_SHIELD_COLLECTED])
// 				.AddPrecondition(beliefs[MAGE_AVAILABLE])
// 				.AddEffect(beliefs[RUNED_SHIELD_BUILT])
// 				.Build());
// 			
// 			// Resources Delivery
// 			actions.Add(new AgentAction.Builder("Villager Deliver Oak Log")
// 				//.WithStrategy(new DeliverResourceStrategy<VillagerAgent>(ResourceType.OakLog, this))
// 				.WithCost(10)
// 				.AddPrecondition(beliefs[UNRESERVED_OAK_LOG_PICKUP])
// 				.AddPrecondition(beliefs[VILLAGERS_AVAILABLE])
// 				.AddEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
// 				.Build());
// 			
// 			actions.Add(new AgentAction.Builder("Villager Deliver Crystal Shard")
// 				//.WithStrategy(new DeliverResourceStrategy<VillagerAgent>(ResourceType.CrystalShard, this))
// 				.WithCost(10)
// 				.AddPrecondition(beliefs[UNRESERVED_CRYSTAL_SHARD_PICKUP])
// 				.AddPrecondition(beliefs[VILLAGERS_AVAILABLE])
// 				.AddEffect(beliefs[ENOUGH_CRYSTAL_SHARDS_COLLECTED])
// 				.Build());
// 			
// 			actions.Add(new AgentAction.Builder("Villager Deliver Iron Ingot")
// 			//	.WithStrategy(new DeliverResourceStrategy<VillagerAgent>(ResourceType.IronIngot, this))
// 				.WithCost(10)
// 				.AddPrecondition(beliefs[UNRESERVED_IRON_INGOT_PICKUP])
// 				.AddPrecondition(beliefs[VILLAGERS_AVAILABLE])
// 				.AddEffect(beliefs[IRON_INGOTS_FOR_SHIELD_COLLECTED])
// 				.AddEffect(beliefs[IRON_INGOTS_FOR_STAFF_COLLECTED])
// 				.Build());
//
// 			// actions.Add(new AgentAction.Builder("Messenger Deliver Oak Log")
// 			// 	.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(ResourceType.OakLog, this))
// 			// 	.AddPrecondition(beliefs[UNRESERVED_OAK_LOG_PICKUP])
// 			// 	.AddPrecondition(beliefs[MESSENGERS_AVAILABLE])
// 			// 	.AddEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
// 			// 	.Build());
// 			//
// 			// actions.Add(new AgentAction.Builder("Messenger Deliver Crystal Shard")
// 			// 	.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(ResourceType.CrystalShard, this))
// 			// 	.AddPrecondition(beliefs[UNRESERVED_CRYSTAL_SHARD_PICKUP])
// 			// 	.AddPrecondition(beliefs[MESSENGERS_AVAILABLE])
// 			// 	.AddEffect(beliefs[ENOUGH_CRYSTAL_SHARDS_COLLECTED])
// 			// 	.Build());
// 			//
// 			// actions.Add(new AgentAction.Builder("Messenger Deliver Iron Ingot")
// 			// 	.WithStrategy(new DeliverResourceStrategy<MessengerAgent>(ResourceType.IronIngot, this))
// 			// 	.AddPrecondition(beliefs[UNRESERVED_IRON_INGOT_PICKUP])
// 			// 	.AddPrecondition(beliefs[MESSENGERS_AVAILABLE])
// 			// 	.AddEffect(beliefs[IRON_INGOTS_FOR_SHIELD_COLLECTED])
// 			// 	.AddEffect(beliefs[IRON_INGOTS_FOR_STAFF_COLLECTED])
// 			// 	.Build());
// 			
// 			// Search for resources
// 			actions.Add(new AgentAction.Builder("Villager Search for Oak Logs")
// 				//.WithStrategy(new SearchForResourceStrategy<VillagerAgent>(ResourceType.OakLog, this))
// 				.WithCost(1)
// 				.AddPrecondition(beliefs[VILLAGERS_AVAILABLE])
// 				.AddEffect(beliefs[UNRESERVED_OAK_LOG_PICKUP])
// 				.Build());
// 			
// 			actions.Add(new AgentAction.Builder("Villager Search for Crystal Shard")
// 			//	.WithStrategy(new SearchForResourceStrategy<VillagerAgent>(ResourceType.CrystalShard, this))
// 				.WithCost(1)
// 				.AddPrecondition(beliefs[VILLAGERS_AVAILABLE])
// 				.AddEffect(beliefs[UNRESERVED_CRYSTAL_SHARD_PICKUP])
// 				.Build());
// 			
// 			actions.Add(new AgentAction.Builder("Villager Search for Iron Ingot")
// 				//.WithStrategy(new SearchForResourceStrategy<VillagerAgent>(ResourceType.IronIngot, this))
// 				.WithCost(1)
// 				.AddPrecondition(beliefs[VILLAGERS_AVAILABLE])
// 				.AddEffect(beliefs[UNRESERVED_IRON_INGOT_PICKUP])
// 				.Build());
// 			
// 			actions.Add(new AgentAction.Builder("Messenger Search for Oak Logs")
// 				//.WithStrategy(new SearchForResourceStrategy<MessengerAgent>(ResourceType.OakLog, this))
// 				.WithCost(10)
// 				.AddPrecondition(beliefs[MESSENGERS_AVAILABLE])
// 				.AddEffect(beliefs[UNRESERVED_OAK_LOG_PICKUP])
// 				.Build());
// 			
// 			// actions.Add(new AgentAction.Builder("Messenger Search for Crystal Shard")
// 			// 	.WithStrategy(new SearchForResourceStrategy<MessengerAgent>(ResourceType.CrystalShard, this))
// 			// 	.WithCost(10)
// 			// 	.AddPrecondition(beliefs[MESSENGERS_AVAILABLE])
// 			// 	.AddEffect(beliefs[UNRESERVED_CRYSTAL_SHARD_PICKUP])
// 			// 	.Build());
// 			//
// 			// actions.Add(new AgentAction.Builder("Messenger Search for Iron Ingot")
// 			// 	.WithStrategy(new SearchForResourceStrategy<MessengerAgent>(ResourceType.IronIngot, this))
// 			// 	.WithCost(10)
// 			// 	.AddPrecondition(beliefs[MESSENGERS_AVAILABLE])
// 			// 	.AddEffect(beliefs[UNRESERVED_IRON_INGOT_PICKUP])
// 			// 	.Build());
// 			//
// 			// actions.Add(new AgentAction.Builder("Mage Search for Oak Logs")
// 			// 	.WithStrategy(new SearchForResourceStrategy<MageAgent>(ResourceType.OakLog, this))
// 			// 	.WithCost(50)
// 			// 	.AddPrecondition(beliefs[MAGE_AVAILABLE])
// 			// 	.AddEffect(beliefs[UNRESERVED_OAK_LOG_PICKUP])
// 			// 	.Build());
// 			//
// 			// actions.Add(new AgentAction.Builder("Mage Search for Crystal Shard")
// 			// 	.WithStrategy(new SearchForResourceStrategy<MageAgent>(ResourceType.CrystalShard, this))
// 			// 	.WithCost(50)
// 			// 	.AddPrecondition(beliefs[MAGE_AVAILABLE])
// 			// 	.AddEffect(beliefs[UNRESERVED_CRYSTAL_SHARD_PICKUP])
// 			// 	.Build());
// 			//
// 			// actions.Add(new AgentAction.Builder("Mage Search for Iron Ingot")
// 			// 	.WithStrategy(new SearchForResourceStrategy<MageAgent>(ResourceType.IronIngot, this))
// 			// 	.WithCost(50)
// 			// 	.AddPrecondition(beliefs[MAGE_AVAILABLE])
// 			// 	.AddEffect(beliefs[UNRESERVED_IRON_INGOT_PICKUP])
// 			// 	.Build());
// 		}
//
// 		protected override void SetupGoals()
// 		{
// 			goals = new();
//
// 			// Artifacts
// 			goals.Add(new AgentGoal.Builder("Create Combined Magical Artifact")
// 				.WithPriority(9999)
// 				.WithDesiredEffect(beliefs[COMBINED_ARTIFACT_BUILT])
// 				.Build());
//
// 			goals.Add(new AgentGoal.Builder("Build Enchanted Staff")
// 				.WithPriority(800)
// 				.WithDesiredEffect(beliefs[ENCHANTED_STAFF_BUILT])
// 				.Build());
//
// 			goals.Add(new AgentGoal.Builder("Build Runed Shield")
// 				.WithPriority(800)
// 				.WithDesiredEffect(beliefs[RUNED_SHIELD_BUILT])
// 				.Build());
//
// 			// Resources Delivery
// 			goals.Add(new AgentGoal.Builder("Collect Enough Oak Logs")
// 				.WithPriority(500)
// 				.WithDesiredEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
// 				.Build());
//
// 			goals.Add(new AgentGoal.Builder("Collect Enough Crystal Shards")
// 				.WithPriority(500)
// 				.WithDesiredEffect(beliefs[ENOUGH_CRYSTAL_SHARDS_COLLECTED])
// 				.Build());
//
// 			goals.Add(new AgentGoal.Builder("Collect Enough Iron Ingots For Staff")
// 				.WithPriority(500)
// 				.WithDesiredEffect(beliefs[IRON_INGOTS_FOR_STAFF_COLLECTED])
// 				.Build());
//
// 			goals.Add(new AgentGoal.Builder("Collect Enough Iron Ingots For Shield")
// 				.WithPriority(500)
// 				.WithDesiredEffect(beliefs[IRON_INGOTS_FOR_SHIELD_COLLECTED])
// 				.Build());
// 		}
//
// 		public List<VillagerAgent> GetAvailableVillagers()
// 		{
// 			//return villagers.Where(v => v.IsAvailable).ToList();
// 			return null;
// 		}
//
// 		// public List<MessengerAgent> GetAvailableMessengers()
// 		// {
// 		// 	return messengers.Where(m => m.IsAvailable).ToList();
// 		// }
//
// 		public MageAgent GetAvailableMage()
// 		{
// 			return mage.IsAvailable ? mage : null;
// 		}
// 		
// 		     // Debug information
//         private void OnGUI()
//         {
//             if (!Application.isPlaying) return;
//
//             GUILayout.BeginArea(new Rect(10, 10, 400, 600));
//             GUILayout.Label("=== MetaAgent GOAP Status ===", GUI.skin.box);
//             
//             if (currentGoal != null)
//             {
//                 GUILayout.Label($"Current Goal: {currentGoal.Name} (Priority: {currentGoal.Priority})");
//             }
//             
//             if (currentAction != null)
//             {
//                 GUILayout.Label($"Current Action: {currentAction.Name}");
//                 GUILayout.Label($"Action Progress: {(currentAction.Complete ? "Complete" : "In Progress")}");
//             }
//
//             GUILayout.Space(10);
//             GUILayout.Label("=== Resource Status ===");
//             GUILayout.Label($"Oak Logs: {resourceManager.OakLogsEffectiveCount}/5");
//             GUILayout.Label($"Iron Ingots: {resourceManager.IronIngotsEffectiveCount}/5");
//             GUILayout.Label($"Crystal Shards: {resourceManager.CrystalsEffectiveCount}/4)");
//
//             GUILayout.Space(10);
//             GUILayout.Label("=== Artifacts Status ===");
//             GUILayout.Label($"Enchanted Staff: {(resourceManager.EnchantedStaff ? "Built" : "Not Built")}");
//             GUILayout.Label($"Runed Shield: {(resourceManager.RunedShield ? "Built" : "Not Built")}");
//             GUILayout.Label($"Combined Artifact: {(resourceManager.CombinedArtifact ? "Built" : "Not Built")}");
//
//             GUILayout.Space(10);
//             GUILayout.Label("=== Agent Availability ===");
//             GUILayout.Label($"Available Villagers: {GetAvailableVillagers().Count}/{villagers.Count}");
//            // GUILayout.Label($"Available Messengers: {GetAvailableMessengers().Count}/{messengers.Count}");
//
//             GUILayout.EndArea();
//         }
//     }
// }