using UnityEngine;
using GOAP.Interfaces;

public class ResourceDropOnDeath : MonoBehaviour
{
    [Tooltip("Generic pickup prefab to spawn on death; will be configured to the carried type.")]
    public ResourcePickup pickupPrefab;

    private Damageable dmg;
    private IResourceCarrier carrier;
    private bool hasDropped; // ensures we never drop twice

    void Awake()
    {
        dmg = GetComponent<Damageable>();
        carrier = GetComponent<IResourceCarrier>();

        if (dmg != null) dmg.onDeath += HandleDeath;
    }

    void OnDestroy()
    {
        if (dmg != null) dmg.onDeath -= HandleDeath;
    }

    void HandleDeath(Damageable _)
    {
        if (hasDropped) return;                 // safety: only once
        if (carrier == null) return;            // only agents with IResourceCarrier
        if (carrier.CurrentResource == null) return; // drop ONLY if carrying something

        // Spawn a fresh pickup at the death spot with the SAME resource type
        var carried = carrier.CurrentResource;        // the exact item that agent was holding
        var drop = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        drop.ResourceType = carried.ResourceType;
        drop.Discovered = true;                       // let sensors notice it immediately
        // No manager crediting, no counts changed, no reservations touched.

        hasDropped = true;
    }
}