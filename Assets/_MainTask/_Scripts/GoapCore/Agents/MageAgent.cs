using System.Collections.Generic;
using _MainTask._Scripts;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Agents
{
    [DefaultExecutionOrder(10)]
    public class MageAgent : GoapAgent
    {
        const string NOTHING = "Nothing";

        // Crafting beliefs (match your resource manager flags)
        const string ENOUGH_OAK = "EnoughOak";
        const string ENOUGH_CRYSTAL = "EnoughCrystal";
        const string ENOUGH_IRON = "EnoughIron";

        const string CAN_CRAFT_STAFF = "CanCraftStaff";
        const string CAN_CRAFT_SHIELD = "CanCraftShield";
        const string CAN_CRAFT_ARTIFACT = "CanCraftArtifact";

        const string NEAR_BUILD_LOCATION = "NearBuildLocation";

        // Combat beliefs
        const string ENEMY_VISIBLE = "EnemyVisible";
        const string IN_MELEE_RANGE = "InMeleeRange";
        const string LOW_HEALTH = "LowHealth";
        const string HAS_POTION = "HasPotion";

        GoapResourceManager rm;
        Transform buildLocation;
        NavMeshAgent nav;

        SimpleAwareness sense;
        SimpleHealer healer;
        SimpleCombat combat;

        protected override void Awake()
        {
            base.Awake();
            rm = GoapResourceManager.Instance;
            buildLocation = GOAP.BuildLocation.Instance.transform; // singleton:contentReference[oaicite:13]{index=13}
            navMeshAgent = GetComponent<NavMeshAgent>();
            nav = navMeshAgent;
            sense = GetComponent<SimpleAwareness>();
            healer = GetComponent<SimpleHealer>();
            combat = GetComponent<SimpleCombat>();
        }

        protected override void SetupBeliefs()
        {
            beliefs = new();
            var f = new BeliefFactory(this, beliefs);

            f.AddBelief(NOTHING, () => false);
            f.AddBelief(NEAR_BUILD_LOCATION, () => Vector3.Distance(buildLocation.position, transform.position) < 4f);

            // resources
            f.AddBelief(ENOUGH_OAK,     () => rm.OakLogsGatheredCount   >= 5);
            f.AddBelief(ENOUGH_CRYSTAL, () => rm.CrystalShardsGatheredCount >= 4);
            f.AddBelief(ENOUGH_IRON,    () => rm.IronIngotsGatheredCount   >= 5);

            // craftability (example logic; adjust if your manager exposes booleans)
            f.AddBelief(CAN_CRAFT_STAFF,    () => rm.OakLogsGatheredCount >= 5 && rm.IronIngotsGatheredCount >= 3 && !rm.EnchantedStaff);
            f.AddBelief(CAN_CRAFT_SHIELD,   () => rm.CrystalShardsGatheredCount >= 4 && rm.IronIngotsGatheredCount >= 2 && !rm.RunedShield);
            f.AddBelief(CAN_CRAFT_ARTIFACT, () => rm.EnchantedStaff && rm.RunedShield && !rm.CombinedArtifact);

            // combat
            f.AddBelief(ENEMY_VISIBLE, () => sense && sense.Target != null);
            f.AddBelief(IN_MELEE_RANGE, () => sense && sense.InAttackRange);
            f.AddBelief(LOW_HEALTH, () => sense && sense.Health01 < 0.4f);
            f.AddBelief(HAS_POTION, () => healer && healer.HasPotion);
        }

        protected override void SetupActions()
        {
            actions = new();

            actions.Add(new AgentAction.Builder("Relax")
                .WithStrategy(new WanderStrategy(nav, 20f))
                .AddEffect(beliefs[NOTHING]).Build());

            // Move to build spot
            actions.Add(new AgentAction.Builder("Go To Build")
                .WithStrategy(new MoveToStrategy(nav, buildLocation.position))
                .AddEffect(beliefs[NEAR_BUILD_LOCATION]).Build());

            // Craft pipelines (use your existing strategies in IActionStrategy.cs):contentReference[oaicite:14]{index=14}
            actions.Add(new AgentAction.Builder("Craft Enchanted Staff")
                .WithStrategy(new CraftEnchantedStaffStrategy(rm, 3f))
                .AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
                .AddPrecondition(beliefs[CAN_CRAFT_STAFF])
                .AddEffect(beliefs[CAN_CRAFT_STAFF]).Build());

            actions.Add(new AgentAction.Builder("Craft Runed Shield")
                .WithStrategy(new CraftRunedShieldStrategy(rm, 3f))
                .AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
                .AddPrecondition(beliefs[CAN_CRAFT_SHIELD])
                .AddEffect(beliefs[CAN_CRAFT_SHIELD]).Build());

            actions.Add(new AgentAction.Builder("Craft Combined Artifact")
                .WithStrategy(new CraftCombinedArtifactStrategy(rm, 5f))
                .AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
                .AddPrecondition(beliefs[CAN_CRAFT_ARTIFACT])
                .AddEffect(beliefs[CAN_CRAFT_ARTIFACT]).Build());

            // ===== Combat actions (NEW) =====
            actions.Add(new AgentAction.Builder("Chase Enemy")
                .WithStrategy(new MoveToDynamicTargetStrategy(nav, () => sense ? sense.Target : null))
                .AddPrecondition(beliefs[ENEMY_VISIBLE])
                .AddEffect(beliefs[IN_MELEE_RANGE]).Build());

            actions.Add(new AgentAction.Builder("Melee Attack")
                .WithStrategy(new MeleeAttackStrategy(sense, combat))
                .AddPrecondition(beliefs[IN_MELEE_RANGE])
                .AddEffect(beliefs[ENEMY_VISIBLE]).Build());

            actions.Add(new AgentAction.Builder("Retreat From Threat")
                .WithStrategy(new RetreatStrategy(GetComponent<NavMovement>(), sense, 1.6f))
                .AddPrecondition(beliefs[LOW_HEALTH])
                .AddEffect(beliefs[ENEMY_VISIBLE]).Build());

            actions.Add(new AgentAction.Builder("Drink Potion")
                .WithStrategy(new DrinkPotionStrategy(healer))
                .AddPrecondition(beliefs[LOW_HEALTH])
                .AddPrecondition(beliefs[HAS_POTION])
                .AddEffect(beliefs[LOW_HEALTH]).Build());
        }

        protected override void SetupGoals()
        {
            goals = new HashSet<AgentGoal>();

            // Combat (NEW – mage survival prioritized highest)
            goals.Add(new AgentGoal.Builder("Survive")
                .WithPriority(1200)
                .WithDesiredEffect(beliefs[LOW_HEALTH]).Build());

            goals.Add(new AgentGoal.Builder("Defend Base")
                .WithPriority(1100)
                .WithDesiredEffect(beliefs[ENEMY_VISIBLE]).Build());

            // Crafting (below combat)
            goals.Add(new AgentGoal.Builder("Build Combined Artifact")
                .WithPriority(1000)
                .WithDesiredEffect(beliefs[CAN_CRAFT_ARTIFACT]).Build());

            goals.Add(new AgentGoal.Builder("Build Runed Shield")
                .WithPriority(900)
                .WithDesiredEffect(beliefs[CAN_CRAFT_SHIELD]).Build());

            goals.Add(new AgentGoal.Builder("Build Enchanted Staff")
                .WithPriority(800)
                .WithDesiredEffect(beliefs[CAN_CRAFT_STAFF]).Build());

            goals.Add(new AgentGoal.Builder("Idle")
                .WithPriority(1)
                .WithDesiredEffect(beliefs[NOTHING]).Build());
        }
    }
}
