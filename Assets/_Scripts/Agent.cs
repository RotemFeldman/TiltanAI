using System.Linq;
using UnityEngine;
using UtilityAI;

[ExecuteInEditMode] // This makes certain functions run in editor mode
public class Agent : MonoBehaviour
{
    [Header("Agent Settings")] [SerializeField]
    public AgentStats baseStats;

    [SerializeField] protected string agentName = "Agent";

    [Header("Agent Group Settings")] [SerializeField]
    private AgentGroup agentGroup;

    [SerializeField] private AgentGroupTextures groupTextures;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Current Stats")] [SerializeField]
    private float currentHealth;

    [SerializeField] private bool isRegeneratingHealth = false;

    // Timer for stat updates
    private float statUpdateTimer = 0f;
    private const float STAT_UPDATE_INTERVAL = 0.5f; // Update stats every half second


    private AgentGroup lastGroup; // To track changes
    public Sensor sensor; // To track sensor position






    private void OnEnable()
    {
        // This will run both in edit mode and play mode
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        lastGroup = agentGroup;
        UpdateAgentAppearance();
    }



    void Start()
    {

        if (baseStats != null)
        {
            // Initialize current health to max health when the game starts
            currentHealth = baseStats.maxHealth;



        }
        else
        {
            Debug.LogError("Agent is missing base stats configuration!", this);
        }


    }

    void Update()
    {
        // Check for group changes in editor
        if (!Application.isPlaying && lastGroup != agentGroup)
        {
            lastGroup = agentGroup;
            UpdateAgentAppearance();
        }

        // Only process stat updates during gameplay
        if (Application.isPlaying)
        {
            statUpdateTimer += Time.deltaTime;

            if (statUpdateTimer >= STAT_UPDATE_INTERVAL)
            {
                UpdateStats();
                statUpdateTimer = 0f;
            }
        }
    }

    private void UpdateStats()
    {
        // Only process if we have base stats
        if (baseStats != null)
        {
            // Process health regeneration if enabled
            if (isRegeneratingHealth && currentHealth < baseStats.maxHealth)
            {
                // Apply health regeneration rate (per second)
                float healthToAdd = baseStats.healthRegenRate * STAT_UPDATE_INTERVAL;
                currentHealth = Mathf.Min(currentHealth + healthToAdd, baseStats.maxHealth);
                if (currentHealth >= baseStats.maxHealth)
                {
                    isRegeneratingHealth = false; // Stop regenerating if at max health
                }



            }

            // Process hunger depletion
            float hungerToDeplete = (baseStats.hungerRate) * STAT_UPDATE_INTERVAL;
            CurrentHunger = Mathf.Max(0f, CurrentHunger - hungerToDeplete);

            

        }
    }



    private void UpdateAgentAppearance()
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError($"SkinnedMeshRenderer is null on {gameObject.name}!", this);
            return;
        }

        if (groupTextures == null)
        {
            Debug.LogError($"GroupTextures is null on {gameObject.name}!", this);
            return;
        }

        Material groupMaterial = groupTextures.GetMaterialForGroup(agentGroup);
        if (groupMaterial != null)
        {
            // Handle material assignment differently in edit mode vs play mode
            if (Application.isPlaying)
            {
                skinnedMeshRenderer.material = new Material(groupMaterial);
            }
            else
            {
                // In edit mode, we can directly assign the material
                skinnedMeshRenderer.sharedMaterial = groupMaterial;
            }
        }
        else
        {
            Debug.LogWarning($"No material found for group {agentGroup} on {gameObject.name}", this);
        }
    }

    // Public methods to manipulate health
    public void TakeDamage(float damageAmount)
    {
        if (baseStats == null) return;

        currentHealth = Mathf.Max(0f, currentHealth - damageAmount);



        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        if (currentHealth < MaxHealth)
            isRegeneratingHealth = true;

    }

    public void Heal(float healAmount)
    {
        if (baseStats == null) return;

        currentHealth = Mathf.Min(baseStats.maxHealth, currentHealth + healAmount);
    }



    // Virtual method for death that can be overridden by subclasses
    protected virtual void Die()
    {
        Debug.Log($"{agentName} has died!");
        // Subclasses can override this to implement death behavior
    }

    public AgentGroup Group
    {
        get => agentGroup;
        set
        {
            if (agentGroup != value)
            {
                agentGroup = value;
                lastGroup = value;
                UpdateAgentAppearance();
            }
        }
    }

    // Properties to access agent stats
    public float MaxHealth => baseStats != null ? baseStats.maxHealth : 0f;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => baseStats != null ? currentHealth / baseStats.maxHealth : 0f;
    public float MoveSpeed => baseStats != null ? baseStats.moveSpeed : 0f;
    public float AttackDamage => baseStats != null ? baseStats.baseDamage : 0f;
    public float AttackSpeed => baseStats != null ? baseStats.attackSpeed : 0f;
    public float HealthRegenRate => baseStats != null ? baseStats.healthRegenRate : 0f;

    public float CurrentHunger = 1f; // Assuming hunger is a value between 0 and 1

#if UNITY_EDITOR
    private void OnValidate()
    {
        // This is called when values are changed in the inspector
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        // Ensure update happens on the next editor frame
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) // Check if object still exists
            {
                UpdateAgentAppearance();
            }
        };
    }
#endif

   
}