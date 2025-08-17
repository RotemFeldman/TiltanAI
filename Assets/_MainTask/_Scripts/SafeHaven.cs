using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class SafeHaven : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public Transform redirectPoint;   // place just outside the haven
    public float pushForce = 8f;

    void Reset() { var c = GetComponent<Collider>(); c.isTrigger = true; }

    void OnTriggerEnter(Collider other) => Handle(other);
    void OnTriggerStay(Collider other)  => Handle(other);

    void Handle(Collider other)
    {
        if (!other.CompareTag(enemyTag)) return;

        var agent = other.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (redirectPoint != null) agent.SetDestination(redirectPoint.position);
            else
            {
                var dir = (other.transform.position - transform.position).normalized;
                agent.SetDestination(other.transform.position + dir * 5f);
            }
        }

        var rb = other.attachedRigidbody;
        if (rb) rb.AddForce((other.transform.position - transform.position).normalized * pushForce, ForceMode.VelocityChange);
    }
}