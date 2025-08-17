using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHP = 100f;
    public float currentHP = 100f;

    public event Action<float, GameObject> OnDamaged;  // (amount, source)
    public event Action<GameObject> OnDied;            // (killer)

    public bool IsDead => currentHP <= 0f;
    public float Health01 => Mathf.Clamp01(currentHP / maxHP);

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return;
        currentHP -= amount;
        OnDamaged?.Invoke(amount, source);
        if (currentHP <= 0f) OnDied?.Invoke(source);
    }
}