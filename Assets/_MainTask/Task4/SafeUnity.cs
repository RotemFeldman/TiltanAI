using UnityEngine;

public static class SafeUnity
{
    /// Tries to read t.position, returns false if t is null or destroyed.
    public static bool TryGetPosition(Transform t, out Vector3 pos)
    {
        pos = default;
        if (t == null) return false;
        try { pos = t.position; return true; }
        catch (MissingReferenceException) { return false; }
    }

    /// Planar distance check with null/destroy guards.
    public static bool SafePlanarWithin(Transform a, Transform b, float radius)
    {
        if (a == null || b == null) return false;
        if (!TryGetPosition(a, out var pa) || !TryGetPosition(b, out var pb)) return false;
        pa.y = pb.y = 0f;
        return (pa - pb).sqrMagnitude <= radius * radius;
    }
}