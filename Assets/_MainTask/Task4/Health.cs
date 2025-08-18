using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHP = 100f;
    public float currentHP = 100f;
    public bool destroyOnDeath = false;
    public float destroyDelay = 0f;

    public event Action<float, GameObject> OnDamaged;
    public event Action<GameObject> OnDied;

    public bool IsDead => currentHP <= 0f;
    public float Health01 => Mathf.Clamp01(currentHP / Mathf.Max(0.0001f, maxHP));

    bool deathInvoked;

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return;
        amount = Mathf.Max(0f, amount);
        currentHP -= amount;
        OnDamaged?.Invoke(amount, source);

        if (currentHP <= 0f && !deathInvoked)
        {
            deathInvoked = true;
            currentHP = 0f;
            OnDied?.Invoke(source);
            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }
    }

    [ContextMenu("Kill (debug)")]
    public void KillDebug() => TakeDamage(999999f, null);
}