using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Agents
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(NavMeshAgent))]
    public class MageAgent : GoapAgent
    {
        // --- Keys / Beliefs ---
        private const string NOTHING = "Nothing";

        // Crafting chain
        private const string CAN_BUILD_STAFF   = "CanBuildStaff";
        private const string HAS_STAFF         = "HasStaff";
        private const string CAN_BUILD_SHIELD  = "CanBuildShield";
        private const string HAS_SHIELD        = "HasShield";
        private const string HAS_ARTIFACT      = "HasArtifact"; // final goal

        // Finding (so others can keep collecting while mage waits)
        private const string ENOUGH_OAK_LOGS_FOUND   = "EnoughOakLogsFound";
        private const string ENOUGH_CRYSTALS_FOUND   = "EnoughCrystalsFound";
        private const string ENOUGH_IRON_FOUND       = "EnoughIronFound";

        private const string NEAR_BUILD_LOCATION = "NearBuildLocation";

        // Enemy avoidance
        private const string ENEMY_NEAR       = "EnemyNear";
        private const string SAFE_FROM_ENEMY  = "SafeFromEnemy";

        [Header("Refs")]
        [SerializeField] GoapResourceManager resourceManager;
        [SerializeField] Transform buildLocation;

        [Header("Enemy Avoidance")]
        [SerializeField] private string enemyTag = "Enemy";
        [SerializeField] private float senseRange = 14f;
        [SerializeField] private float dangerRange = 10f;         // when enemy inside this → retreat
        [SerializeField] private float retreatSafeDistance = 14f; // how far to run
        [SerializeField] private float lostSightCoyoteTime = 1.0f;
        [SerializeField] private float retargetHysteresis = 1.3f;

        private NavMeshAgent navMeshAgent;
        private Transform currentEnemy;
        private float lastEnemySeenTime;

        protected override void Awake()
        {
            base.Awake();
            navMeshAgent = GetComponent<NavMeshAgent>();
            resourceManager = GoapResourceManager.Instance;
            buildLocation = BuildLocation.Instance.transform;
        }

        protected override void Update()
        {
            base.Update();
            UpdateEnemyTracking();
        }

        // ---------- Enemy sensing (sticky nearest) ----------
        void UpdateEnemyTracking()
        {
            // Find nearest enemy in sensing range
            Transform best = null; float bestSqr = float.PositiveInfinity;
            var hits = Physics.OverlapSphere(transform.position, senseRange);
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
                    SafeUnity.SafePlanarWithin(transform, currentEnemy, senseRange);

                if (stillInRange) lastEnemySeenTime = Time.time;

                if (best != null && currentEnemy != null && best != currentEnemy)
                {
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

        // ---------- GOAP wiring ----------
        protected override void SetupBeliefs()
        {
            beliefs = new();
            var factory = new BeliefFactory(this, beliefs);

            // Idle
            factory.AddBelief(NOTHING, () => false);

            // Crafting state
            factory.AddBelief(CAN_BUILD_STAFF,  () => resourceManager.OakLogsEffectiveCount   >= 5 &&
                                                      resourceManager.IronIngotsEffectiveCount >= 3);
            factory.AddBelief(HAS_STAFF, () => resourceManager.EnchantedStaff);

            factory.AddBelief(CAN_BUILD_SHIELD, () => resourceManager.CrystalsEffectiveCount   >= 4 &&
                                                      resourceManager.IronIngotsEffectiveCount >= 2);
            factory.AddBelief(HAS_SHIELD, () => resourceManager.RunedShield);

            factory.AddBelief(HAS_ARTIFACT, () => resourceManager.CombinedArtifact);

            // Resource discovery (so others can fetch)
            factory.AddBelief(ENOUGH_OAK_LOGS_FOUND, () => resourceManager.OakLogsFoundCount >= 5);
            factory.AddBelief(ENOUGH_CRYSTALS_FOUND, () => resourceManager.CrystalsFoundCount >= 4);
            factory.AddBelief(ENOUGH_IRON_FOUND, () => resourceManager.IronIngotsFoundCount >= 5);

            // Location safety
            factory.AddBelief(NEAR_BUILD_LOCATION,
                () => SafeUnity.SafePlanarWithin(transform, buildLocation, 5f));

            // Enemy avoidance beliefs
            factory.AddBelief(ENEMY_NEAR, () =>
            {
                if (currentEnemy == null) return false;
                return SafeUnity.SafePlanarWithin(transform, currentEnemy, dangerRange);
            });
            factory.AddBelief(SAFE_FROM_ENEMY, () =>
            {
                if (currentEnemy == null) return true;
                return !SafeUnity.SafePlanarWithin(transform, currentEnemy, retreatSafeDistance);
            });
        }

        protected override void SetupActions()
        {
            actions = new();

            // Idle/patrol
            actions.Add(new AgentAction.Builder("Patrol")
                .WithStrategy(new WanderStrategy(navMeshAgent, 25f))
                .AddEffect(beliefs[NOTHING])
                .Build());

            // Go to build site (used as a precondition for crafting)
            actions.Add(new AgentAction.Builder("Go To Build Location")
                .WithStrategy(new MoveToStrategy(navMeshAgent, buildLocation.position))
                .AddEffect(beliefs[NEAR_BUILD_LOCATION])
                .Build());

            // --- Crafting actions (require being near build site) ---
            actions.Add(new AgentAction.Builder("Craft Enchanted Staff")
                .WithStrategy(new CraftEnchantedStaffStrategy(resourceManager, 7f))   // consumes oak+iron on success
                .AddPrecondition(beliefs[CAN_BUILD_STAFF])
                .AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
                .AddEffect(beliefs[HAS_STAFF])
                .Build());

            actions.Add(new AgentAction.Builder("Craft Runed Shield")
                .WithStrategy(new CraftRunedShieldStrategy(resourceManager, 10f))     // consumes crystals+iron on success
                .AddPrecondition(beliefs[CAN_BUILD_SHIELD])
                .AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
                .AddEffect(beliefs[HAS_SHIELD])
                .Build());

            actions.Add(new AgentAction.Builder("Craft Combined Artifact")
                .WithStrategy(new CraftCombinedArtifactStrategy(resourceManager, 15f)) // flips CombinedArtifact on success
                .AddPrecondition(beliefs[HAS_STAFF])
                .AddPrecondition(beliefs[HAS_SHIELD])
                .AddPrecondition(beliefs[NEAR_BUILD_LOCATION])
                .AddEffect(beliefs[HAS_ARTIFACT])
                .Build());

            // --- Avoidance: run away when enemies are near ---
            actions.Add(new AgentAction.Builder("Retreat From Enemy")
                .WithStrategy(new RetreatFromEnemyStrategy(navMeshAgent, () => currentEnemy, retreatSafeDistance))
                .AddPrecondition(beliefs[ENEMY_NEAR])
                .AddEffect(beliefs[SAFE_FROM_ENEMY])
                .Build());

            // Optional: searching (lets mage help trigger discoveries if idle)
            actions.Add(new AgentAction.Builder("Search For Oak Logs")
                .WithStrategy(new WanderStrategy(navMeshAgent, 20f))
                .AddEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
                .Build());
            actions.Add(new AgentAction.Builder("Search For Crystals")
                .WithStrategy(new WanderStrategy(navMeshAgent, 20f))
                .AddEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
                .Build());
            actions.Add(new AgentAction.Builder("Search For Iron")
                .WithStrategy(new WanderStrategy(navMeshAgent, 20f))
                .AddEffect(beliefs[ENOUGH_IRON_FOUND])
                .Build());
        }

        protected override void SetupGoals()
        {
            goals = new HashSet<AgentGoal>();

            // 📌 Highest priority: finish the artifact
            goals.Add(new AgentGoal.Builder("Build Combined Artifact")
                .WithPriority(100)
                .WithDesiredEffect(beliefs[HAS_ARTIFACT])
                .Build());

            // If we can’t build the artifact yet, build its parts (high priority)
            goals.Add(new AgentGoal.Builder("Build Enchanted Staff")
                .WithPriority(80)
                .WithDesiredEffect(beliefs[HAS_STAFF])
                .Build());

            goals.Add(new AgentGoal.Builder("Build Runed Shield")
                .WithPriority(75)
                .WithDesiredEffect(beliefs[HAS_SHIELD])
                .Build());

            // 🛡️ Survival: run away from nearby enemies (very high, but just under artifact)
            goals.Add(new AgentGoal.Builder("Stay Safe From Enemies")
                .WithPriority(98)
                .WithDesiredEffect(beliefs[SAFE_FROM_ENEMY])
                .Build());

            // Low-priority discovery helpers (optional)
            goals.Add(new AgentGoal.Builder("Find Oak Logs")
                .WithPriority(25)
                .WithDesiredEffect(beliefs[ENOUGH_OAK_LOGS_FOUND])
                .Build());
            goals.Add(new AgentGoal.Builder("Find Crystals")
                .WithPriority(22)
                .WithDesiredEffect(beliefs[ENOUGH_CRYSTALS_FOUND])
                .Build());
            goals.Add(new AgentGoal.Builder("Find Iron")
                .WithPriority(20)
                .WithDesiredEffect(beliefs[ENOUGH_IRON_FOUND])
                .Build());

            // Fallback
            goals.Add(new AgentGoal.Builder("Do Nothing")
                .WithPriority(1)
                .WithDesiredEffect(beliefs[NOTHING])
                .Build());
        }
    }
}
