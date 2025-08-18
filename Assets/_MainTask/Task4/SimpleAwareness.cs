using UnityEngine;

public class SimpleAwareness : MonoBehaviour, IAwareness
{
    [Header("Masks")]
    public LayerMask allyMask, enemyMask, potionMask, losMask;

    [Header("Ranges")]
    public float attackRange = 2.2f;
    public float sightRange = 18f;
    public float senseRange = 8f;

    [Header("Stability")]
    public float retargetHysteresis = 1.3f;
    public float lostSightCoyoteTime = 1.0f;

    Transform stickyTarget;
    float lastSeenTime;

    MeleeWeapon _weapon;

    public Transform Target => stickyTarget;
    public Vector3 ThreatCenter { get; private set; }
    public int AlliesNearby { get; private set; }
    public int EnemiesNearby { get; private set; }
    public float Health01 => GetComponent<Health>()?.Health01 ?? 1f;

    public bool InAttackRange
    {
        get
        {
            if (!Target) return false;
            if (_weapon) return _weapon.InRange(transform, Target);
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = Target.position;    b.y = 0f;
            return Vector3.Distance(a, b) <= attackRange;
        }
    }

    public bool CanSeeTarget =>
        stickyTarget && Vector3.Distance(transform.position, stickyTarget.position) <= sightRange &&
        !Physics.Linecast(transform.position, stickyTarget.position, losMask);

    public bool PotionInRange { get; private set; }
    public float DistToTarget01 => stickyTarget ? Mathf.Clamp01(Vector3.Distance(transform.position, stickyTarget.position)/sightRange) : 1f;
    public float DistToPotion01 { get; private set; }
    public bool NearSafeHaven { get; private set; }

    void Awake()
    {
        _weapon = GetComponent<SimpleCombat>()?.weapon;
    }

    void Update()
    {
        var pos = transform.position;

        var allies = Physics.OverlapSphere(pos, senseRange, allyMask);
        var enemies = Physics.OverlapSphere(pos, senseRange, enemyMask);
        AlliesNearby = allies.Length;
        EnemiesNearby = enemies.Length;

        Transform best = null; float bestSqr = Mathf.Infinity; Vector3 accum = Vector3.zero;
        foreach (var c in enemies)
        {
            accum += c.transform.position;
            float d = (c.transform.position - pos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = c.transform; }
        }
        ThreatCenter = enemies.Length > 0 ? accum / Mathf.Max(1, enemies.Length) : pos - transform.forward * 3f;

        if (stickyTarget == null)
        {
            stickyTarget = best;
            if (stickyTarget) lastSeenTime = Time.time;
        }
        else
        {
            bool stickyVisible = stickyTarget && Vector3.Distance(pos, stickyTarget.position) <= sightRange &&
                                 !Physics.Linecast(pos, stickyTarget.position, losMask);
            if (stickyVisible) lastSeenTime = Time.time;

            if (best && stickyTarget && best != stickyTarget)
            {
                float stickySqr = (stickyTarget.position - pos).sqrMagnitude;
                if (bestSqr * retargetHysteresis < stickySqr)
                    stickyTarget = best;
            }

            if (stickyTarget &&
                (Time.time - lastSeenTime) > lostSightCoyoteTime &&
                Vector3.Distance(pos, stickyTarget.position) > sightRange * 1.1f)
            {
                stickyTarget = null;
            }
        }

        var pots = Physics.OverlapSphere(pos, senseRange, potionMask);
        if (pots.Length > 0)
        {
            float bd = Mathf.Infinity;
            foreach (var c in pots)
            {
                float dd = (c.transform.position - pos).sqrMagnitude;
                if (dd < bd) bd = dd;
            }
            PotionInRange = true;
            DistToPotion01 = Mathf.Clamp01(Mathf.Sqrt(bd) / senseRange);
        }
        else { PotionInRange=false; DistToPotion01=1f; }

        var safe = GOAP.BuildLocation.Instance;
        NearSafeHaven = safe && Vector3.Distance(pos, safe.transform.position) < 4f;
    }
}
