using System.Collections.Generic;
using UnityEngine;

/*
 * PlanetNoise
 * -----------
 * This is the "terrain recipe" of the planet. For any direction from the
 * centre of the planet, it tells how high the ground is (GetHeight).
 * CubeSphereGenerator calls it for every vertex, and PlanetScatter,
 * PortalSpawner, RiverWater... also use it to know where the ground is.
 *
 * The height is made with several layers added together:
 *   1. gentle hills (fractal noise = "fbm")
 *   2. mountains, only in some regions (a "mask" says where)
 *   3. rivers dug into the ground
 *   4. the hills are made flatter along the paths
 *   5. a flat platform for the portal
 *
 * We wrote our OWN 3D Perlin noise (Noise3) instead of using
 * Mathf.PerlinNoise, because Mathf.PerlinNoise is only 2D. With 2D noise on
 * a sphere we would get stretched shapes and seams. With 3D noise we can
 * just give the (x, y, z) of the direction and it is smooth everywhere.
 *
 * The seed shuffles the permutation table of the noise, so a different seed
 * = a different planet, and the same seed = always the same planet.
 *
 * Paths: we make some "spokes" = arcs on the sphere that start at the portal
 * and go in different directions. A point is "on a path" if it is close to
 * one of these arcs. The paths are a bit wavy (PathWobble) so they look like
 * real trails and not perfect lines.
 *
 * Things to know / improve:
 *  - All the heights are in world units and are NOT scaled with the radius
 *    of the planet. If we change the radius we must change the heights too
 *    (see the comment just below).
 *  - It is a bit slow: for one vertex, Warp() and PathDistance() are
 *    computed again in GetHeight, GetRiver and GetPath. We could compute
 *    everything one time per vertex and return it together.
 *  - The units are mixed: portalZoneRadius is a "fraction of 180 degrees"
 *    but the path settings are in degrees. It's confusing in the Inspector.
 */
public class PlanetNoise : MonoBehaviour
{
    // Defaults are tuned for a planet of radius ~30. For radius 10, divide the heights by 3.

    [Header("Portal Platform Zone")]
    public bool enablePortalZone = true;
    public Vector3 portalDirection = new Vector3(0f, 1f, 0f);   // top of the planet by default
    [Tooltip("Flat area size as a fraction of 180 degrees (0.08 = about 14 degrees)")]
    public float portalZoneRadius = 0.08f;
    [Tooltip("Smooth blending edge, same units as the radius")]
    public float portalTransitionBlend = 0.04f;

    [Header("Paths to the portal")]
    public bool enablePaths = true;
    [Range(0, 8)] public int pathCount = 4;
    [Tooltip("How far each random path goes from the portal, in degrees of arc")]
    [Range(20f, 120f)] public float pathLength = 70f;
    [Tooltip("Optional: directions (from the planet centre) that must be connected to the portal, e.g. the beacon. Leave empty for random paths only.")]
    public Vector3[] pathTargets;
    [Tooltip("Half width of the painted path, in degrees of arc (1 degree = about 0.5 units at radius 30)")]
    [Range(0.5f, 8f)] public float pathHalfWidth = 2.5f;
    [Tooltip("Softness of the painted path edge, in degrees")]
    [Range(0.5f, 6f)] public float pathEdgeBlend = 2f;
    [Tooltip("How far beside the path the terrain is calmed (mountains and rivers fade out), in degrees")]
    [Range(2f, 20f)] public float pathShoulder = 10f;
    [Tooltip("Fraction of hill height kept on the path (0 = perfectly flat, 1 = untouched)")]
    [Range(0f, 1f)] public float pathFlatten = 0.3f;
    [Tooltip("How much the paths bend like natural trails (0 = straight)")]
    [Range(0f, 0.3f)] public float pathMeander = 0.1f;
    public float pathMeanderScale = 3f;

