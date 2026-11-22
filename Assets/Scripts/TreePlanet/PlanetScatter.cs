using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * PlanetScatter
 * -------------
 * Puts trees, rocks, grass... on the planet automatically.
 * Put it on the planet, next to CubeSphereGenerator and PlanetNoise.
 *
 * Each "Layer" is one type of object (for example "Trees") with its own
 * rules: at what height it can grow, max slope, far from rivers, far from
 * the paths, minimum space between two objects...
 *
 * How it works (for each layer):
 *   1. take a random direction on the sphere
 *   2. ask PlanetNoise the height, the river, the path distance there
 *   3. if one rule is not OK, we "reject" this spot and try another one
 *   4. if everything is OK, we spawn a random prefab of the layer there,
 *      with a random rotation and size
 * We try at most count * 30 times, so if the rules are too strict we get
 * less objects than "count". The Debug.Log at the end says why the spots
 * were rejected, this is very useful to tune the values.
 *
 * IMPORTANT: this is NOT done when the game starts. We do it in the editor
 * with the ⋮ menu -> "Scatter All", and the objects are saved in the scene.
 * So if we change the seed or the terrain, we must click "Scatter All"
 * again, or the trees will float or be inside the ground.
 *
 * Every random thing uses System.Random(seed), so the same seed always
 * gives the same forest.
 *
 * Things to improve:
 *  - The spacing check looks at ALL the objects already placed, for every
 *    new one. With a lot of objects (grass) it gets very slow. That's why
 *    minSeparation = 0 skips it. A grid would be faster.
 *  - SurfaceNormal calls GetHeight 3 more times for each spot (slow).
 *  - Small bug in the Debug.Log: "path:" is printed at the end, glued to
 *    the average height, without a comma or a space.
 */
public class PlanetScatter : MonoBehaviour
{
    // One type of object to scatter, with its rules. [Serializable] makes it
    // show in the Inspector as a list we can open.
    [Serializable]
    public class Layer
    {
        public string name = "Trees";
        public GameObject[] prefabs;
        public int count = 200;
        public int seed = 1;

        [Header("Where it can grow")]
        [Range(0f, 1f)] public float minHeight = 0.35f;  // same 0..1 scale as the biome colors
        [Range(0f, 1f)] public float maxHeight = 0.60f;
        [Range(0f, 90f)] public float maxSlope = 25f;    // degrees
        [Range(0f, 1f)] public float maxRiver = 0.05f;   // avoid rivers

        [Tooltip("Stay this many degrees away from paths and the portal platform (1 degree = about 0.5 units)")]
        public float pathClearance = 4f;
        public float minSeparation = 2.5f;               // 0 = no spacing check (fast, for grass)

        [Header("Look")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
        [Tooltip("ON = tilt with the ground (grass, rocks). OFF = straight up from the planet centre (trees)")]
        public bool alignToSurface = false;
        public float sink = 0.05f;                       // pushes the object slightly into the ground
    }

    public Layer[] layers;

    // Every container we create starts with this name, so ClearAll knows
    // which children it can delete (and doesn't delete other children).
    const string Prefix = "Scatter_";

    // Inspector ⋮ menu → "Scatter All"
    // We need the planet to be generated first, because we use minHeight and
    // maxHeight from CubeSphereGenerator to have the same 0..1 height scale
    // as the biome colors (so the trees are on the green parts).
    [ContextMenu("Scatter All")]
    public void ScatterAll()
    {
        var gen = GetComponent<CubeSphereGenerator>();
        var noise = GetComponent<PlanetNoise>();
        if (gen == null || noise == null || layers == null) return;

        if (gen.maxHeight <= gen.minHeight)
        {
            Debug.LogWarning("Generate the planet first (CubeSphereGenerator ⋮ → Generate Planet).", this);
            return;
        }

        noise.ApplySeed();
        ClearAll();

        foreach (var layer in layers)
            ScatterLayer(layer, gen, noise);

        MarkDirty();
    }

