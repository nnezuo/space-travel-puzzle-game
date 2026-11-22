using UnityEngine;

/*
 * CubeSphereGenerator
 * -------------------
 * This script makes the planet mesh by code. The idea is called a
 * "cube sphere":
 *   1. we make a cube with 6 faces, each face is a grid of
 *      resolution x resolution points
 *   2. we normalize every point (length = 1), so the cube "inflates" and
 *      becomes a sphere
 *   3. we push every point out by (radius + height of the terrain), the
 *      height comes from PlanetNoise (mountains, rivers, paths...)
 *   4. we color every vertex with PlanetBiomeColor (no texture)
 *   5. we give the mesh to the MeshFilter and to a MeshCollider, so the
 *      player can walk on it
 *
 * A cube sphere is better than the default Unity sphere (UV sphere) for
 * this, because the UV sphere has a lot of tiny triangles at the poles,
 * and the cube sphere is more even.
 *
 * The same seed always gives the same planet, so what we see in the Editor
 * (with the "Generate Planet" menu) is the same as in the game.
 *
 * Known problems / things to improve:
 *  - Each face of the cube has its OWN vertices on its borders, so the
 *    vertices on the edges are duplicated. RecalculateNormals doesn't know
 *    they are the same point, so we can sometimes see thin lines (seams)
 *    in the light along the 12 edges of the cube.
 *  - Just normalizing the cube points gives more vertices near the corners
 *    of the cube than in the middle of the faces. There is a better formula
 *    to spread them evenly, we didn't have time to try it.
 *  - With resolution 256 the mesh is very big (6 x 256 x 256 = 393 216
 *    vertices) and the MeshCollider is slow to build. 100-128 is enough.
 *  - The planet is generated again in Start every time we press Play, even
 *    if it was already generated in the editor.
 *  - The mesh name "Planet 1 Cube Sphere" is written in the code, so it is
 *    the same name for every planet that uses this script.
 */
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphereGenerator : MonoBehaviour
{
    public float radius = 10f;
    [Range(2, 256)] public int resolution = 100;   // use 100-128 for smooth terrain

    public Material planetMaterial;
    public PlanetNoise planetNoise;
    public PlanetBiomeColor biomeColor;

    [Header("Editor")]
    [Tooltip("Regenerate automatically when you change a value. Turn off if it feels slow.")]
    public bool autoUpdate = false;

    // Lowest/highest terrain height found; PlanetScatter uses them to match the biome colors
    [HideInInspector] public float minHeight;
    [HideInInspector] public float maxHeight;

    const string MeshName = "Planet 1 Cube Sphere";

    void Start()
    {
        GeneratePlanet();   // same seed = same planet, so it matches what you saw in the Editor
    }

    // Called by PlanetNoise / PlanetBiomeColor when a value changes in the Inspector
    // We can't build a mesh directly inside OnValidate (Unity doesn't like
    // it and shows warnings), so we use delayCall: it runs the code a bit
    // later, when the editor is ready. "this == null" checks if the object
    // was deleted in the meantime.
    public void EditorRefresh()
    {
#if UNITY_EDITOR
        if (!autoUpdate) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying) return;
            GeneratePlanet();
        };
#endif
    }

#if UNITY_EDITOR
    void OnValidate() => EditorRefresh();
