using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class CombatAgent : MonoBehaviour
{
    public float attackRange = 1.8f;
    public float attackCooldown = 1.0f;
    public float damage = 12f;

    private float cd;
    private Damageable dmg;

    void Awake() => dmg = GetComponent<Damageable>();
    void Update() { if (cd > 0f) cd -= Time.deltaTime; }

    public float Health01 => Mathf.Clamp01(dmg.currentHP / Mathf.Max(1f, dmg.maxHP));

    public bool TryAttack(Transform target)
    {
        if (target == null || cd > 0f) return false;
        float d = Vector3.Distance(transform.position, target.position);
        if (d > attackRange) return false;

        var victim = target.GetComponent<Damageable>();
        if (victim != null)
        {
            transform.LookAt(target.position);
            victim.ApplyDamage(damage);
            cd = attackCooldown;
            return true;
        }
        return false;
    }
}