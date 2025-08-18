using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SafeHaven : MonoBehaviour
{
    [Tooltip("Radius in meters that is protected (enemies cannot enter).")]
    public float radius = 7.8f;

    [Tooltip("How far outside the edge to clamp destinations.")]
    public float edgePadding = 0.5f;

    [Tooltip("Only agents on these layers are affected (set this to Enemies).")]
    public LayerMask affectsLayers;

    static readonly List<SafeHaven> all = new();

    void OnEnable()  { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public static bool Any => all.Count > 0;

    bool AppliesTo(GameObject agent) =>
        agent != null && ((affectsLayers.value & (1 << agent.layer)) != 0);

    /// Clamp a desired destination so it stays OUTSIDE all matching safe havens for this agent.
    public static Vector3 ClampDestinationFor(GameObject agent, Vector3 desired)
    {
        if (agent == null || all.Count == 0) return desired;

        Vector3 result = desired;
        foreach (var h in all)
        {
            if (h == null || !h.AppliesTo(agent)) continue;

            var center = h.transform.position;
            center.y = result.y;

            Vector3 to = result - center;
            float dist = to.magnitude;

            // If desired is inside the haven, push it to just outside the edge.
            float edge = Mathf.Max(0.01f, h.radius + h.edgePadding);
            if (dist < edge)
            {
                // If the point is exactly at center, pick a direction away from agent pos
                if (dist < 1e-3f)
                {
                    Vector3 fromAgent = result - agent.transform.position;
                    if (fromAgent.sqrMagnitude < 1e-4f) fromAgent = Vector3.right;
                    to = fromAgent.normalized;
                }
                else to /= dist;

                result = center + to * edge;
            }
        }
        return result;
    }

    /// True if the agent is currently within any haven that applies to them.
    public static bool IsInsideFor(GameObject agent, Vector3 pos, out SafeHaven inside)
    {
        inside = null;
        if (agent == null) return false;
        foreach (var h in all)
        {
            if (h == null || !h.AppliesTo(agent)) continue;
            var center = h.transform.position; center.y = pos.y;
            if ((pos - center).sqrMagnitude < (h.radius * h.radius)) { inside = h; return true; }
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawSphere(transform.position, Mathf.Max(0.01f, radius));
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, radius));
    }
}
