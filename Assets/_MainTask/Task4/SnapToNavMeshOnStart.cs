using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-5)]
public class SnapToNavMeshOnStart : MonoBehaviour
{
    public float maxSampleDistance = 6f;
    void Start()
    {
        if (!NavMesh.SamplePosition(transform.position, out var hit, maxSampleDistance, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: No NavMesh under/near spawn.");
            return;
        }
        var agent = GetComponent<NavMeshAgent>();
        if (agent && agent.isOnNavMesh) agent.Warp(hit.position);
        else transform.position = hit.position;
    }
}