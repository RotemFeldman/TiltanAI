using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PotionPickup : MonoBehaviour
{
    public int amount = 1;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Accept pick-ups from either side (enemies or villagers with SimpleHealer)
        var healer = other.GetComponentInParent<SimpleHealer>();
        if (healer == null) healer = other.GetComponent<SimpleHealer>();
        if (healer != null)
        {
            healer.potionCount += amount;
            Destroy(gameObject);
        }
    }
}