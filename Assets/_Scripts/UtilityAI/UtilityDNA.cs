using UnityEngine;

namespace UtilityAI
{
    public struct UtilityDNA
    {
        // Sensor
        public float SensorRange;
        
        // Health-related DNA
        public float healthPercentage;
        public float maxHealth;
        public float currentHealth;
        public bool isRegeneratingHealth;
        public bool isLowHealth;
        public bool isCriticalHealth;
        
        // Hunger-related DNA
        public float hungerLevel;
        public bool isHungry;
        public bool isStarving;
        
        // Combat-related DNA
        public float attackDamage;
        public float attackSpeed;
        public bool canAttack;
        
        // Movement-related DNA
        public float moveSpeed;
        public bool canMove;
        
        // Agent identification
        public AgentGroup agentGroup;
        public string agentName;
        
        // Thresholds for health states
        private const float LOW_HEALTH_THRESHOLD = 0.3f;
        private const float CRITICAL_HEALTH_THRESHOLD = 0.1f;
        private const float HUNGRY_THRESHOLD = 0.4f;
        private const float STARVING_THRESHOLD = 0.15f;
        
        // Constructor to create DNA from an Agent
        public UtilityDNA(Agent agent)
        {
            if (agent == null)
            {
                // Default values for null agent
                SensorRange = 0f;
                healthPercentage = 0f;
                maxHealth = 0f;
                currentHealth = 0f;
                isRegeneratingHealth = false;
                isLowHealth = false;
                isCriticalHealth = false;
                hungerLevel = 0f;
                isHungry = false;
                isStarving = false;
                attackDamage = 0f;
                attackSpeed = 0f;
                canAttack = false;
                moveSpeed = 0f;
                canMove = false;
                agentGroup = AgentGroup.None;
                agentName = "Unknown";
                return;
            }
            
            //Sensor
            SensorRange = agent.GetComponent<Sensor>().detectionRadius;
            // Health data
            healthPercentage = agent.HealthPercentage;
            maxHealth = agent.MaxHealth;
            currentHealth = agent.CurrentHealth;
            isRegeneratingHealth = agent.CurrentHealth < agent.MaxHealth;
            isLowHealth = healthPercentage <= LOW_HEALTH_THRESHOLD;
            isCriticalHealth = healthPercentage <= CRITICAL_HEALTH_THRESHOLD;
            
            // Hunger data
            hungerLevel = agent.CurrentHunger;
            isHungry = hungerLevel <= HUNGRY_THRESHOLD;
            isStarving = hungerLevel <= STARVING_THRESHOLD;
            
            // Combat data
            attackDamage = agent.AttackDamage;
            attackSpeed = agent.AttackSpeed;
            canAttack = attackDamage > 0 && !isCriticalHealth;
            
            // Movement data
            moveSpeed = agent.MoveSpeed;
            canMove = moveSpeed > 0 && !isCriticalHealth;
            
            // Agent identification
            agentGroup = agent.Group;
            agentName = agent.name;
        }
        
        // Helper methods for common checks
        public bool NeedsHealing => isLowHealth || isCriticalHealth;
        public bool NeedsFood => isHungry || isStarving;
        public bool IsInDanger => isCriticalHealth || isStarving;
        public bool IsHealthy => !isLowHealth && !isHungry;
        
        // Priority scoring for different needs (0-1 scale)
        public float HealthPriority => isCriticalHealth ? 1f : (isLowHealth ? 0.7f : 0f);
        public float HungerPriority => isStarving ? 0.9f : (isHungry ? 0.5f : 0f);
        public float CombatReadiness => canAttack && !isLowHealth ? 1f : 0f;
        
        // Overall survival priority (higher means more urgent)
        public float SurvivalPriority => Mathf.Max(
            isCriticalHealth ? 1f : 0f,
            isStarving ? 0.95f : 0f,
            isLowHealth ? 0.6f : 0f,
            isHungry ? 0.3f : 0f
        );
    }
}