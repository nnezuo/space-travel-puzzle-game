using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour
{
    public enum Rule { Normal, Low, Reversed }

    [Header("Rule")]
    public Rule rule = Rule.Normal;
    public float strength = 20f;
    [Range(0.05f, 1f)] public float lowGravityMultiplier = 0.25f;

    [Header("Zone")]
    [Tooltip("0 = infinite range")]
    public float influenceRadius = 0f;
    [Tooltip("Higher priority wins when zones overlap (use for localized zones)")]
    public int priority = 0;

    static readonly List<GravitySource> all = new List<GravitySource>();
    void OnEnable()  => all.Add(this);
    void OnDisable() => all.Remove(this);

    public bool InRange(Vector3 p) =>
        influenceRadius <= 0f || (p - transform.position).sqrMagnitude <= influenceRadius * influenceRadius;

    // Returns the gravity ACCELERATION vector at a world position
    public Vector3 GetGravity(Vector3 p)
    {
        Vector3 toCenter = (transform.position - p).normalized;
        float s = strength;
        if (rule == Rule.Low) s *= lowGravityMultiplier;
        if (rule == Rule.Reversed) s = -s;
        return toCenter * s;
    }

    // Which source currently affects this position?
    public static GravitySource GetBest(Vector3 p)
    {
        GravitySource best = null;
        float bestDist = float.MaxValue;
        foreach (var g in all)
        {
            if (!g.InRange(p)) continue;
            float d = (g.transform.position - p).sqrMagnitude;
            if (best == null || g.priority > best.priority ||
                (g.priority == best.priority && d < bestDist))
            {
                best = g; bestDist = d;
            }
        }
        return best;
    }
}