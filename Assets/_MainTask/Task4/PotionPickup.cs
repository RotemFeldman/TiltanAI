using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PotionPickup : MonoBehaviour
{
    [Header("Potion")]
    [Tooltip("How many potions this pickup grants.")]
    public int amount = 1;

    [Tooltip("Optional: limit to these layers (e.g., Allies | Enemies). Leave 0 = anyone.")]
    public LayerMask whoCanPick;

    [Header("Placement QoL")]
    [Tooltip("Snap the pickup to the ground at start using a downward raycast.")]
    public bool snapToGroundOnStart = true;
    [Tooltip("Layers considered ground for snapping. Leave ~0 to hit everything with colliders.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Start the ray this far above current position.")]
    public float rayStartHeight = 5f;
    [Tooltip("Lift the final position slightly above ground to avoid z-fighting.")]
    public float groundOffset = 0.1f;

    void Reset()
    {
        EnsureColliderAndRB();
    }

    void OnValidate()
    {
        // Keep prefab safe even if edited in the Inspector
        EnsureColliderAndRB();
    }

    void Awake()
    {
        EnsureColliderAndRB();
        if (snapToGroundOnStart)
            SnapToGround();
    }

    void EnsureColliderAndRB()
    {
        var col = GetComponent<Collider>();
        if (!(col is SphereCollider) && !(col is CapsuleCollider) && !(col is BoxCollider))
        {
            // add a small sphere if none present
            col = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.4f;
        }
        col.isTrigger = true; // triggers don't collide; they only fire OnTrigger

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;   // ✔ won't get pushed by physics
        rb.useGravity = false;   // ✔ won't fall
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // just to be neat
    }

    void SnapToGround()
    {
        var start = transform.position + Vector3.up * Mathf.Abs(rayStartHeight);
        if (Physics.Raycast(start, Vector3.down, out var hit, Mathf.Abs(rayStartHeight) * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + Vector3.up * Mathf.Max(0.0f, groundOffset);
        }
        // If no ground hit, we just keep current position.
    }

    void OnTriggerEnter(Collider other)
    {
        // Limit by layer if requested
        if (whoCanPick.value != 0)
        {
            if ((whoCanPick.value & (1 << other.gameObject.layer)) == 0)
                return;
        }

        // Find a healer on the entering object or its parents
        var healer = other.GetComponentInParent<SimpleHealer>();
        if (healer == null) return;

        healer.AddPotions(amount);
        Destroy(gameObject);
    }
}
