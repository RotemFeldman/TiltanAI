using UnityEngine;

public interface IAwareness
{
    Transform Target { get; }
    Vector3 ThreatCenter { get; }
    int AlliesNearby { get; }
    int EnemiesNearby { get; }
    float Health01 { get; }
    bool InAttackRange { get; }
    bool CanSeeTarget { get; }
    bool PotionInRange { get; }
    float DistToTarget01 { get; }
    float DistToPotion01 { get; }
    bool NearSafeHaven { get; }
}

public interface IMovement
{
    void Search();
    void Chase(Transform target);
    void RetreatFrom(Vector3 point);
}

public interface ICombat
{
    bool IsDead { get; }
    float LastDamageTaken { get; }
    MeleeWeapon weapon { get; }
    float Attack(Transform target);
    void SetTryingToEnterSafe(bool v);
}

public interface IHealer
{
    bool HasPotion { get; }
    int potionCount { get; set; }
    bool Drink();
}