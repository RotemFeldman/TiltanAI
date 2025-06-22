using UnityEngine;
using System;

[Serializable]
public class AgentStats
{
    [Header("Base Stats")]
    [SerializeField] [Min(0)] public float maxHealth = 100f;
    [SerializeField] [Min(0)] public float maxEnergy = 100f;
    [SerializeField] [Range(0, 10)] public float healthRegenRate = 1f;
    [SerializeField] [Range(0, 10)] public float energyRegenRate = 5f;

    [Header("Defense Stats")]
    [SerializeField] [Min(0)] public float physicalDefense = 10f;
    [SerializeField] [Min(0)] public float magicalDefense = 10f;
    [SerializeField] [Range(0, 1)] public float elementalResistance = 0f;

    [Header("Offense Stats")]
    [SerializeField] public float baseDamage = 10f;
    [SerializeField] [Range(0, 1)] public float attackSuccessChance = 0.8f;
    [SerializeField] [Range(0, 0.5f)] public float damageRandomization = 0.2f;
    [SerializeField] [Range(0, 1)] public float criticalChance = 0.05f;
    [SerializeField] [Min(1f)] public float criticalDamageMultiplier = 1.5f;
    [SerializeField] [Min(0)] public float attackSpeed = 1f;
    [SerializeField] [Min(0)] public float attackCooldown = 0.5f;
    

    [Header("Movement Stats")]
    [SerializeField] [Min(0)] public float moveSpeed = 5f;
    [SerializeField] [Min(0)] public float rotationSpeed = 120f;
    
    [Header("Hunger and Sleep stats")] 
    [SerializeField] [Min(0)] public float hungerRate = 0.1f; // How fast hunger increases
    
}