using UnityEngine;
using Unity.Behavior;

public class MageAgentBehavior : MonoBehaviour
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
        BehaviorGraph.BlackboardReference.GetVariable("MageActions", out var Action);
        switch (Action.ObjectValue.ToString())
        {
            case nameof(MageActions.SearchForResources):
                HandleSearchForResources();
                break;
            case nameof(MageActions.BuildEnchantedStaff):
                HandleEnchantedStaff();
                break;
            case nameof(MageActions.BuildRunedShield):
                HandleRunedShield();
                break;
            case nameof(MageActions.ConnectArtifacts):
                HandleConnectArtifacts();
                break;
            case nameof(MageActions.Idle):
                Debug.Log("MageAgent is currently idle.");
                break;
        }
    }

    private void HandleSearchForResources()
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
    
    private void HandleEnchantedStaff()
    {
        BehaviorGraph.BlackboardReference.GetVariable("BuildLocation", out var buildLocation);
        if (buildLocation.ObjectValue == null)
        {
            Debug.LogWarning("Build location not set for Mage. Cannot build Enchanted Staff.");
            return;
        }
        
        Debug.Log("Mage is building the Enchanted Staff...");
        // Simulate staff crafting process.
        BehaviorGraph.BlackboardReference.SetVariableValue("EnchantedStaffBuilt", true);
        BehaviorGraph.BlackboardReference.SetVariableValue("MageActions", MageActions.Idle);

        Debug.Log("Enchanted Staff has been successfully built!");
    }

    private void HandleRunedShield()
    {
        BehaviorGraph.BlackboardReference.GetVariable("BuildLocation", out var buildLocation);
        if (buildLocation.ObjectValue == null)
        {
            Debug.LogWarning("Build location not set for Mage. Cannot build Runed Shield.");
            return;
        }
        
        Debug.Log("Mage is building the Runed Shield...");
        // Simulate shield crafting process.
        BehaviorGraph.BlackboardReference.SetVariableValue("RunedShieldBuilt", true);
        BehaviorGraph.BlackboardReference.SetVariableValue("MageActions", MageActions.Idle);

        Debug.Log("Runed Shield has been successfully built!");
    }
    
    private void HandleConnectArtifacts()
    {
        BehaviorGraph.BlackboardReference.GetVariable("EnchantedStaffBuilt", out var staffBuilt);
        BehaviorGraph.BlackboardReference.GetVariable("RunedShieldBuilt", out var shieldBuilt);

        if (!(bool)staffBuilt.ObjectValue || !(bool)shieldBuilt.ObjectValue)
        {
            Debug.LogWarning("Cannot connect artifacts. Staff or Shield not built yet.");
            return;
        }

        BehaviorGraph.BlackboardReference.GetVariable("BuildLocation", out var buildLocation);
        if (buildLocation.ObjectValue == null)
        {
            Debug.LogWarning("Build location not set for Mage. Cannot connect artifacts.");
            return;
        }

        Debug.Log("Mage is connecting artifacts...");
        // Use animations or visual effects to show the connection process (if any)
        BehaviorGraph.BlackboardReference.SetVariableValue("CombinedArtifactsBuilt", true);
        BehaviorGraph.BlackboardReference.SetVariableValue("MageActions", MageActions.Idle);

        Debug.Log("Artifacts have been successfully combined into the Combined Magical Artifact!");
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
                BehaviorGraph.BlackboardReference.SetVariableValue("CurrentResource", nearestResource);

                Debug.Log("Mage found a resource: " + nearestResource.name);
                _foundResource = true;

                // Update the resource destination
                _resourceDestination = nearestResource.transform.position;
                HasReachedResourceDestination();
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
    
    private void SetRandomSearchDestination() 
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

    private void HasReachedSearchDestination()
    {
        float distanceToDestination = Vector3.Distance(transform.position, _searchDestination);
        if (distanceToDestination < 1f) 
        {
            _hasSearchDestination = false;
            BehaviorGraph.BlackboardReference.SetVariableValue("hasSearchDestination", _hasSearchDestination);
        }
    }

    private void HasReachedResourceDestination()
    {
        float distanceToDestination = Vector3.Distance(transform.position, _resourceDestination);
        if (distanceToDestination < 1f)
        {
            BehaviorGraph.BlackboardReference.SetVariableValue("HandledResource", true);
            BehaviorGraph.BlackboardReference.SetVariableValue("MageActions", MageActions.Idle);
            Debug.Log("Mage has reached the resource destination.");
        }
    }
    
    private void OnDrawGizmosSelected()
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