#endif

    // Inspector ⋮ menu → "Generate Planet" builds it in Edit mode
    [ContextMenu("Generate Planet")]
    public void GeneratePlanet()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (resolution < 2)
        {
            Debug.LogError("Planet resolution must be at least 2.", this);
            return;
        }

        if (planetNoise != null) planetNoise.ApplySeed();

        // Delete the previous mesh so regenerating does not leak memory
        if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == MeshName)
        {
            if (Application.isPlaying) Destroy(meshFilter.sharedMesh);
            else DestroyImmediate(meshFilter.sharedMesh);
        }

        /*
         * We prepare all the arrays. We save the direction, the height and
         * the river value of each vertex, because we need them again later
         * for the colors (after the normals are computed).
         * Number of triangles: each face has (resolution-1)^2 squares,
         * each square = 2 triangles = 6 indices, and there are 6 faces.
         */
        int verticesPerFace = resolution * resolution;
        int totalVertices = verticesPerFace * 6;

        Vector3[] vertices = new Vector3[totalVertices];
        Vector3[] directions = new Vector3[totalVertices];
        float[] heights = new float[totalVertices];
        float[] rivers = new float[totalVertices];
        Color[] colors = new Color[totalVertices];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6 * 6];

        float minH = float.MaxValue;
        float maxH = float.MinValue;

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int face = 0; face < 6; face++)
        {
            int start = face * verticesPerFace;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (float)x / (resolution - 1);
                    float v = (float)y / (resolution - 1);

                    // Cube point -> sphere direction (length 1)
                    Vector3 direction = GetCubePoint(face, u, v).normalized;

                    float terrainHeight = 0f;
                    if (planetNoise != null)
                        terrainHeight = planetNoise.GetHeight(direction);

                    directions[vertexIndex] = direction;
                    heights[vertexIndex] = terrainHeight;
                    rivers[vertexIndex] = planetNoise != null ? planetNoise.GetRiver(direction) : 0f;
                    vertices[vertexIndex] = direction * (radius + terrainHeight);

                    if (terrainHeight < minH) minH = terrainHeight;
                    if (terrainHeight > maxH) maxH = terrainHeight;

                    vertexIndex++;
                }
            }

            // Make the triangles of this face. For each small square of the
            // grid we have 4 corners:
            //    c --- d
            //    |     |
            //    a --- b
            // and we make 2 triangles with them.
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int a = start + y * resolution + x;
                    int b = a + 1;
                    int c = a + resolution;
                    int d = c + 1;

                    // Pick the winding order so triangles face outward
                    // (Unity only draws the front side of a triangle. The
                    // front depends on the order of the 3 points. Because
                    // the 6 faces of the cube are not all oriented the same
                    // way, we test with a cross product and flip if needed,
                    // otherwise some faces would be invisible.)
                    Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                    Vector3 outward = vertices[a].normalized;

                    if (Vector3.Dot(normal, outward) > 0f)
                    {
                        triangles[triangleIndex++] = a; triangles[triangleIndex++] = b; triangles[triangleIndex++] = c;
                        triangles[triangleIndex++] = b; triangles[triangleIndex++] = d; triangles[triangleIndex++] = c;
                    }
                    else
                    {
                        triangles[triangleIndex++] = a; triangles[triangleIndex++] = c; triangles[triangleIndex++] = b;
                        triangles[triangleIndex++] = b; triangles[triangleIndex++] = c; triangles[triangleIndex++] = d;
                    }
                }
            }
        }

        minHeight = minH;
        maxHeight = maxH;

        // UInt32 is needed because a normal mesh can only have 65 535
        // vertices (UInt16), and our planet has a lot more.
        Mesh mesh = new Mesh();
        mesh.name = MeshName;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();          // normals first: we need them to measure the slope
        Vector3[] normals = mesh.normals;

        // Vertex colors from height, latitude, rivers, slope, and paths
        for (int i = 0; i < totalVertices; i++)
        {
            if (biomeColor == null) { colors[i] = Color.white; continue; }

            // normalizedHeight: 0 = lowest point of the planet, 1 = highest
            // latitude: 0 at the equator, 1 at the poles (we use the world Y)
            // slope: 0 if the ground is flat (normal = direction from the
            //        centre), bigger when the ground is steep
            // Note: GetPath is called a second time here for every vertex,
            // we could have saved it in the first loop like the rivers.
            float normalizedHeight = Mathf.InverseLerp(minH, maxH, heights[i]);
            float latitude = Mathf.Abs(directions[i].y);
            float slope = 1f - Mathf.Clamp01(Vector3.Dot(normals[i], directions[i]));
            float path = planetNoise != null ? planetNoise.GetPath(directions[i]) : 0f; // Added path sampling

            colors[i] = biomeColor.GetBiomeColor(normalizedHeight, latitude, rivers[i], slope, directions[i], path);
        }

        mesh.colors = colors;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        // Collider: refresh it so the player and crystal spawner use the new shape
        MeshCollider col = GetComponent<MeshCollider>();
        if (col == null) col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = null;   // set to null first to force Unity to rebuild the collider
        col.sharedMesh = mesh;

        if (planetMaterial != null)
            meshRenderer.sharedMaterial = planetMaterial;
    }

    // Gives a point on one face of a cube that goes from -1 to 1.
    // u and v (0..1) are the position on the face, we change them to -1..1.
    // Each "case" is one face: +X, -X, +Y, -Y, +Z, -Z.
    Vector3 GetCubePoint(int face, float u, float v)
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