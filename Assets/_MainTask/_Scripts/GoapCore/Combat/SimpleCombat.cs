using UnityEngine;

[RequireComponent(typeof(Health))]
public class SimpleCombat : MonoBehaviour, ICombat
{
    public MeleeWeapon weapon;
    Health hp;

    public bool TryingToEnterSafeHaven { get; private set; }
    float lastDamageTaken;
    public float LastDamageTaken { get { var d = lastDamageTaken; lastDamageTaken = 0f; return d; } }
    public bool IsDead => hp.IsDead;

    void Awake()
    {
        hp = GetComponent<Health>();
        if (!weapon) weapon = gameObject.AddComponent<MeleeWeapon>();
        hp.OnDamaged += (amt, src) => lastDamageTaken += amt;
    }

    public float Attack(Transform target)
    {
        if (!target) return 0f;
        bool hit;
        return weapon.Attack(transform, target, out hit);
    }

    public void SetTryingToEnterSafe(bool value) => TryingToEnterSafeHaven = value;
}