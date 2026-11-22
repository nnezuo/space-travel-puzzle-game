using System.Collections.Generic;
using UnityEngine;

// Builds a thin water mesh that only exists inside the river channels.
// Put it on the planet, next to CubeSphereGenerator and PlanetNoise.
/*
 * How it works:
 *  - We make a second cube sphere (like CubeSphereGenerator) but this one
 *    is the WATER surface.
 *  - For each point, the water height = the ground height as if there was
 *    no river (ground + carve), a bit lower (threshold). So the water is
 *    flat-ish inside the river bed.
 *  - A point is "wet" if the river is deeper than the water level there.
 *    We only keep the triangles that touch a wet point, so the water mesh
 *    only exists inside the rivers and the rest is not drawn.
 *  - The water has no collider, so the player walks in the river (on the
 *    river bed) and doesn't walk on the water.
 *  - In Update we move the texture offset to make the water look like it
 *    is flowing. The ripple texture is made by code with sine waves.
 *
 * Things to improve:
 *  - GetCubePoint is copy-pasted from CubeSphereGenerator. It should be in
 *    one place, so if we change one we don't forget the other.
 *  - The arrays keep ALL the vertices of the grid, even the dry ones that
 *    are not used by any triangle. It works but it wastes memory.
 *  - The water material and the ripple texture are created with "new" every
 *    time we generate, and the old ones are never destroyed (memory leak if
 *    we click "Generate Rivers" a lot in the editor).
 *  - "offset" grows forever. After a very long time float precision could
 *    make the texture move in steps. We could use Mathf.Repeat(offset, 1).
 *  - The water flows in the same direction everywhere (it doesn't follow
 *    the river), and on each cube face the UVs are in a different direction.
 */
public class RiverWater : MonoBehaviour
{
    public Material waterMaterial;     // URP Lit, Surface Type = Transparent

    [Header("Shape")]
    [Range(16, 256)] public int resolution = 128;
    [Tooltip("Water level as a fraction of the river depth. Higher = lower water and narrower rivers")]
    [Range(0.05f, 0.9f)] public float fillLevel = 0.35f;
    [Tooltip("Raise or lower the whole water surface (world units)")]
    public float surfaceLift = 0f;

    [Header("Flow animation")]
    public Vector2 flowSpeed = new Vector2(0.04f, 0.02f);
    public float textureTiling = 10f;

    const string ChildName = "Rivers";
    Material mat;
    Vector2 offset;

    void Start() => Generate();

    // Inspector ⋮ menu → "Generate Rivers" builds it in Edit mode
    [ContextMenu("Generate Rivers")]
    public void Generate()
    {
        var gen = GetComponent<CubeSphereGenerator>();
        var noise = GetComponent<PlanetNoise>();
        if (gen == null || noise == null || waterMaterial == null)
        {
            Debug.LogWarning("RiverWater needs CubeSphereGenerator, PlanetNoise and a Water Material.", this);
            return;
        }
        noise.ApplySeed();

        // Remove the previous water, if any
        Transform old = transform.Find(ChildName);
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        int res = resolution;
        int perFace = res * res;
        var vertices = new Vector3[perFace * 6];
        var normals = new Vector3[perFace * 6];
        var uvs = new Vector2[perFace * 6];
        var wet = new bool[perFace * 6];
        var triangles = new List<int>();

        // threshold = how much lower than the "normal ground" the water is.
        // Example: riverDepth 0.8 and fillLevel 0.35 -> water 0.28 under the
        // banks. Only river parts deeper than 0.28 get water.
        float threshold = noise.riverDepth * fillLevel;
        int v = 0;

        for (int face = 0; face < 6; face++)
        {
            int start = face * perFace;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / (res - 1);
                    float t = (float)y / (res - 1);
                    Vector3 dir = GetCubePoint(face, u, t).normalized;

                    float ground = noise.GetHeight(dir);
                    float carve = noise.GetRiverCarve(dir);

                    // Water surface = the ground as it would be WITHOUT the river, lowered a bit
                    float surface = ground + carve - threshold + surfaceLift;

                    vertices[v] = dir * (gen.radius + surface);
                    normals[v] = dir;                       // calm, smooth lighting
                    uvs[v] = new Vector2(u, t) * textureTiling;

                    // Wet = the channel is deeper than the water level (and not on a path / portal platform)
                    wet[v] = carve > threshold && noise.GetPath(dir) < 0.05f;
                    v++;
                }
            }

            // Only keep grid squares that touch at least one wet vertex
            for (int y = 0; y < res - 1; y++)
            {
                for (int x = 0; x < res - 1; x++)
                {
                    int a = start + y * res + x;
                    int b = a + 1;
                    int c = a + res;
                    int d = c + 1;
                    if (!(wet[a] || wet[b] || wet[c] || wet[d])) continue;

                    // Pick the winding order so triangles face outward
                    Vector3 n = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                    if (Vector3.Dot(n, normals[a]) > 0f)
                    {
                        triangles.Add(a); triangles.Add(b); triangles.Add(c);
                        triangles.Add(b); triangles.Add(d); triangles.Add(c);
                    }
                    else
                    {
                        triangles.Add(a); triangles.Add(c); triangles.Add(b);
                        triangles.Add(b); triangles.Add(c); triangles.Add(d);
                    }
                }
            }
        }

        var mesh = new Mesh { name = "River Water" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();

        // Child object with its own renderer; no collider, so the player can wade through
        var go = new GameObject(ChildName);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // A copy of the material, so the flow animation doesn't change the
        // original material asset in the project.
        mat = new Material(waterMaterial);
        mat.SetTexture("_BaseMap", BuildRippleTexture(128));   // URP texture slot
        mr.sharedMaterial = mat;
    }

    void Update()
    {
        if (mat == null) return;
        offset += flowSpeed * Time.deltaTime;
        mat.SetTextureOffset("_BaseMap", offset);
    }

    // Tileable ripple pattern drawn with sine waves (whole-number frequencies tile perfectly)
    // We add 3 sine waves in different directions to get a "water" pattern.
    // Because the numbers (3, 2, 5...) are whole numbers, the left side of
    // the texture matches the right side, so we don't see where it repeats.
    static Texture2D BuildRippleTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        const float TwoPi = Mathf.PI * 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, w = (float)y / size;
                float s = Mathf.Sin(TwoPi * (3f * u + 2f * w))
                        + Mathf.Sin(TwoPi * (2f * u - 3f * w))
                        + Mathf.Sin(TwoPi * (5f * u + w));
                s = s / 3f * 0.5f + 0.5f;                        // 0..1
                float shade = Mathf.Lerp(0.75f, 1f, s);          // bright glints
                tex.SetPixel(x, y, new Color(shade, shade, shade, Mathf.Lerp(0.65f, 1f, s)));
            }
        }
        tex.Apply();
        return tex;
    }

    // Same cube-to-face mapping as CubeSphereGenerator
    static Vector3 GetCubePoint(int face, float u, float v)
    {
        float x = u * 2f - 1f;
        float y = v * 2f - 1f;
        switch (face)
        {
            case 0: return new Vector3(1, y, x);
            case 1: return new Vector3(-1, y, -x);
            case 2: return new Vector3(x, 1, y);
            case 3: return new Vector3(x, -1, -y);
            case 4: return new Vector3(x, y, 1);
            case 5: return new Vector3(-x, y, -1);
            default: return Vector3.zero;
        }
    }
}