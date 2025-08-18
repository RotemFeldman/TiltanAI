using UnityEngine;

public class SimpleHealer : MonoBehaviour, IHealer
{
    public float healAmount = 35f;
    public bool HasPotion => potionCount > 0;
    public int potionCount { get; set; }

    public bool Drink()
    {
        if (potionCount <= 0) return false;
        var h = GetComponent<Health>();
        if (!h || h.IsDead) return false;

        float before = h.currentHP;
        h.currentHP = Mathf.Clamp(h.currentHP + healAmount, 0f, h.maxHP);
        if (h.currentHP > before)
        {
            potionCount--;
            return true;
        }
        return false;
    }
}