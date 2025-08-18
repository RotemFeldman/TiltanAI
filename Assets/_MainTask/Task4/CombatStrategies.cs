using System;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    // Moves toward a dynamic target position (e.g., enemy position each tick)
    public class MoveToDynamicTargetStrategy : IActionStrategy
    {
        readonly NavMeshAgent agent;
        readonly Func<Vector3> targetPos;
        readonly float stopDistance;

        public bool CanPerform => !Complete;
        public bool Complete => !agent.pathPending && agent.remainingDistance <= stopDistance;

        public MoveToDynamicTargetStrategy(NavMeshAgent agent, Func<Vector3> targetPos, float stopDistance = 2.25f)
        {
            this.agent = agent;
            this.targetPos = targetPos;
            this.stopDistance = Mathf.Max(0.1f, stopDistance);
        }

        public void Start()
        {
            var pos = targetPos();
            agent.stoppingDistance = stopDistance;
            agent.SetDestination(pos);
        }

        public void Update(float dt)
        {
            var pos = targetPos();
            if ((agent.destination - pos).sqrMagnitude > 1.5f * 1.5f)
                agent.SetDestination(pos);
        }

        public void Stop()
        {
            agent.ResetPath();
        }
    }

    // Performs one melee swing at the nearest enemy in radius (if any)
    public class AttackNearestEnemyStrategy : IActionStrategy
    {
        readonly Transform self;
        readonly string enemyTag;
        readonly float radius;
        readonly SimpleCombat combat;
        readonly float tryDuration;
        float t;

        public bool CanPerform => !Complete;
        public bool Complete { get; private set; }

        public AttackNearestEnemyStrategy(Transform self, string enemyTag, float radius, SimpleCombat combat, float tryDuration = 1.0f)
        {
            this.self = self;
            this.enemyTag = enemyTag;
            this.radius = Mathf.Max(0.1f, radius);
            this.combat = combat;
            this.tryDuration = Mathf.Max(0.1f, tryDuration);
        }

        public void Start() { t = 0f; Complete = false; }

        public void Update(float dt)
        {
            t += dt;
            if (combat == null) { Complete = true; return; }

            // Find the closest enemy inside radius
            Collider[] hits = Physics.OverlapSphere(self.position, radius);
            Transform closest = null;
            float best = float.MaxValue;

            foreach (var h in hits)
            {
                if (!h) continue;
                var tr = h.transform;
                if (tr != null && tr.CompareTag(enemyTag))
                {
                    float d = (tr.position - self.position).sqrMagnitude;
                    if (d < best) { best = d; closest = tr; }
                }
            }

            if (closest != null)
            {
                combat.Attack(closest);     // ICombat.Attack expects Transform
                Complete = true;            // one swing; planner can enqueue again
                return;
            }

            if (t >= tryDuration) Complete = true;
        }
    }

    // NEW: Retreat away from the current enemy until a safe distance is reached
    public class RetreatFromEnemyStrategy : IActionStrategy
    {
        readonly NavMeshAgent agent;
        readonly Func<Transform> getThreat;
        readonly float targetSeparation;   // desired separation in meters
        readonly float repathInterval;
        readonly float sampleRadius;

        float lastRepath;
        bool complete;

        public bool CanPerform => !complete;
        public bool Complete => complete;

        public RetreatFromEnemyStrategy(
            NavMeshAgent agent,
            Func<Transform> getThreat,
            float targetSeparation = 12f,
            float repathInterval = 0.4f,
            float sampleRadius = 8f)
        {
            this.agent = agent;
            this.getThreat = getThreat;
            this.targetSeparation = Mathf.Max(2f, targetSeparation);
            this.repathInterval = Mathf.Max(0.05f, repathInterval);
            this.sampleRadius = Mathf.Max(2f, sampleRadius);
        }

        public void Start()
        {
            complete = false;
            RepathAway();
        }

        public void Update(float dt)
        {
            var threat = getThreat();
            if (threat == null || SafeUnity.SafePlanarWithin(agent.transform, threat, targetSeparation) == false)
            {
                // Either no threat, or already far enough → done
                complete = true;
                return;
            }

            if (Time.time - lastRepath >= repathInterval)
                RepathAway();

            if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.2f))
                RepathAway();
        }

        void RepathAway()
        {
            var threat = getThreat();
            if (threat == null) { complete = true; return; }

            if (!SafeUnity.TryGetPosition(threat, out var tp) || !SafeUnity.TryGetPosition(agent.transform, out var me))
            { complete = true; return; }

            me.y = tp.y = 0f;
            var dir = (me - tp);
            if (dir.sqrMagnitude < 0.01f) dir = -agent.transform.forward; // fallback
            dir.Normalize();

            var desired = me + dir * targetSeparation;
            if (NavMesh.SamplePosition(desired, out var hit, sampleRadius, NavMesh.AllAreas))
            {
                agent.stoppingDistance = 0.1f;
                agent.SetDestination(hit.position);
                lastRepath = Time.time;
            }
            else
            {
                // Try a slight randomization if straight-away failed
                var rnd = (UnityEngine.Random.insideUnitCircle.normalized * targetSeparation);
                var tryPos = me + new Vector3(rnd.x, 0f, rnd.y);
                if (NavMesh.SamplePosition(tryPos, out var hit2, sampleRadius, NavMesh.AllAreas))
                {
                    agent.stoppingDistance = 0.1f;
                    agent.SetDestination(hit2.position);
                    lastRepath = Time.time;
                }
                else
                {
                    complete = true; // give up
                }
            }
        }

        public void Stop() { agent.ResetPath(); }
    }

    // NEW: Instant potion usage
    public class DrinkPotionStrategy : IActionStrategy
    {
        readonly SimpleHealer healer;
        bool complete;

        public bool CanPerform => !complete;
        public bool Complete => complete;

        public DrinkPotionStrategy(SimpleHealer healer)
        {
            this.healer = healer;
        }

        public void Start()
        {
            // Attempt to drink once on start
            if (healer != null) healer.Drink();
            complete = true;
        }

        public void Update(float dt) { }
        public void Stop() { }
    }
}
