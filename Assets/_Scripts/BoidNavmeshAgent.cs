using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BoidNavmeshAgent : MonoBehaviour
{
    public float speed;
    public float neighborRadius;
    public float separationDistance;
    public float alignmentWeight;
    public float cohesionWeight;
    public float separationWeight;

    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.stoppingDistance = 0f;
    }

    void Update()
    {
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        int neighborCount = 0;
        
        Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborRadius);
        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject == this.gameObject) continue;
            if (!neighbor.TryGetComponent<BoidNavmeshAgent>(out _)) continue;

            Vector3 toNeighbor = neighbor.transform.position - transform.position;
            float distance = toNeighbor.magnitude;

            alignment += neighbor.transform.forward;
            cohesion += neighbor.transform.position;

            if (distance < separationDistance)
                separation -= toNeighbor.normalized / Mathf.Max(distance, 0.01f);

            neighborCount++;
        }

        if (neighborCount > 0)
        {
            alignment = (alignment / neighborCount).normalized;
            cohesion = ((cohesion / neighborCount) - transform.position).normalized;
            separation = separation.normalized;
        }
        
        Vector3 moveDir =
            alignmentWeight * alignment +
            cohesionWeight * cohesion +
            separationWeight * separation;

        moveDir = moveDir.normalized;
        
        if (moveDir != Vector3.zero)
        {
            Vector3 nextPoint = transform.position + moveDir * (speed * Time.deltaTime);
            agent.SetDestination(nextPoint);
        }
        else
        {
            agent.SetDestination(transform.position);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationDistance);
    }
}