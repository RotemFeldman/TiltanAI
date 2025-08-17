using UnityEngine;

[RequireComponent(typeof(Health))]
public class SimpleHealer : MonoBehaviour, IHealer
{
    public int potionCount = 0;
    public float healAmount = 35f;
    Health hp;

    void Awake(){ hp = GetComponent<Health>(); }
    public bool HasPotion => potionCount > 0;

    public bool Drink()
    {
        if (potionCount <= 0 || hp.IsDead) return false;
        hp.currentHP = Mathf.Min(hp.maxHP, hp.currentHP + healAmount);
        potionCount--;
        return true;
    }
}