using UnityEngine;

[RequireComponent(typeof(Health))]
public class MeleeWeapon : MonoBehaviour
{
    public float damage = 12f;
    public float attackRange = 2.2f;
    public float cooldown = 0.8f;
    float cd;

    public bool InRange(Transform self, Transform target)
    {
        if (!target) return false;

        Vector3 a = self.position; a.y = 0f;
        Vector3 b = target.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);

        float targetRadius = 0f;
        var cc = target.GetComponent<CharacterController>(); if (cc) targetRadius = Mathf.Max(targetRadius, cc.radius);
        var cap = target.GetComponent<CapsuleCollider>();   if (cap) targetRadius = Mathf.Max(targetRadius, cap.radius);

        return dist - targetRadius <= attackRange;
    }



    public float Attack(Transform self, Transform target, out bool hit)
    {
        hit = false;
        if (cd > 0f || !InRange(self, target)) return 0f;
        cd = cooldown;

        var h = target.GetComponent<Health>();
        if (h && !h.IsDead)
        {
            h.TakeDamage(damage, self.gameObject);
            hit = true;
            return damage;
        }
        return 0f;
    }

    void Update(){ if (cd > 0f) cd -= Time.deltaTime; }
}