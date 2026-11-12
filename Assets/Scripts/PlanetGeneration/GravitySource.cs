using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour
{
    public enum Rule { Normal, Low, Reversed }

    [Header("Rule")]
    public Rule rule = Rule.Normal;

    [Tooltip("ON = the player's keys (1/2/3) choose the gravity here. OFF = this source's own rule is used")]
    public bool playerCanChangeGravity = false;
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

    // Gravity acceleration at a world position, using this source's own rule
    public Vector3 GetGravity(Vector3 p) => GetGravity(p, rule);

    // Same, but with a rule chosen by the caller (GravityMode uses this)
    public Vector3 GetGravity(Vector3 p, Rule ruleToUse)
    {
        Vector3 toCenter = (transform.position - p).normalized;
        float s = strength;
        if (ruleToUse == Rule.Low) s *= lowGravityMultiplier;
        if (ruleToUse == Rule.Reversed) s = -s;
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