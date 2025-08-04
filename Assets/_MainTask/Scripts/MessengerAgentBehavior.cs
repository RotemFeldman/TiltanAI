using UnityEngine;
using Unity.Behavior;

public class MessengerAgentBehavior : MonoBehaviour
{
    [Header("Graph References")]
    [SerializeField] private BehaviorGraphAgent behaviorGraph;

    [Header("Search Settings")]
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private float maxSearchDistance = 20f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Delivery Settings")]
    [SerializeField] private Transform buildLocation; // Assign this in the inspector
    [SerializeField] private float destinationThreshold = 1f;

    private Vector3 _pickupLocation;
    private Vector3 _searchDestination;
    private bool _hasSearchDestination = false;

    private void Awake()
    {
        behaviorGraph = GetComponent<BehaviorGraphAgent>();
    }

    private void Update()
    {
        behaviorGraph.BlackboardReference.GetVariable("MessengerActions", out var Action);

        switch (Action.ObjectValue.ToString())
        {
        case nameof(MessengerActions.CarryResourceToBuild):
            if (!_hasSearchDestination)
            {
                SetRandomSearchDestination(); // Find a random pickup location
            }
            else
            {
                // Retrieve "AtPickupLocation" variable and check its value
                behaviorGraph.BlackboardReference.GetVariable("AtPickupLocation", out var atPickupLocationVariable);
                bool atPickupLocation = atPickupLocationVariable != null && (bool)atPickupLocationVariable.ObjectValue;

                if (!atPickupLocation)
                {
                    CheckIfReachedPickupLocation();
                }
                else
                {
                    CheckIfReachedBuildLocation(); // Proceed to building location after pickup
                }
            }
            break;

        case nameof(MessengerActions.Idle):
            CheckIfReturnedToIdle();
            break;
    }
}

    private void SetRandomSearchDestination()
    {
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(5f, maxSearchDistance);

        Vector3 direction = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.forward;
        Vector3 randomPoint = transform.position + direction * randomDistance;

        if (Physics.Raycast(randomPoint + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            _pickupLocation = hit.point;
            _searchDestination = hit.point;
            _hasSearchDestination = true;

            behaviorGraph.BlackboardReference.SetVariableValue("PickupLocation", _pickupLocation);
            behaviorGraph.BlackboardReference.SetVariableValue("hasSearchDestination", _hasSearchDestination);
        }
        else
        {
            SetRandomSearchDestination(); // Retry
        }
    }

    private void CheckIfReachedPickupLocation()
    {
        float distance = Vector3.Distance(transform.position, _pickupLocation);
        if (distance <= destinationThreshold)
        {
            _hasSearchDestination = false;
            behaviorGraph.BlackboardReference.SetVariableValue("AtPickupLocation", true);
            behaviorGraph.BlackboardReference.SetVariableValue("hasSearchDestination", false);
        }
    }

    private void CheckIfReachedBuildLocation()
    {
        if (buildLocation == null)
            return;

        float distance = Vector3.Distance(transform.position, buildLocation.position);
        if (distance <= destinationThreshold)
        {
            behaviorGraph.BlackboardReference.SetVariableValue("AtBuildLocation", true);
        }
    }

    private void CheckIfReturnedToIdle()
    {
        behaviorGraph.BlackboardReference.GetVariable("Idle", out var idle);
        Vector3 idlePos = (Vector3)idle.ObjectValue;
        if (Vector3.Distance(transform.position, idlePos) <= destinationThreshold)
        {
            behaviorGraph.BlackboardReference.SetVariableValue("Action", MessengerActions.Idle);
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

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxSearchDistance);
    }
}