    [Header("Base terrain (gentle hills)")]
    public float noiseScale = 1.4f;
    [Tooltip("Approximate hill amplitude in world units")]
    public float height = 2.5f;
    [Range(1, 6)] public int octaves = 4;
    [Range(0.1f, 0.8f)] public float persistence = 0.45f;   // how much each finer layer counts
    [Range(1.5f, 3f)] public float lacunarity = 2f;         // how much finer each layer is

    [Header("Organic warp (bends everything so nothing looks gridlike)")]
    public float warpScale = 1.2f;
    [Range(0f, 0.8f)] public float warpStrength = 0.3f;

    [Header("Mountains")]
    public float mountainHeight = 7f;
    [Tooltip("LOWER = wider mountain ranges. HIGHER = more, smaller ranges")]
    public float mountainScale = 1.1f;
    [Range(1, 5)] public int mountainOctaves = 4;
    [Tooltip("How much of the planet is mountainous (0 = none, 1 = almost all)")]
    [Range(0f, 1f)] public float mountainCoverage = 0.45f;
    [Tooltip("1 = broad rounded peaks, 3 = narrow pointed peaks")]
    [Range(1f, 3f)] public float mountainSharpness = 1.4f;
    [Tooltip("Higher = only the strongest ridges rise; lower = wider mountain bodies")]
    [Range(0f, 0.9f)] public float ridgeFloor = 0.45f;
    [Tooltip("Higher = rounder ridge crests, lower = sharper crests")]
    [Range(0.02f, 0.5f)] public float ridgeSoftness = 0.2f;

    [Header("Rivers")]
    public float riverScale = 1.3f;
    [Range(0.005f, 0.1f)] public float riverWidth = 0.05f;
    public float riverDepth = 0.8f;

    [Header("Seed")]
    public int seed = 12345;

    // Perlin noise rarely reaches +-1 (typical +-0.6); this makes "height" the real amplitude
    const float NoiseGain = 1.7f;

    // Fixed offsets so each layer samples a different region of the noise
    static readonly Vector3 OffHills = new Vector3(17.3f, 41.1f, 5.7f);
    static readonly Vector3 OffMask = new Vector3(63.9f, 8.4f, 29.2f);
    static readonly Vector3 OffMountains = new Vector3(3.1f, 77.7f, 52.6f);
    static readonly Vector3 OffRiver = new Vector3(91.5f, 22.8f, 14.3f);

    // One path = a great-circle arc from the portal (a) to an end point (b)
    struct Spoke { public Vector3 a, b, n; }   // n = unit normal of the arc's plane

    int[] perm;                 // permutation table (built from the seed)
    int builtSeed;
    Spoke[] spokes = new Spoke[0];
    Vector3 portalDir = Vector3.up;
    bool dirty = true;          // set when an Inspector value changes, so the paths rebuild

    void Awake() => ApplySeed();

    // Public so the generator and scatter can call it in Edit mode
    public void ApplySeed()
    {
        BuildTable();
        BuildSpokes();
        dirty = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        octaves = Mathf.Max(1, octaves);
        mountainOctaves = Mathf.Max(1, mountainOctaves);
        dirty = true;
        var gen = GetComponent<CubeSphereGenerator>();
        if (gen != null) gen.EditorRefresh();
    }
#endif

    // Rebuild automatically if anything is missing or an Inspector value changed
    // (every public function calls this first, so we never use an old table
    // or old paths, even in Edit mode where Awake is not called)
    void EnsureReady()
    {
        if (perm == null || dirty || builtSeed != seed) ApplySeed();
    }

    // ---------------------------------------------------------------
    // Seed: shuffle 0..255 with a seeded random generator
    // ---------------------------------------------------------------
    // This is the classic Perlin noise table. It is the only "random" part
    // of the noise: the noise uses it to choose a gradient direction at each
    // corner of the grid. The shuffle is the Fisher-Yates shuffle.
    void BuildTable()
    {
        int[] p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;

        var rng = new System.Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int tmp = p[i]; p[i] = p[j]; p[j] = tmp;
        }

