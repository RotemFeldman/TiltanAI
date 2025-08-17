// PotionPickup.cs
using UnityEngine;
public class PotionPickup : MonoBehaviour
{
    // Example: heal or buff; tweak as needed
    public float heal = 35f;

    void OnTriggerEnter(Collider other)
    {
        var dmg = other.GetComponent<Damageable>();
        if (dmg != null)
        {
            dmg.currentHP = Mathf.Min(dmg.maxHP, dmg.currentHP + heal);
            Destroy(gameObject);
        }
    }
}