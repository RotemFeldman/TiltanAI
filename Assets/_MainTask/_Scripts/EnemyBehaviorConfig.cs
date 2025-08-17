using UnityEngine;
using Unity.Behavior;

[CreateAssetMenu(fileName="EnemyBehaviorConfig", menuName="AI/Enemy Behavior Config")]
public class EnemyBehaviorConfig : ScriptableObject
{
    [Header("Movement/Perception")]
    public float moveSpeed = 3.5f;
    public float angularSpeed = 720f;
    public float accel = 12f;
    public float visionRange = 18f;
    public float visionAngle = 160f;
    public float chaseStopDistance = 1.8f;

    [Header("Combat")]
    public float attackRange = 1.7f;
    public float attackCooldown = 1.25f;
    public float damage = 15f;

    [Header("Tactical Flavor")]
    public float patrolRadius = 12f;
    public float idleTime = 1.5f;
    public float bravery = 1f;     // 0..2 (cowardly to fearless)

    [Header("Optional BehaviorGraph (different tree per archetype)")]
    public BehaviorGraph graphAsset;   // leave null to use the simple FSM below
}