        perm = new int[512];                       // doubled so lookups never wrap
        for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
        builtSeed = seed;
    }

    // ---------------------------------------------------------------
    // Paths: build the list of arcs that start at the portal
    // ---------------------------------------------------------------
    // We split the circle around the portal in pathCount parts (sectors) and
    // put one path in each part, with a small random angle. This way the
    // paths are not all on the same side. Each path ends at a random length
    // between 75% and 100% of pathLength.
    // The "end" point uses cos / sin to walk "len" radians on the sphere from
    // the portal in the direction "tangent".
    void BuildSpokes()
    {
        var list = new List<Spoke>();
        portalDir = portalDirection.sqrMagnitude > 1e-6f ? portalDirection.normalized : Vector3.up;

        if (enablePaths)
        {
            // Two directions lying on the ground around the portal (tangent plane)
            Vector3 helper = Mathf.Abs(portalDir.y) < 0.9f ? Vector3.up : Vector3.right;
            Vector3 t1 = Vector3.Cross(portalDir, helper).normalized;
            Vector3 t2 = Vector3.Cross(portalDir, t1);

            // Random paths: one per sector around the portal, with seeded jitter
            var rng = new System.Random(seed + 777);
            for (int i = 0; i < pathCount; i++)
            {
                float sector = 2f * Mathf.PI / pathCount;
                float bearing = (i + (float)rng.NextDouble() * 0.5f) * sector;
                float len = pathLength * Mathf.Deg2Rad * Mathf.Lerp(0.75f, 1f, (float)rng.NextDouble());

                Vector3 tangent = t1 * Mathf.Cos(bearing) + t2 * Mathf.Sin(bearing);
                Vector3 end = portalDir * Mathf.Cos(len) + tangent * Mathf.Sin(len);   // point on the sphere
                AddSpoke(list, end);
            }

            // Paths to the targets you list in the Inspector
            if (pathTargets != null)
                foreach (var t in pathTargets)
                    if (t.sqrMagnitude > 1e-6f) AddSpoke(list, t.normalized);
        }

        spokes = list.ToArray();
    }

    void AddSpoke(List<Spoke> list, Vector3 end)
    {
        Vector3 n = Vector3.Cross(portalDir, end);
        if (n.sqrMagnitude < 1e-6f) return;        // same or opposite direction: no unique arc
        list.Add(new Spoke { a = portalDir, b = end, n = n.normalized });
    }

    // Angular distance (radians) from point p to the arc a->b of one spoke
    // This is the hardest math of the file. The arc is part of a big circle
    // on the sphere, and this circle is in a plane (normal n).
    //  - First we measure how far p is from that plane ("side").
    //  - Then we check if p is between the 2 ends of the arc. If yes, the
    //    distance is just asin(side).
    //  - If not, p is "after" one of the ends, so we take the distance to
    //    the closest end point instead.
    static float DistToArc(Vector3 p, Spoke s)
    {
        float side = Vector3.Dot(p, s.n);               // how far p is from the arc's plane
        Vector3 q = p - s.n * side;                     // p projected onto that plane
        float qm = q.magnitude;
        if (qm > 1e-5f)
        {
            q /= qm;
            // q is "between" a and b if it is on the correct side of both end points
            if (Vector3.Dot(Vector3.Cross(s.a, q), s.n) >= 0f &&
                Vector3.Dot(Vector3.Cross(q, s.b), s.n) >= 0f)
                return Mathf.Asin(Mathf.Clamp(Mathf.Abs(side), 0f, 1f));
        }
        // Beyond either end: distance to the nearest end point
        float da = Mathf.Acos(Mathf.Clamp(Vector3.Dot(p, s.a), -1f, 1f));
        float db = Mathf.Acos(Mathf.Clamp(Vector3.Dot(p, s.b), -1f, 1f));
        return Mathf.Min(da, db);
    }

    // Slightly bends the position before measuring, so paths meander like trails
    Vector3 PathWobble(Vector3 d)
    {
        if (pathMeander <= 0f) return d;
        Vector3 p = d * pathMeanderScale;
        Vector3 o = new Vector3(
            Noise3(p.x + 7.7f, p.y + 1.3f, p.z + 9.1f),
            Noise3(p.x + 21.2f, p.y + 4.4f, p.z + 13.8f),
            Noise3(p.x + 2.9f, p.y + 18.6f, p.z + 6.5f));
        return (d + o * pathMeander).normalized;
    }

    // Angular distance (radians) to the nearest path
    float PathDistance(Vector3 d)
    {
        if (spokes.Length == 0) return float.MaxValue;
        Vector3 p = PathWobble(d);
        float best = float.MaxValue;
        for (int i = 0; i < spokes.Length; i++)
            best = Mathf.Min(best, DistToArc(p, spokes[i]));
        return best;
    }

    // 1 inside halfWidth, fading smoothly to 0 over blend (both in degrees)
    static float PathMask(float dist, float halfDeg, float blendDeg)
    {
        if (dist == float.MaxValue) return 0f;
        float half = halfDeg * Mathf.Deg2Rad;
        return 1f - Smooth(half, half + blendDeg * Mathf.Deg2Rad, dist);
    }

    // ---------------------------------------------------------------
    // Noise building blocks
    // ---------------------------------------------------------------

    // Proper smoothstep: 0 below edge0, 1 above edge1, smooth S-curve between
    static float Smooth(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    // Smooth fade curve (6t^5 - 15t^4 + 10t^3): no visible grid lines in the noise
    static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    // Gradient: one of 12 directions chosen by the hash (Ken Perlin's improved noise)
    static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    // TRUE 3D gradient noise, roughly -1..1. No axis-aligned stretching, works for negative inputs
    // How it works (simple version):
    //  - space is cut in cubes of size 1. We find the cube that holds the point
    //    (xi, yi, zi) and where the point is inside it (xf, yf, zf from 0 to 1)
    //  - each of the 8 corners of the cube gets a "random" gradient from the
    //    perm table, and we compute its effect on the point (Grad)
    //  - we mix the 8 values with Lerp, using the Fade curve, first on x,
    //    then on y, then on z
    // This is based on Ken Perlin's "improved noise" (2002).
    float Noise3(float x, float y, float z)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y), zi = Mathf.FloorToInt(z);
        float xf = x - xi, yf = y - yi, zf = z - zi;
        xi &= 255; yi &= 255; zi &= 255;

        float u = Fade(xf), v = Fade(yf), w = Fade(zf);

        int a = perm[xi] + yi; int aa = perm[a] + zi; int ab = perm[a + 1] + zi;
        int b = perm[xi + 1] + yi; int ba = perm[b] + zi; int bb = perm[b + 1] + zi;

        float x1 = Mathf.Lerp(Grad(perm[aa], xf, yf, zf), Grad(perm[ba], xf - 1, yf, zf), u);
        float x2 = Mathf.Lerp(Grad(perm[ab], xf, yf - 1, zf), Grad(perm[bb], xf - 1, yf - 1, zf), u);
        float y1 = Mathf.Lerp(x1, x2, v);

        x1 = Mathf.Lerp(Grad(perm[aa + 1], xf, yf, zf - 1), Grad(perm[ba + 1], xf - 1, yf, zf - 1), u);
        x2 = Mathf.Lerp(Grad(perm[ab + 1], xf, yf - 1, zf - 1), Grad(perm[bb + 1], xf - 1, yf - 1, zf - 1), u);
        float y2 = Mathf.Lerp(x1, x2, v);

        return Mathf.Lerp(y1, y2, w);
    }

    // Rotates the sample point between octaves so the noise grid never lines up with itself
    static Vector3 Rotate(Vector3 p)
    {
        return new Vector3(
             0.80f * p.y + 0.60f * p.z,
            -0.80f * p.x + 0.36f * p.y - 0.48f * p.z,
            -0.60f * p.x - 0.48f * p.y + 0.64f * p.z);
    }

    // Fractal noise: several layers of finer and weaker noise, normalized to about -1..1
    // (fbm = "fractal Brownian motion"). The first layer makes the big shapes,
    // the next layers add smaller and smaller details:
    //   - each layer ("octave") is lacunarity times smaller (more detail)
    //   - and "gain" (persistence) times weaker
    // We divide by "norm" so the result stays around -1..1 whatever the
    // number of octaves.
    float Fbm(Vector3 p, int oct, float gain)
    {
        float sum = 0f, amp = 1f, norm = 0f;
        for (int i = 0; i < oct; i++)
        {
            sum += Noise3(p.x, p.y, p.z) * amp;
            norm += amp;
            amp *= gain;
            p = Rotate(p) * lacunarity;
        }
        return sum / norm;
    }

    // Ridged fractal noise with ROUNDED crests (no V-shaped creases), about 0..1.
    // Fine detail only appears on high ground, so valleys stay smooth to walk on.
    // The trick for mountains: normal noise is like soft hills, but if we take
    // 1 - |noise|, the places where the noise crosses 0 become sharp ridges
    // (like mountain chains). We use a "soft" abs (sqrt(n*n + s*s)) so the top
    // is a bit round. "weight" makes the next octave only strong where the
    // last one was high, so the details are on the mountain tops.
    float RidgedFbm(Vector3 p, int oct, float gain)
    {
        float sum = 0f, amp = 1f, norm = 0f, weight = 1f;
        float s = ridgeSoftness;
        for (int i = 0; i < oct; i++)
        {
            float n = Noise3(p.x, p.y, p.z) * 2f;
            float r = 1f - (Mathf.Sqrt(n * n + s * s) - s);   // smooth version of 1 - |n|
            r = Mathf.Clamp01(r);
            r *= r;
            r *= weight;
            weight = Mathf.Clamp01(r * 2f);

            sum += r * amp;
            norm += amp;
            amp *= gain;
            p = Rotate(p) * lacunarity;
        }
        return sum / norm;
    }

    // Domain warp: bends rivers into meanders and mountains into natural ranges
    // Instead of reading the noise at the point d, we first move d a bit with
    // ANOTHER noise, then we read the noise there. The result looks twisted
    // and more natural. Same idea as PathWobble above.
    Vector3 Warp(Vector3 d)
    {
        if (warpStrength <= 0f) return d;
        Vector3 p = d * warpScale;
        Vector3 offset = new Vector3(
            Noise3(p.x + 11.1f, p.y + 3.7f, p.z + 8.2f),
            Noise3(p.x + 31.4f, p.y + 17.9f, p.z + 2.3f),
            Noise3(p.x + 5.5f, p.y + 23.8f, p.z + 41.6f));
        return d + offset * warpStrength;
    }

    // ---------------------------------------------------------------
    // Terrain layers (take the already-warped position w)
    // ---------------------------------------------------------------

    // 0 = no mountains here, 1 = mountain region (soft edges)
    float MountainMask(Vector3 w)
    {
        float f = Fbm(w * (noiseScale * 0.5f) + OffMask, 2, 0.5f);
        float edge0 = Mathf.Lerp(0.25f, -0.25f, mountainCoverage);
        return Smooth(edge0, edge0 + 0.20f, f);
    }

    // 1 on the river line, fading smoothly to 0 at the banks (rounded channel)
    // A river is where the noise is close to 0: the line where the noise goes
    // from negative to positive makes a long curvy line, like a river.
    // riverWidth says how close to 0 counts as river.
    float RiverShape(Vector3 w)
    {
        float n = Fbm(w * riverScale + OffRiver, 2, 0.5f);
        float t = 1f - Mathf.Clamp01(Mathf.Abs(n) / riverWidth);
        return t * t * (3f - 2f * t);
    }

    // Terrain "corridor" around the paths (wider than the painted path so slopes stay gentle)
    float Corridor(Vector3 d)
    {
        if (!enablePaths) return 0f;
        return PathMask(PathDistance(d), pathHalfWidth, pathShoulder);
    }

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    // 0..1 river strength (colors, water and scatter rules). No rivers on mountains or paths.
    // (NOTE: this comment is for GetRiver below, not for GetRiverCarve. It
    // looks like GetRiverCarve was added in between and the comment stayed here)

    // How much the river digs into the ground here, in world units (0 = no river).
    // Same formula as step 3 of GetHeight, so the water mesh matches the channel exactly.
    public float GetRiverCarve(Vector3 d)
    {
        EnsureReady();
        Vector3 w = Warp(d);
        float mm = MountainMask(w);
        float corridor = Corridor(d);
        return RiverShape(w) * (1f - mm) * (1f - corridor) * riverDepth;
    }

    public float GetRiver(Vector3 d)
    {
        EnsureReady();
        Vector3 w = Warp(d);
        return RiverShape(w) * (1f - MountainMask(w)) * (1f - Corridor(d));
    }

    // 0..1: how much this spot is path (used by CubeSphereGenerator for the dirt color).
    // Includes the portal platform, so the paths visibly connect to it.
    public float GetPath(Vector3 d)
    {
        EnsureReady();
        float m = enablePaths ? PathMask(PathDistance(d), pathHalfWidth, pathEdgeBlend) : 0f;

        if (enablePortalZone)
        {
            float zoneDeg = portalZoneRadius * 180f;
            float ang = Vector3.Angle(d, portalDir);
            m = Mathf.Max(m, 1f - Smooth(zoneDeg * 0.85f, zoneDeg, ang));
        }
        return m;
    }

    // Ground height (world units) in a direction from the planet centre
    // This is THE main function. d must be a direction with length 1.
    // The real position of the ground = centre + d * (radius + GetHeight(d)).
    // Can be negative (under the radius) in the valleys and rivers.
    public float GetHeight(Vector3 d)
    {
        EnsureReady();
        Vector3 w = Warp(d);
        float corridor = Corridor(d);

        // 1) Gentle hills and valleys
        float h = Fbm(w * noiseScale + OffHills, octaves, persistence) * NoiseGain * height;

        // 2) Mountains (fade out inside the path corridor)
        float mm = MountainMask(w);
        float mountainMask = mm * (1f - corridor);
        if (mountainMask > 0f)
        {
            float ridged = RidgedFbm(w * mountainScale + OffMountains, mountainOctaves, 0.5f);
            float shape = Smooth(ridgeFloor, 1f, ridged);
            shape = Mathf.Pow(shape, mountainSharpness);
            h += shape * mountainMask * mountainHeight;
        }

        // 3) Rivers (not carved under paths, so paths never end in a ditch)
        h -= RiverShape(w) * (1f - mm) * (1f - corridor) * riverDepth;

        // 4) Calm the hills along the paths so they are easy to walk
        h *= Mathf.Lerp(1f, pathFlatten, corridor);

        // 5) Flat platform for the portal
        // We multiply h by 0 near the portal, so the ground there is exactly
        // at "radius" (flat), and by 1 far from it (no change).
        if (enablePortalZone)
        {
            float ang = Vector3.Angle(d, portalDir) / 180f;
            h *= Smooth(portalZoneRadius, portalZoneRadius + portalTransitionBlend, ang);
        }

        return h;
    }

    // Distance in degrees from direction d to the EDGE of the nearest path or the portal platform.
    // Negative = inside the path/platform. Used by PlanetScatter to keep objects off the paths.
    public float GetPathClearanceDegrees(Vector3 d)
    {
        EnsureReady();
        float best = float.MaxValue;

        if (enablePaths)
        {
            float dist = PathDistance(d);                       // radians, same wobble as the painted path
            if (dist != float.MaxValue)
                best = dist * Mathf.Rad2Deg - pathHalfWidth;
        }

        if (enablePortalZone)
        {
            float platformEdge = portalZoneRadius * 180f;
            best = Mathf.Min(best, Vector3.Angle(d, portalDir) - platformEdge);
        }
        return best;
    }
}