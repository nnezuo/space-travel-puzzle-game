using UnityEngine;

/*
 * PlanetBiomeColor
 * ----------------
 * This script chooses the COLOR of each vertex of the planet mesh. We don't
 * use textures: CubeSphereGenerator calls GetBiomeColor() for every vertex
 * and saves the color in the mesh (vertex colors). The material must use a
 * shader that shows vertex colors.
 *
 * The color is made in layers, each one painted on top of the last one:
 *   1. height bands: sand (low) -> grass -> forest -> rock -> snow (high)
 *   2. snow on the poles (top and bottom of the planet)
 *   3. rock where the ground is steep (slope)
 *   4. blue on the rivers
 *   5. brown on the paths
 *   6. random light / dark patches so it doesn't look too flat
 *
 * "blend" makes the border between 2 bands soft instead of a hard line.
 *
 * OnValidate (only in the editor) asks CubeSphereGenerator to rebuild the
 * planet every time we change a value in the Inspector, so we can see the
 * result right away. It only works if "autoUpdate" is on in
 * CubeSphereGenerator (because with a high resolution it is slow).
 */
public class PlanetBiomeColor : MonoBehaviour
{
    [Header("Colors")]
    public Color sand = new Color(0.78f, 0.70f, 0.45f);
    public Color grass = new Color(0.30f, 0.55f, 0.22f);
    public Color forest = new Color(0.14f, 0.36f, 0.16f);
    public Color rock = new Color(0.42f, 0.40f, 0.38f);
    public Color pathColor = new Color(0.55f, 0.45f, 0.35f); // Dirt/Path color
    public Color snow = Color.white;
    public Color water = new Color(0.16f, 0.40f, 0.70f);

    [Header("Height bands (0..1 = lowest..highest point of the planet)")]
    [Range(0f, 1f)] public float sandEnd = 0.33f;
    [Range(0f, 1f)] public float grassEnd = 0.50f;
    [Range(0f, 1f)] public float forestEnd = 0.60f;
    [Range(0f, 1f)] public float snowStart = 0.85f;
    [Range(0.01f, 0.2f)] public float blend = 0.05f;   // bigger = softer transitions

    [Header("Slope (steep ground becomes rock)")]
    [Range(0f, 1f)] public float rockSlopeStart = 0.08f;
    [Range(0f, 1f)] public float rockSlopeEnd = 0.25f;

    [Header("Poles")]
    [Range(0f, 1f)] public float snowLatitude = 0.8f;

    [Range(0f, 0.3f)] public float colorVariation = 0.12f;  // random light/dark patches

#if UNITY_EDITOR
    void OnValidate()
    {
        var gen = GetComponent<CubeSphereGenerator>();
        if (gen != null) gen.EditorRefresh();
    }
#endif

    // Smooth 0..1 transition around an edge
    // Returns 0 below (edge - blend), 1 above (edge + blend), and a smooth
    // curve in between.
    float Edge(float edge, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - blend, edge + blend, value));
    }

    // h = height 0..1, latitude = |y| of the direction, river = 0..1,
    // slope = 0 (flat) to ~0.3+ (steep), dir = direction from planet centre
    // path = 0..1, how much this point is on a path (from PlanetNoise)
    public Color GetBiomeColor(float h, float latitude, float river, float slope, Vector3 dir, float path)
    {
        // Height layers
        Color c = sand;
        c = Color.Lerp(c, grass, Edge(sandEnd, h));
        c = Color.Lerp(c, forest, Edge(grassEnd, h));
        c = Color.Lerp(c, rock, Edge(forestEnd, h));
        c = Color.Lerp(c, snow, Edge(snowStart, h));

        // Snowy poles & steep slopes
        c = Color.Lerp(c, snow, Mathf.InverseLerp(snowLatitude, 1f, latitude));
        c = Color.Lerp(c, rock, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rockSlopeStart, rockSlopeEnd, slope)));

        // Rivers
        // (river - 0.4) * 3: no blue under 0.4, full blue from about 0.73
        c = Color.Lerp(c, water, Mathf.Clamp01((river - 0.4f) * 3f));

        // Using the Path Color from the Inspector
        c = Color.Lerp(c, pathColor, Mathf.Clamp01(path * 2f));
        // ------------------------------------------

        // Light/dark patches
        // Mathf.PerlinNoise is only 2D, so we mix x, y, z into 2 numbers.
        // It's not perfect (the patches can look stretched in some places)
        // but nobody notices. k goes from (1 - colorVariation) to (1 + colorVariation).
        float v = Mathf.PerlinNoise(dir.x * 18f + dir.y * 7f + 3f, dir.z * 18f + 11f);
        float k = 1f + (v - 0.5f) * 2f * colorVariation;
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
    }
}