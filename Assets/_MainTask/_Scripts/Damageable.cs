using UnityEngine;

public class Damageable : MonoBehaviour
{
    public float maxHP = 100f;
    public float currentHP;

    public System.Action<Damageable> onDeath;

    void Awake() => currentHP = maxHP;

    public void ApplyDamage(float amt)
    {
        currentHP -= amt;
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        onDeath?.Invoke(this);
        Destroy(gameObject);
    }
}