    // Inspector ⋮ menu → "Clear All"
    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        // Go backwards because we delete children while looping
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(Prefix)) continue;
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        MarkDirty();
    }

    void ScatterLayer(Layer layer, CubeSphereGenerator gen, PlanetNoise noise)
    {
        if (layer.prefabs == null || layer.prefabs.Length == 0) return;

        Transform container = new GameObject(Prefix + layer.name).transform;
        container.SetParent(transform, false);

        var rng = new System.Random(layer.seed);
        var placed = new List<Vector3>();
        int attempts = layer.count * 30;

        // Diagnostics: why were spots rejected?
        int rejHeight = 0, rejRiver = 0, rejSlope = 0, rejSpacing = 0, rejPath = 0;
        float heightSum = 0f;
        int samples = 0;

        for (int i = 0; i < attempts && placed.Count < layer.count; i++)
        {
            Vector3 dir = RandomDirection(rng);

            float h = noise.GetHeight(dir);
            float normalized = Mathf.InverseLerp(gen.minHeight, gen.maxHeight, h);
            heightSum += normalized;
            samples++;

            if (normalized < layer.minHeight || normalized > layer.maxHeight) { rejHeight++; continue; }
            if (noise.GetRiver(dir) > layer.maxRiver) { rejRiver++; continue; }
            if (noise.GetPathClearanceDegrees(dir) < layer.pathClearance) { rejPath++; continue; }

            Vector3 normal = SurfaceNormal(dir, gen.radius, noise);
            if (Vector3.Angle(normal, dir) > layer.maxSlope) { rejSlope++; continue; }

            Vector3 pos = dir * (gen.radius + h);

            if (layer.minSeparation > 0f)
            {
                bool tooClose = false;
                float minSqr = layer.minSeparation * layer.minSeparation;
                foreach (var p in placed)
                    if ((p - pos).sqrMagnitude < minSqr) { tooClose = true; break; }
                if (tooClose) { rejSpacing++; continue; }
            }
            placed.Add(pos);

            GameObject prefab = layer.prefabs[rng.Next(layer.prefabs.Length)];
            GameObject obj = Spawn(prefab, container);

            // Rotation: first turn the object so its up = "up", then a random
            // turn around its own up so all the trees don't look the same way.
            // We use localPosition because pos is relative to the planet centre.
            Vector3 up = layer.alignToSurface ? normal : dir;
            obj.transform.localPosition = pos - up * layer.sink;
            obj.transform.localRotation =
                Quaternion.FromToRotation(Vector3.up, up) *
                Quaternion.AngleAxis((float)rng.NextDouble() * 360f, Vector3.up);
            obj.transform.localScale = Vector3.one *
                Mathf.Lerp(layer.scaleRange.x, layer.scaleRange.y, (float)rng.NextDouble());
        }

        // Report in the Console. If almost everything is rejected by "height",
        // compare the min/max height of the layer with the average height.
        // (the "path" count is at the end without a separator, see the top)
        float avg = samples > 0 ? heightSum / samples : 0f;
        Debug.Log($"PlanetScatter: {layer.name} placed {placed.Count}/{layer.count}. " +
                  $"Rejected -> height: {rejHeight}, river: {rejRiver}, slope: {rejSlope}, spacing: {rejSpacing}. " +
                  $"Average ground height on the 0-1 scale: {avg:F2}" +
                  $"path: {rejPath}", this);
    }

    // Uniform random direction on a sphere (deterministic)
    // Random point in a cube, keep it only if it is inside the ball, then
    // normalize. If we didn't throw away the points outside the ball, there
    // would be more objects in the direction of the cube corners.
    // (We don't use Random.onUnitSphere because it doesn't use our seed.)
    static Vector3 RandomDirection(System.Random rng)
    {
        while (true)
        {
            var v = new Vector3((float)(rng.NextDouble() * 2 - 1),
                                (float)(rng.NextDouble() * 2 - 1),
                                (float)(rng.NextDouble() * 2 - 1));
            float m = v.sqrMagnitude;
            if (m > 0.0001f && m <= 1f) return v / Mathf.Sqrt(m);
        }
    }

    // Ground normal from three nearby terrain samples
    // The mesh doesn't exist yet as a list we can read easily, so we compute
    // the normal ourself: we take 3 ground points very close to each other
    // (p0, p1, p2), make a small triangle, and the cross product of its 2
    // sides is the normal. If it points inside the planet we flip it.
    static Vector3 SurfaceNormal(Vector3 dir, float radius, PlanetNoise noise)
    {
        Vector3 helper = Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right;
        Vector3 t = Vector3.Cross(dir, helper).normalized;
        Vector3 b = Vector3.Cross(dir, t);
        const float e = 0.01f;

        Vector3 p0 = dir * (radius + noise.GetHeight(dir));
        Vector3 d1 = (dir + t * e).normalized;
        Vector3 d2 = (dir + b * e).normalized;
        Vector3 p1 = d1 * (radius + noise.GetHeight(d1));
        Vector3 p2 = d2 * (radius + noise.GetHeight(d2));

        Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
        return Vector3.Dot(n, dir) < 0f ? -n : n;
    }

    GameObject Spawn(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)   // keeps the prefab link in Edit mode
            return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
        return Instantiate(prefab, parent);
    }

    // Tells Unity the scene changed (the * in the title), otherwise it doesn't
    // ask to save and we lose the scattered objects when we close the editor.
    void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}