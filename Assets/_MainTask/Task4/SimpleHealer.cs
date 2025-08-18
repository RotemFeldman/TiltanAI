using UnityEngine;

[RequireComponent(typeof(Health))]
public class SimpleHealer : MonoBehaviour, IHealer
{
    [Header("Inventory")]
    [SerializeField] private int _potionCount = 0;

    /// <summary>
    /// Required by IHealer. Public int property with get/set.
    /// Backed by serialized _potionCount so you can set it in Inspector.
    /// </summary>
    public int potionCount
    {
        get => _potionCount;
        set => _potionCount = Mathf.Max(0, value);
    }

    /// <summary>Convenience flag (many callers use this).</summary>
    public bool HasPotion => potionCount > 0;

    [Header("Effect")]
    public float healAmount = 35f;
    public float drinkCooldown = 1.0f;

    [Header("QoL")]
    public bool autoDrinkWhenLow = false;
    [Range(0.05f, 1f)] public float autoDrinkThreshold = 0.35f; // 35% HP

    private float lastDrinkTime;
    private Health hp;

    void Awake()
    {
        hp = GetComponent<Health>();
    }

    void Update()
    {
        if (!autoDrinkWhenLow || hp == null) return;
        if (!HasPotion) return;

        if (hp.Health01 <= autoDrinkThreshold)
        {
            Drink();
        }
    }

    /// <summary>Add given number of potions to inventory.</summary>
    public void AddPotions(int n)
    {
        if (n <= 0) return;
        potionCount += n;
    }

    /// <summary>Drink a potion if available; returns true if healed.</summary>
    public bool Drink()
    {
        if (!HasPotion || hp == null) return false;
        if (Time.time - lastDrinkTime < drinkCooldown) return false;
        if (hp.Health01 >= 0.999f) return false;

        potionCount--;
        hp.Heal(healAmount);
        lastDrinkTime = Time.time;
        return true;
    }
}