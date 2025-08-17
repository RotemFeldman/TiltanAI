using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public EnemyBehaviorConfig config;
    public string enemyTag = "Enemy";
    public string[] targetTags = { "Villager", "Mage", "Messenger" };

    private NavMeshAgent agent;
    private float attackTimer;
    private Vector3 patrolOrigin;
    private Vector3 curPatrolPoint;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        patrolOrigin = transform.position;

        if (config == null)
        {
            Debug.LogError($"{name}: Missing EnemyBehaviorConfig");
            enabled = false;
            return;
        }

        // Nav settings from config
        agent.speed = config.moveSpeed;
        agent.angularSpeed = config.angularSpeed;
        agent.acceleration = config.accel;
        agent.stoppingDistance = config.chaseStopDistance;

        PickNewPatrol();
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        var target = FindBestTarget();
        if (target != null)
        {
            var tpos = target.position;
            float dist = Vector3.Distance(transform.position, tpos);

            // chase
            agent.SetDestination(tpos);

            // attack
            if (dist <= config.attackRange && attackTimer <= 0f)
            {
                attackTimer = config.attackCooldown;
                TryDamage(target.gameObject);
            }
        }
        else
        {
            // simple patrol
            if (!agent.hasPath || agent.remainingDistance <= 0.35f)
            {
                if (Random.value < 0.3f) // idle a bit
                {
                    agent.ResetPath();
                    Invoke(nameof(PickNewPatrol), config.idleTime);
                }
                else PickNewPatrol();
            }
        }
    }

    Transform FindBestTarget()
    {
        var pos = transform.position;
        var fwd = transform.forward;

        Transform best = null;
        float bestD = Mathf.Infinity;

        foreach (var tag in targetTags)
        {
            var objs = GameObject.FindGameObjectsWithTag(tag);
            foreach (var o in objs)
            {
                var dir = (o.transform.position - pos);
                float d = dir.magnitude;
                if (d > config.visionRange) continue;

                float ang = Vector3.Angle(fwd, dir);
                if (ang > config.visionAngle * 0.5f) continue;

                if (d < bestD) { bestD = d; best = o.transform; }
            }
        }
        return best;
    }

    void PickNewPatrol()
    {
        Vector2 r = Random.insideUnitCircle * config.patrolRadius;
        Vector3 want = patrolOrigin + new Vector3(r.x, 0f, r.y);
        if (NavMesh.SamplePosition(want, out var hit, 5f, NavMesh.AllAreas))
            curPatrolPoint = hit.position;
        else
            curPatrolPoint = patrolOrigin;

        agent.SetDestination(curPatrolPoint);
    }

    void TryDamage(GameObject victim)
    {
        transform.LookAt(victim.transform.position);
        var dmg = victim.GetComponent<Damageable>();
        if (dmg != null) dmg.ApplyDamage(config.damage);
    }
}