using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PotionPickup : MonoBehaviour
{
    public int amount = 1;

    void Reset()
    {
        var c = GetComponent<Collider>(); if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var healer = other.GetComponentInParent<SimpleHealer>() ?? other.GetComponent<SimpleHealer>();
        if (healer != null)
        {
            healer.potionCount += amount;
            Destroy(gameObject);
        }
    }
}