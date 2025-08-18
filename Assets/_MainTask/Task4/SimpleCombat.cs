using UnityEngine;

[RequireComponent(typeof(Health))]
public class SimpleCombat : MonoBehaviour, ICombat
{
    Health hp;
    public float LastDamageTaken { get; private set; }
    public MeleeWeapon weapon { get; set; }
    public bool IsDead => hp && hp.IsDead;

    bool tryingToEnterSafe;

    void Awake()
    {
        hp = GetComponent<Health>();
        if (!weapon) weapon = GetComponent<MeleeWeapon>();
        if (!weapon) weapon = GetComponentInChildren<MeleeWeapon>();
        if (!weapon) weapon = gameObject.AddComponent<MeleeWeapon>();

        if (hp) hp.OnDamaged += (amt, src) => LastDamageTaken += amt;
    }

    public void SetTryingToEnterSafe(bool v) => tryingToEnterSafe = v;
    public bool TryingToEnterSafeHaven => tryingToEnterSafe;

    public float Attack(Transform target) => weapon.Attack(transform, target);
}