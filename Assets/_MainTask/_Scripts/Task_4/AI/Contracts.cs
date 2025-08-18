using UnityEngine;

public interface IMovement
{
    void Search();
    void Chase(Transform target);
    void RetreatFrom(Vector3 point);
}

public interface ICombat
{
    float Attack(Transform target);     // returns damage dealt (for fitness/feedback)
    bool IsDead { get; }
    float LastDamageTaken { get; }      // read/then reset by consumer each Update
    bool TryingToEnterSafeHaven { get; } // set by movement/path logic if needed
}

public interface IHealer
{
    bool HasPotion { get; }
    bool Drink(); // heals and consumes a potion
}

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