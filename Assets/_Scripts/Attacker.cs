using UnityEngine;

/// <summary>
/// Serializable class containing all attackable parameters
/// </summary>



/// <summary>
/// Interface for providing attack functionality to agents
/// </summary>
public class Attacker: MonoBehaviour
{
    
    void Awake()
    {
        var agent = GetComponent<Agent>();
        if (agent == null)
        {
            Debug.LogError("Attacker requires an Agent component to function properly.");
            return;
        }
        baseDamage = agent.baseStats.baseDamage;
        attackSuccessChance = agent.baseStats.attackSuccessChance;
        damageRandomization = agent.baseStats.damageRandomization;
        criticalChance = agent.baseStats.criticalChance;
        criticalDamageMultiplier = agent.baseStats.criticalDamageMultiplier;
        attackCooldown = agent.baseStats.attackCooldown;
    }

    public float baseDamage;
    public float attackSuccessChance;
    public float damageRandomization;
    public float criticalChance;
    public float criticalDamageMultiplier;

    public float attackCooldown;
    
    

    /// <summary>
    /// Attempts to attack another agent with random success chance and damage
    /// </summary>
    /// <param name="target">The target agent to attack</param>
    /// <returns>True if the attack was successful, false otherwise</returns>
    public bool TryAttack(Agent target)
    {
        // Check if attack is successful based on attackSuccessChance
        bool isSuccessful = Random.value <= attackSuccessChance;
    
        if (isSuccessful && target != null)
        {
            // Determine if this is a critical hit
            bool isCritical = Random.value <= criticalChance;
        
            // Calculate damage
            float damage = CalculateDamage(isCritical);
        
            // Apply damage to target
            target.TakeDamage(damage);
        }

        Debug.Log("Attack " + (isSuccessful ? "succeeded" : "failed") + " against " + target.name);
        
        return isSuccessful;
    }


    /// <summary>
    /// Calculates damage for an attack with randomization
    /// </summary>
    /// <param name="isCritical">Whether this is a critical hit</param>
    /// <returns>The final damage amount</returns>
    float CalculateDamage(bool isCritical)
    {
        // Base damage
        float damage = baseDamage;
    
        // Apply randomization
        float randomFactor = 1f + Random.Range(-damageRandomization, damageRandomization);
        damage *= randomFactor;
    
        // Apply critical hit multiplier if applicable
        if (isCritical)
        {
            damage *= criticalDamageMultiplier;
        }
    
        return damage;
    }

 
}