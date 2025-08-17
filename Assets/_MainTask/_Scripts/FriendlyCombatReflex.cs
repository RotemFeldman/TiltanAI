using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CombatAgent)), RequireComponent(typeof(NavMeshAgent))]
public class FriendlyCombatReflex : MonoBehaviour
{
    public float detectRadius = 8f;
    public LayerMask enemyMask;
    public bool canDrinkPotion = true;
    public LayerMask potionMask;
    public float lowHp = 0.35f;

    private CombatAgent combat;
    private NavMeshAgent agent;

    void Awake(){ combat = GetComponent<CombatAgent>(); agent = GetComponent<NavMeshAgent>(); }

    void Update()
    {
        var h01 = combat.Health01;

        // drink potion if low & near
        if (canDrinkPotion && h01 < lowHp)
        {
            var p = FindNearest(transform.position, detectRadius, potionMask);
            if (p){ agent.SetDestination(p.position); return; }
        }

        // attack if enemy in range; otherwise do nothing (GOAP keeps doing its job)
        var e = FindNearest(transform.position, detectRadius, enemyMask);
        if (e)
        {
            float d = Vector3.Distance(transform.position, e.position);
            if (d > combat.attackRange) agent.SetDestination(e.position);
            else combat.TryAttack(e);
        }
    }

    Transform FindNearest(Vector3 pos, float r, LayerMask mask)
    {
        Transform best = null; float bestD = float.PositiveInfinity;
        var cols = Physics.OverlapSphere(pos, r, mask);
        foreach (var c in cols)
        {
            float d = (c.transform.position - pos).sqrMagnitude;
            if (d < bestD){ bestD = d; best = c.transform; }
        }
        return best;
    }
}