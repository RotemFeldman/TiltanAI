using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class SafeHaven : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public Transform redirectPoint;   // place just outside the haven
    public float pushForce = 10f;

    void Reset() { var c = GetComponent<Collider>(); c.isTrigger = true; }

    void OnTriggerEnter(Collider other) => Handle(other);
    void OnTriggerStay(Collider other)  => Handle(other);

    void Handle(Collider other)
    {
        if (!other.CompareTag(enemyTag)) return;

        var agent = other.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            Vector3 target = redirectPoint ? redirectPoint.position
                : other.transform.position + (other.transform.position - transform.position).normalized * 5f;

            if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }
    
    void OnValidate()
    {
        var c = GetComponent<Collider>();
        if (c && !c.isTrigger) c.isTrigger = true;
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        var col = GetComponent<Collider>();
        if (!col) return;

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sph)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sph.center, sph.radius);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}