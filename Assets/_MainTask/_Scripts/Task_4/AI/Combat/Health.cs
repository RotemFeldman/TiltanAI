using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Tooltip("Auto-destroy the GameObject when OnDied fires.")]
    public bool destroyOnDeath = false;
    public float destroyDelay = 0f;

    public event Action<float, GameObject> OnDamaged;  // (amount, source)
    public event Action<GameObject> OnDied;            // (killer)

    public bool IsDead => currentHP <= 0f;
    public float Health01 => Mathf.Clamp01(currentHP / Mathf.Max(0.0001f, maxHP));

    bool deathInvoked = false;

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return;

        currentHP -= Mathf.Max(0f, amount);
        OnDamaged?.Invoke(amount, source);

        if (currentHP <= 0f && !deathInvoked)
        {
            deathInvoked = true;
            currentHP = 0f; // clamp so we never go below 0
            OnDied?.Invoke(source);

            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }
    }

    [ContextMenu("Kill (debug)")]
    public void KillDebug()
    {
        TakeDamage(99999f, null);
    }
}