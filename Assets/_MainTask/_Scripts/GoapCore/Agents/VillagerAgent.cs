using System;
using System.Collections.Generic;
using _MainTask._Scripts;
using GOAP.Interfaces;
using UnityEngine;
using UnityEngine.AI;
using GOAP;

namespace GOAP.Agents
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(SimpleAwareness))]
    [RequireComponent(typeof(SimpleCombat))]
    [RequireComponent(typeof(SimpleHealer))]
    [RequireComponent(typeof(Health))]
    public class VillagerAgent : GoapAgent, IResourceCarrier
    {
        // IResourceCarrier
        [field: SerializeField] public ResourcePickup TargetResourcePickup { get; set; }
        [field: SerializeField] public ResourcePickup CurrentResource { get; private set; }

        // Keys
        private const string NOTHING = "Nothing";
        private const string HAS_TARGET_RESOURCE_PICKUP = "hasTargetResourcePickup";
        private const string HOLDING_RESOURCE = "holdingResource";

        // Finding
        private const string ENOUGH_OAK_LOGS_FOUND = "EnoughOakLogsFound";
        private const string ENOUGH_CRYSTALS_FOUND = "EnoughCrystalsFound";
        private const string ENOUGH_IRON_FOUND = "EnoughIronFound";

        // Collected
        private const string ENOUGH_OAK_LOGS_COLLECTED = "EnoughOakLogsCollected";
        private const string ENOUGH_CRYSTALS_COLLECTED = "EnoughCrystalsCollected";
        private const string ENOUGH_IRON_COLLECTED = "EnoughIronCollected";

        // Positions
        private const string NEAR_BUILD_LOCATION = "NearBuildLocation";
        private const string NEAR_PICKUP_LOCATION = "NearPickupLocation";

        // Combat (preconditions)
        private const string ENEMY_VISIBLE = "EnemyVisible";
        private const string IN_MELEE_RANGE = "InMeleeRange";
        private const string LOW_HEALTH = "LowHealth";
        private const string HAS_POTION = "HasPotion";

        // Combat (end-states for goals)
        private const string ENEMY_GONE = "EnemyGone";
        private const string SAFE = "Safe";

        [SerializeField] private GoapResourceManager resourceManager;

        // local comps
        private NavMeshAgent nav;
        private SimpleAwareness sense;
        private SimpleHealer healer;
        private SimpleCombat combat;

        protected override void Awake()
        {
            base.Awake();
            resourceManager = GoapResourceManager.Instance;

            nav = GetComponent<NavMeshAgent>();
            sense = GetComponent<SimpleAwareness>();
            healer = GetComponent<SimpleHealer>();
            combat = GetComponent<SimpleCombat>();

            navMeshAgent ??= nav;
        }

        // IMPORTANT: override Start and call base.Start()
        protected override void Start()
        {
            base.Start(); // <-- this ensures SetupBeliefs/Actions/Goals run (initializes 'goals') :contentReference[oaicite:3]{index=3}

            var h = GetComponent<Health>();
            if (h != null)
            {
                h.OnDied += _ =>
                {
                    if (CurrentResource != null) DropResource();
                    Destroy(gameObject);
                };
            }
        }

        protected override void Update()
        {
            base.Update();

            // Arrive to pickup
            if (TargetResourcePickup != null &&
                Vector3.Distance(transform.position, TargetResourcePickup.Position) < 2f)
            {
                PickupResource(TargetResourcePickup);
            }

            // Arrive to build → deliver
            var bl = BuildLocation.Instance;
            if (CurrentResource != null && bl != null &&
                Vector3.Distance(transform.position, bl.transform.position) < 3f)
            {
                DropResource();
            }
        }

        // IResourceCarrier
        public void PickupResource(ResourcePickup resource)
        {
            if (resource == null) return;
            CurrentResource = resource;
            TargetResourcePickup = null;
            if (CurrentResource.gameObject) CurrentResource.gameObject.SetActive(false);
        }

        public void DropResource()
        {
            if (CurrentResource == null) return;
            resourceManager.AddGatheredResource(CurrentResource.ResourceType);
            if (CurrentResource.gameObject) Destroy(CurrentResource.gameObject);
            CurrentResource = null;
            TargetResourcePickup = null;
        }

        protected override void SetupBeliefs()
        {
            beliefs = new();
            var factory = new BeliefFactory(this, beliefs);

            factory.AddBelief(NOTHING, () => false);
            factory.AddBelief(HAS_TARGET_RESOURCE_PICKUP, () => TargetResourcePickup != null);
            factory.AddBelief(HOLDING_RESOURCE, () => CurrentResource != null);

            // Finding
            factory.AddBelief(ENOUGH_OAK_LOGS_FOUND, () => resourceManager.OakLogsFoundCount >= 5);
            factory.AddBelief(ENOUGH_CRYSTALS_FOUND, () => resourceManager.CrystalsFoundCount >= 4);
            factory.AddBelief(ENOUGH_IRON_FOUND, () => resourceManager.IronIngotsFoundCount >= 5);

            // Collected
            factory.AddBelief(ENOUGH_OAK_LOGS_COLLECTED, () => resourceManager.OakLogsGatheredCount >= 5);
            factory.AddBelief(ENOUGH_CRYSTALS_COLLECTED, () => resourceManager.CrystalShardsGatheredCount >= 4);
            factory.AddBelief(ENOUGH_IRON_COLLECTED, () => resourceManager.IronIngotsGatheredCount >= 5);

            // BuildLocation — lazy: evaluate at runtime to avoid Awake race
            factory.AddBelief(NEAR_BUILD_LOCATION, () =>
            {
                var b = BuildLocation.Instance;
                return b != null && Vector3.Distance(b.transform.position, transform.position) < 5f;
            });

            factory.AddBelief(NEAR_PICKUP_LOCATION, () =>
                TargetResourcePickup != null &&
                Vector3.Distance(TargetResourcePickup.transform.position, transform.position) < 2f);

            // Combat preconditions
            factory.AddBelief(ENEMY_VISIBLE, () => sense && sense.Target != null);
            factory.AddBelief(IN_MELEE_RANGE, () => sense && sense.InAttackRange);
            factory.AddBelief(LOW_HEALTH, () => sense && sense.Health01 < 0.4f);
            factory.AddBelief(HAS_POTION, () => healer && healer.HasPotion);

            // Combat end states
            factory.AddBelief(ENEMY_GONE, () => sense && sense.Target == null);
            factory.AddBelief(SAFE, () => sense && sense.Health01 >= 0.4f);
        }

        protected override void SetupActions()
        {
            actions = new();

            // Idle / Search
            actions.Add(new AgentAction.Builder("Relax")
                .WithStrategy(new WanderStrategy(nav, 50f))
                .AddEffect(beliefs[NOTHING])
                .Build());

            actions.Add(new AgentAction.Builder("Search For Oak Logs")
                .WithStrategy(new WanderStrategy(nav, 30f))
                .AddEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
                .Build());

            actions.Add(new AgentAction.Builder("Search For Crystals")
                .WithStrategy(new WanderStrategy(nav, 30f))
                .AddEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
                .Build());

            actions.Add(new AgentAction.Builder("Search For Iron")
                .WithStrategy(new WanderStrategy(nav, 30f))
                .AddEffect(beliefs[ENOUGH_IRON_FOUND])
                .Build());

            // Pickup & Delivery
            actions.Add(new AgentAction.Builder("Request Pickup Target")
                .WithStrategy(new RequestPickupStrategy<VillagerAgent>(this))
                .AddEffect(beliefs[HAS_TARGET_RESOURCE_PICKUP])
                .Build());

            actions.Add(new AgentAction.Builder("Go To Pickup Location")
                .WithStrategy(new MoveToPickupStrategy<VillagerAgent>(nav, this))
                .AddPrecondition(beliefs[HAS_TARGET_RESOURCE_PICKUP])
                .AddEffect(beliefs[NEAR_PICKUP_LOCATION])
                .Build());

            actions.Add(new AgentAction.Builder("Pickup Resource")
                .WithStrategy(new PickupResourceStrategy<VillagerAgent>(this))
                .AddPrecondition(beliefs[NEAR_PICKUP_LOCATION])
                .AddEffect(beliefs[HOLDING_RESOURCE])
                .Build());

            // Lazy build location (prevents null position)
            actions.Add(new AgentAction.Builder("Go To Build Location")
                .WithStrategy(new MoveToBuildLocationStrategy(nav))
                .AddPrecondition(beliefs[HOLDING_RESOURCE])
                .AddEffect(beliefs[NEAR_BUILD_LOCATION])
                .Build());

            actions.Add(new AgentAction.Builder("Deliver Resource")
                .WithStrategy(new DeliverResourceStrategy<VillagerAgent>(this))
                .WithCost(10)
                .AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
                .AddEffect(beliefs[HOLDING_RESOURCE])
                .AddEffect(beliefs[ENOUGH_OAK_LOGS_COLLECTED])
                .AddEffect(beliefs[ENOUGH_CRYSTALS_COLLECTED])
                .AddEffect(beliefs[ENOUGH_IRON_COLLECTED])
                .Build());

            // Combat
            actions.Add(new AgentAction.Builder("Chase Enemy")
                .WithStrategy(new MoveToDynamicTargetStrategy(nav, () => sense ? sense.Target : null))
                .AddPrecondition(beliefs[ENEMY_VISIBLE])
                .AddEffect(beliefs[IN_MELEE_RANGE])
                .Build());

            actions.Add(new AgentAction.Builder("Melee Attack")
                .WithStrategy(new MeleeAttackStrategy(sense, combat))
                .AddPrecondition(beliefs[IN_MELEE_RANGE])
                .AddEffect(beliefs[ENEMY_GONE])   // end-state for planner
                .Build());

            actions.Add(new AgentAction.Builder("Retreat From Threat")
                .WithStrategy(new RetreatStrategy(GetComponent<NavMovement>(), sense, 1.4f))
                .AddPrecondition(beliefs[LOW_HEALTH])
                .AddEffect(beliefs[SAFE])         // end-state for planner
                .Build());

            actions.Add(new AgentAction.Builder("Drink Potion")
                .WithStrategy(new DrinkPotionStrategy(healer))
                .AddPrecondition(beliefs[LOW_HEALTH])
                .AddPrecondition(beliefs[HAS_POTION])
                .AddEffect(beliefs[SAFE])         // end-state for planner
                .Build());
        }

        protected override void SetupGoals()
        {
            goals = new HashSet<AgentGoal>();

            // Combat (positive end-state goals)
            goals.Add(new AgentGoal.Builder("Survive")
                .WithPriority(1100)
                .WithDesiredEffect(beliefs[SAFE])
                .Build());

            goals.Add(new AgentGoal.Builder("Defend Base")
                .WithPriority(1000)
                .WithDesiredEffect(beliefs[ENEMY_GONE])
                .Build());

            // Collect
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

            // Finding
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

            // Idle
            goals.Add(new AgentGoal.Builder("Do Nothing")
                .WithPriority(1)
                .WithDesiredEffect(beliefs[NOTHING])
                .Build());
        }
    }
}
