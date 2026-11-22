using System.Collections.Generic;
using UnityEngine;

/*
 * GravitySource
 * -------------
 * Put this on every planet (or on a special zone) that should pull the
 * player. It replaces the normal Unity gravity.
 *
 * - All the GravitySource in the scene save themselves in a static list
 *   ("all") when they are enabled. So PlayerController can ask
 *   GravitySource.GetBest(position) without having a reference to the planets.
 * - The gravity always points to the centre of the object (transform.position).
 * - "rule" can make the gravity normal, low (multiplied by lowGravityMultiplier)
 *   or reversed (pushes away from the planet).
 * - "influenceRadius" limits the zone. 0 = the planet pulls from everywhere.
 * - "priority" is used when 2 zones overlap: the higher one wins. If they
 *   have the same priority, the closest one wins.
 *
 * Simplification: the force is the same at any distance (it is not like
 * real gravity that gets weaker when you go far). It's easier to control
 * for the gameplay.
 *
 * Note: GetBest is called every physics step and loops over all the sources.
 * With a few planets it's ok, with hundreds it could be slow.
 */
public class GravitySource : MonoBehaviour
{
    public enum Rule { Normal, Low, Reversed }

    [Header("Rule")]
    public Rule rule = Rule.Normal;

    [Tooltip("ON = the player's keys (1/2/3) choose the gravity here. OFF = this source's own rule is used")]
    public bool playerCanChangeGravity = true;
    public float strength = 20f;
    [Range(0.05f, 1f)] public float lowGravityMultiplier = 0.25f;

    [Header("Zone")]
    [Tooltip("0 = infinite range")]
    public float influenceRadius = 0f;
    [Tooltip("Higher priority wins when zones overlap (use for localized zones)")]
    public int priority = 0;

    // Static list of every active source in the scene (registered on enable)
    static readonly List<GravitySource> all = new List<GravitySource>();
    void OnEnable()  => all.Add(this);
    void OnDisable() => all.Remove(this);

    // We compare squared distances (sqrMagnitude) because it's faster than
    // computing a square root.
    public bool InRange(Vector3 p) =>
        influenceRadius <= 0f || (p - transform.position).sqrMagnitude <= influenceRadius * influenceRadius;

    // Gravity acceleration at a world position, using this source's own rule
    public Vector3 GetGravity(Vector3 p) => GetGravity(p, rule);

    // Same, but with a rule chosen by the caller (GravityMode uses this)
    // Direction = toward the centre, size = strength (changed by the rule).
    public Vector3 GetGravity(Vector3 p, Rule ruleToUse)
    {
        Vector3 toCenter = (transform.position - p).normalized;
        float s = strength;
        if (ruleToUse == Rule.Low) s *= lowGravityMultiplier;
        if (ruleToUse == Rule.Reversed) s = -s;
        return toCenter * s;
    }

    // Which source currently affects this position?
    // We look at all the sources in range and keep the best one:
    // first the highest priority, then the closest one.
    // Returns null if the position is in no zone (the player will float).
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