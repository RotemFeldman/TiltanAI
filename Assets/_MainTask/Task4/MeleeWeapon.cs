using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public float damage = 12f;
    public float attackRange = 2.2f;
    public float cooldown = 0.8f;
    float nextTime;

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

    public float Attack(Transform attacker, Transform target)
    {
        if (Time.time < nextTime || !target) return 0f;
        if (!InRange(attacker, target)) return 0f;

        var h = target.GetComponent<Health>();
        if (h)
        {
            h.TakeDamage(damage, attacker ? attacker.gameObject : gameObject);
            nextTime = Time.time + cooldown;
            Debug.DrawLine(attacker.position + Vector3.up*1.5f, target.position + Vector3.up*1.5f, Color.red, 0.1f);
            return 1f; // reward signal
        }
        return 0f;
    }
}