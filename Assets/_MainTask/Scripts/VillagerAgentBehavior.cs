using System;
using UnityEngine;
using Unity.Behavior;
using Random = System.Random;
using Unity.Mathematics;

public class VillagerAgentBehavior : MonoBehaviour
{
    [SerializeField] private BehaviorGraphAgent BehaviorGraph;
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private LayerMask resourceLayer; // Set this in inspector to match your resource objects' layer
    [SerializeField] private float searchInterval = 0.5f; // How often to perform the sphere check
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxSearchDistance = 20f; 
    
    private Vector3 _searchDestination;
    private Vector3 _resourceDestination;
    private bool _hasSearchDestination = false;
    private bool _foundResource = false;
    
    private void Awake()
    {
        BehaviorGraph = GetComponent<BehaviorGraphAgent>();
    }

    private void Update()
    {
        BehaviorGraph.BlackboardReference.GetVariable("VillagerActions", out var Action);
        if (Action.ObjectValue.ToString() == nameof(VillagerActions.SearchForResources))
        {
            if (!_hasSearchDestination)
            {
                SetRandomSearchDestination();
            }
            else
            {
                HasReachedSearchDestination();
                SearchForResources();
            }
        }
        else if (Action.ObjectValue.ToString() == nameof(VillagerActions.ChopTree))
        {
            HasReachedResourceDestination();
        }
        else if (Action.ObjectValue.ToString() == nameof(VillagerActions.RefineCrystals))
        {
            HasReachedResourceDestination();
        }
        else if (Action.ObjectValue.ToString() == nameof(VillagerActions.CollectIronIngot))
        {
            HasReachedResourceDestination();
        }
    }

    private void SearchForResources()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, searchRadius, resourceLayer);
        
        if (hitColliders.Length > 0)
        {
            // Found at least one resource
            GameObject nearestResource = FindNearestResource(hitColliders);
            if (nearestResource != null)
            {
                if (nearestResource.layer == 11)
                {
                    BehaviorGraph.BlackboardReference.SetVariableValue("VillagerActions", VillagerActions.ChopTree);
                    BehaviorGraph.BlackboardReference.SetVariableValue("resourceObj", nearestResource);
                }

                if (nearestResource.layer == 12 || nearestResource.layer == 13)
                {
                    BehaviorGraph.BlackboardReference.SetVariableValue("VillagerActions", VillagerActions.TaskCompleted);
                }

            }
        }
    }
    
    private GameObject FindNearestResource(Collider[] resources)
    {
        GameObject nearest = null;
        float nearestDistance = float.MaxValue;
        Vector3 currentPosition = transform.position;

        foreach (Collider resource in resources)
        {
            float distance = Vector3.Distance(currentPosition, resource.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = resource.gameObject;
            }
        }
        return nearest;
    }

    private void SetRandomSearchDestination() //
    {
        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        float randomDistance = UnityEngine.Random.Range(5f, maxSearchDistance);

        Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
        Vector3 randomPoint = transform.position + (randomDirection * randomDistance);

        if (Physics.Raycast(randomPoint + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            _searchDestination = hit.point;
            _hasSearchDestination = true;
            BehaviorGraph.BlackboardReference.SetVariableValue("SearchDestination", _searchDestination);
            BehaviorGraph.BlackboardReference.SetVariableValue("hasSearchDestination", _hasSearchDestination);
        }
        else
        {
            SetRandomSearchDestination();
        }
    }

    private void HasReachedSearchDestination() //
    {
        float distanceToDestination = Vector3.Distance(transform.position, _searchDestination);
        if (distanceToDestination < 1f) 
        {
            _hasSearchDestination = false;
            BehaviorGraph.BlackboardReference.SetVariableValue("hasSearchDestination", _hasSearchDestination);
        }
    }
    private void HasReachedResourceDestination() //
    {
        float distanceToDestination = Vector3.Distance(transform.position, _resourceDestination);
        if (distanceToDestination < 13f)
        {
            //Debug.Log("12312313");
            BehaviorGraph.BlackboardReference.SetVariableValue("handelingMaterial", true);
            BehaviorGraph.BlackboardReference.SetVariableValue("reachedGatheredDist", true);
        }
    }
    
    private void OnDrawGizmosSelected() //
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
        if (_hasSearchDestination)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_searchDestination, 1f);
            Gizmos.DrawLine(transform.position, _searchDestination);
        }

        // Draw max search range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxSearchDistance);

    }
    
        

}