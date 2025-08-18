using System;
using System.Collections.Generic;
using System.Linq;
using _MainTask._Scripts;
using GOAP.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Agents
{
    [DefaultExecutionOrder(10)]
    public class VillagerAgent : GoapAgent , IResourceCarrier
    {
        [field:SerializeField] public ResourcePickup TargetResourcePickup { get; set; }
        [field:SerializeField] public ResourcePickup CurrentResource { get; private set; }

        private const string NOTHING = "Nothing";
        private const string HAS_TARGET_RESOURCE_PICKUP = "hasTargetResourcePickup";
        private const string HOLDING_RESOURCE = "holdingResource";

        // Resource beliefs
        private const string ENOUGH_OAK_LOGS_FOUND = "EnoughOakLogsFound";
        private const string ENOUGH_CRYSTALS_FOUND = "EnoughCrystalsFound";
        private const string ENOUGH_IRON_FOUND = "EnoughIronFound";

        private const string ENOUGH_OAK_LOGS_COLLECTED = "EnoughOakLogsCollected";
        private const string ENOUGH_CRYSTALS_COLLECTED = "EnoughCrystalsCollected";
        private const string ENOUGH_IRON_COLLECTED = "EnoughIronCollected";

        private const string NEAR_BUILD_LOCATION = "NearBuildLocation";
        private const string NEAR_PICKUP_LOCATION = "NearPickupLocation";

        // Combat beliefs
        private const string ENEMY_VISIBLE = "EnemyVisible";
        private const string ENEMY_IN_RANGE = "EnemyInRange";
        private const string AREA_SAFE = "AreaSafe";

        // Survival beliefs (NEW)
        private const string WOUNDED = "Wounded";
        private const string HEALTH_OK = "HealthOK";
        private const string HAS_POTION = "HasPotion";
        private const string SAFE_FROM_THREAT = "SafeFromThreat";

        [SerializeField] GoapResourceManager resourceManager;
        [SerializeField] Transform buildLocation;

        // Combat wiring
        [Header("Combat")]
        [SerializeField] private SimpleCombat combat;            // uses villager's MeleeWeapon
        [SerializeField] private float attackRadius = 2.4f;      // ≈ weapon range
        [SerializeField] private string enemyTag = "Enemy";      // tag used by enemies
        [SerializeField] private float enemySenseRange = 15f;    // how far we look for enemies
        [SerializeField] private float defendRadius = 12f;       // area to keep clear (for AREA_SAFE)
        [SerializeField] private float lostSightCoyoteTime = 1.0f;
        [SerializeField] private float retargetHysteresis = 1.3f;

        // Survival wiring (NEW)
        [Header("Survival")]
        [SerializeField, Range(0.05f,1f)] private float woundedThreshold = 0.35f;
        [SerializeField] private float retreatSafeDistance = 12f;
        [SerializeField] private SimpleHealer healer;            // optional; if present enables Drink

        private Transform currentEnemy;                          // sticky target
        private float lastEnemySeenTime;
        private Health hp;
        private NavMeshAgent navMeshAgent;

        protected override void Awake()
        {
            base.Awake();
            resourceManager = GoapResourceManager.Instance;
            buildLocation = BuildLocation.Instance.transform;
            navMeshAgent = GetComponent<NavMeshAgent>();
            hp = GetComponent<Health>();

            // Combat wiring
            if (combat == null) combat = GetComponent<SimpleCombat>();
            if (combat == null) combat = GetComponentInChildren<SimpleCombat>();

            // Healer wiring
            if (healer == null) healer = GetComponent<SimpleHealer>();       // add to prefab if you want potions
        }

        protected override void Update()
        {
            base.Update();
            UpdateEnemyTracking();

            // Existing pickup/delivery flow
            if (TargetResourcePickup != null)
            {
                if (Vector3.Distance(transform.position, TargetResourcePickup.Position) < 2f)
                {
                    PickupResource(TargetResourcePickup);
                }
            }

            if (CurrentResource != null)
            {
                if (Vector3.Distance(transform.position, CurrentResource.Position) < 3f)
                {
                    DropResource();
                }
            }
        }

        // === Enemy tracking (sticky) ===
        void UpdateEnemyTracking()
        {
            // find nearest enemy by tag in sensing radius
            Transform best = null; float bestSqr = float.PositiveInfinity;
            var hits = Physics.OverlapSphere(transform.position, enemySenseRange);
            foreach (var h in hits)
            {
                if (!h) continue;
                var tr = h.transform;
                if (tr != null && tr.CompareTag(enemyTag))
                {
                    float d = (tr.position - transform.position).sqrMagnitude;
                    if (d < bestSqr) { bestSqr = d; best = tr; }
                }
            }

            if (currentEnemy == null)
            {
                currentEnemy = best;
                if (currentEnemy != null) lastEnemySeenTime = Time.time;
            }
            else
            {
                bool stillInRange = currentEnemy &&
                    SafeUnity.SafePlanarWithin(transform, currentEnemy, enemySenseRange);

                if (stillInRange) lastEnemySeenTime = Time.time;

                if (best != null && currentEnemy != null && best != currentEnemy)
                {
                    // switch if new is much closer
                    Vector3 me, ce, be;
                    if (SafeUnity.TryGetPosition(transform, out me) &&
                        SafeUnity.TryGetPosition(currentEnemy, out ce) &&
                        SafeUnity.TryGetPosition(best, out be))
                    {
                        float curSqr = (ce - me).sqrMagnitude;
                        float bestSqr2 = (be - me).sqrMagnitude;
                        if (bestSqr2 * retargetHysteresis < curSqr) currentEnemy = best;
                    }
                }

                if ((!stillInRange) && (Time.time - lastEnemySeenTime) > lostSightCoyoteTime)
                    currentEnemy = null;
            }
        }

        bool InMeleeRange(Transform target)
        {
            if (target == null) return false;
            if (combat != null && combat.weapon != null)
                return combat.weapon.InRange(transform, target);
            // planar fallback
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = target.position;   b.y = 0f;
            return Vector3.Distance(a, b) <= attackRadius + 0.05f;
        }

        public void PickupResource(ResourcePickup resource)
        {
            CurrentResource = resource;
            TargetResourcePickup = null;
            CurrentResource.gameObject.SetActive(false);
        }

        public void DropResource()
        {
            resourceManager.AddGatheredResource(CurrentResource.ResourceType);
            Destroy(CurrentResource.gameObject);
            CurrentResource = null;
            TargetResourcePickup = null;
        }

        protected override void SetupBeliefs()
        {
            beliefs = new();
            BeliefFactory factory = new BeliefFactory(this, beliefs);

            // Baseline / resources
            factory.AddBelief(NOTHING, () => false);
            factory.AddBelief(HAS_TARGET_RESOURCE_PICKUP, (() => TargetResourcePickup != null));
            factory.AddBelief(HOLDING_RESOURCE, () => CurrentResource != null);

            // Finding
            factory.AddBelief(ENOUGH_OAK_LOGS_FOUND, () => resourceManager.OakLogsFoundCount >= 5);
            factory.AddBelief(ENOUGH_CRYSTALS_FOUND, () => resourceManager.CrystalsFoundCount >= 4);
            factory.AddBelief(ENOUGH_IRON_FOUND, () => resourceManager.IronIngotsFoundCount >= 5);

            // Collection
            factory.AddBelief(ENOUGH_OAK_LOGS_COLLECTED, () => resourceManager.OakLogsGatheredCount >= 5);
            factory.AddBelief(ENOUGH_CRYSTALS_COLLECTED, () => resourceManager.CrystalShardsGatheredCount >= 4);
            factory.AddBelief(ENOUGH_IRON_COLLECTED, () => resourceManager.IronIngotsGatheredCount >= 5);

            factory.AddBelief(NEAR_BUILD_LOCATION, () =>
                SafeUnity.SafePlanarWithin(transform, buildLocation, 5f));
            factory.AddBelief(NEAR_PICKUP_LOCATION, () =>
                TargetResourcePickup != null &&
                SafeUnity.SafePlanarWithin(transform, TargetResourcePickup.transform, 2f));

            // Combat
            factory.AddBelief(ENEMY_VISIBLE, () => currentEnemy != null);
            factory.AddBelief(ENEMY_IN_RANGE, () => currentEnemy != null && InMeleeRange(currentEnemy));
            factory.AddBelief(AREA_SAFE, () =>
            {
                var near = Physics.OverlapSphere(transform.position, defendRadius);
                foreach (var h in near) if (h != null && h.transform != null && h.transform.CompareTag(enemyTag)) return false;
                return true;
            });

            // Survival (NEW)
            factory.AddBelief(WOUNDED, () =>
            {
                float hp01 = hp ? hp.Health01 : 1f;
                return hp01 < woundedThreshold;
            });
            factory.AddBelief(HEALTH_OK, () =>
            {
                float hp01 = hp ? hp.Health01 : 1f;
                return hp01 >= 0.6f; // consider "healthy enough" after a potion
            });
            factory.AddBelief(HAS_POTION, () => healer != null && healer.HasPotion);
            factory.AddBelief(SAFE_FROM_THREAT, () =>
            {
                if (currentEnemy == null) return true;
                return !SafeUnity.SafePlanarWithin(transform, currentEnemy, retreatSafeDistance);
            });
        }

        protected override void SetupActions()
        {
            actions = new();

            // Idle/patrol
            actions.Add(new AgentAction.Builder("Relax")
                .WithStrategy(new WanderStrategy(navMeshAgent,50f))
                .AddEffect(beliefs[NOTHING])
                .Build());

            // Search
            actions.Add(new AgentAction.Builder("Search For Oak Logs")
                .WithStrategy(new WanderStrategy(navMeshAgent,30f))
                .AddEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
                .Build());
            actions.Add(new AgentAction.Builder("Search For Crystals")
                .WithStrategy(new WanderStrategy(navMeshAgent,30f))
                .AddEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
                .Build());
            actions.Add(new AgentAction.Builder("Search For Iron")
                .WithStrategy(new WanderStrategy(navMeshAgent,30f))
                .AddEffect(beliefs[ENOUGH_IRON_FOUND])
                .Build());

            // Delivery
            actions.Add(new AgentAction.Builder("Request Pickup Target")
                .WithStrategy(new RequestPickupStrategy<VillagerAgent>(this))
                .AddEffect(beliefs[HAS_TARGET_RESOURCE_PICKUP])
                .Build());

            actions.Add(new AgentAction.Builder("Go To Pickup Location")
                .WithStrategy(new MoveToPickupStrategy<VillagerAgent>(navMeshAgent, this))
                .AddPrecondition(beliefs[HAS_TARGET_RESOURCE_PICKUP])
                .AddEffect(beliefs[NEAR_PICKUP_LOCATION])
                .Build());

            actions.Add((new AgentAction.Builder("Pickup Resource"))
                .WithStrategy(new PickupResourceStrategy<VillagerAgent>(this))
                .AddPrecondition(beliefs[NEAR_PICKUP_LOCATION])
                .AddEffect(beliefs[HOLDING_RESOURCE])
                .Build());

            actions.Add(new AgentAction.Builder("Go To Build Location")
                .WithStrategy(new MoveToStrategy(navMeshAgent, buildLocation.position))
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
                .WithStrategy(new MoveToDynamicTargetStrategy(navMeshAgent,
                    () => currentEnemy != null ? currentEnemy.position : transform.position,
                    stopDistance: Mathf.Max(2.0f, attackRadius)))
                .AddPrecondition(beliefs[ENEMY_VISIBLE])
                .AddEffect(beliefs[ENEMY_IN_RANGE])
                .Build());

            actions.Add(new AgentAction.Builder("Attack Enemy")
                .WithStrategy(new AttackNearestEnemyStrategy(transform, enemyTag, attackRadius + 0.15f, combat))
                .AddPrecondition(beliefs[ENEMY_IN_RANGE])
                .AddEffect(beliefs[AREA_SAFE])
                .Build());

            // Survival (NEW)
            actions.Add(new AgentAction.Builder("Drink Potion")
                .WithStrategy(new DrinkPotionStrategy(healer))
                .AddPrecondition(beliefs[WOUNDED])
                .AddPrecondition(beliefs[HAS_POTION])
                .AddEffect(beliefs[HEALTH_OK])
                .Build());

            actions.Add(new AgentAction.Builder("Retreat From Enemy")
                .WithStrategy(new RetreatFromEnemyStrategy(navMeshAgent, () => currentEnemy, retreatSafeDistance))
                .AddPrecondition(beliefs[WOUNDED])
                .AddEffect(beliefs[SAFE_FROM_THREAT])
                .Build());
        }

        protected override void SetupGoals()
        {
            goals = new HashSet<AgentGoal>();

            // Survival is top priority:
            goals.Add(new AgentGoal.Builder("Heal Up")
                .WithPriority(90)                     // highest: drink if possible
                .WithDesiredEffect(beliefs[HEALTH_OK])
                .Build());

            goals.Add(new AgentGoal.Builder("Retreat When Wounded")
                .WithPriority(85)                     // next: run away if no potion
                .WithDesiredEffect(beliefs[SAFE_FROM_THREAT])
                .Build());

            // Delivery goals
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

            // Finding goals
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

            // Defend (below survival, above normal work)
            goals.Add(new AgentGoal.Builder("Defend Area")
                .WithPriority(65)
                .WithDesiredEffect(beliefs[AREA_SAFE])
                .Build());

            goals.Add(new AgentGoal.Builder("Do Nothing")
                .WithPriority(1)
                .WithDesiredEffect(beliefs[NOTHING])
                .Build());
        }
    }
}
