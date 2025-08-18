using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SafeHaven : MonoBehaviour
{
    [Tooltip("Radius in meters that is protected (enemies cannot enter).")]
    public float radius = 25f;

    [Tooltip("How far outside the edge to clamp destinations.")]
    public float edgePadding = 0.5f;

    [Tooltip("Only agents on these layers are affected (e.g., Enemies).")]
    public LayerMask affectsLayers;

    static readonly List<SafeHaven> all = new();

    void OnEnable()  { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    bool AppliesTo(GameObject agent) =>
        agent != null && ((affectsLayers.value & (1 << agent.layer)) != 0);

    public static bool Any => all.Count > 0;

    // === Queries ===

    public static bool IsInsideFor(GameObject agent, Vector3 pos, out SafeHaven inside)
    {
        inside = null;
        if (agent == null) return false;
        foreach (var h in all)
        {
            if (h == null || !h.AppliesTo(agent)) continue;
            var c = h.transform.position; c.y = pos.y;
            if ((pos - c).sqrMagnitude < h.radius * h.radius) { inside = h; return true; }
        }
        return false;
    }

    public static bool IsPointInsideAnyFor(GameObject agent, Vector3 pos)
    {
        return IsInsideFor(agent, pos, out _);
    }

    /// If the TARGET is inside a haven that applies to this agent, return the nearest edge point.
    public static bool TryGetEdgePointTowardsTarget(GameObject agent, Vector3 targetPos, out Vector3 edgePoint)
    {
        edgePoint = targetPos;
        if (agent == null || all.Count == 0) return false;

        SafeHaven chosen = null;
        float best = float.PositiveInfinity;
        foreach (var h in all)
        {
            if (h == null || !h.AppliesTo(agent)) continue;
            var c = h.transform.position; c.y = targetPos.y;
            float r = h.radius;
            if ((targetPos - c).sqrMagnitude < r * r)
            {
                float d = (targetPos - c).sqrMagnitude;
                if (d < best) { best = d; chosen = h; }
            }
        }

        if (chosen == null) return false;

        var center = chosen.transform.position; center.y = targetPos.y;
        var dir = targetPos - center;
        if (dir.sqrMagnitude < 1e-4f) dir = Vector3.right; // degenerate case
        dir.Normalize();
        float edge = Mathf.Max(0.01f, chosen.radius + chosen.edgePadding);
        edgePoint = center + dir * edge;
        return true;
    }

    /// Clamp a desired destination so it stays OUTSIDE all matching safe havens for this agent.
    public static Vector3 ClampDestinationFor(GameObject agent, Vector3 desired)
    {
        if (agent == null || all.Count == 0) return desired;

        Vector3 result = desired;
        foreach (var h in all)
        {
            if (h == null || !h.AppliesTo(agent)) continue;

            var center = h.transform.position; center.y = result.y;
            Vector3 to = result - center;
            float dist = to.magnitude;

            float edge = Mathf.Max(0.01f, h.radius + h.edgePadding);
            if (dist < edge)
            {
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

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawSphere(transform.position, Mathf.Max(0.01f, radius));
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, radius));
    }
}
