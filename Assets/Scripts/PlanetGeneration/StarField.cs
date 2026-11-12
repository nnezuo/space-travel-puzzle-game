using UnityEngine;

// A sphere of static stars around the camera, visible only at night.
// Stars are far away, so they follow the camera position (but not its rotation).
[RequireComponent(typeof(ParticleSystem))]
public class StarField : MonoBehaviour
{
    public DayNightController controller;
    public Camera cam;
    [Tooltip("A transparent URP Particles/Unlit material (white)")]
    public Material starMaterial;

    [Header("Stars")]
    public int starCount = 600;
    [Tooltip("Must be smaller than the camera's Far Clipping Plane (and bigger than the sun/moon distance)")]
    public float distance = 700f;
    public Vector2 sizeRange = new Vector2(1.5f, 4f);
    public int seed = 7;

    Material mat;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        var ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = starCount;

        var emission = ps.emission; emission.enabled = false;   // we place every star by hand
        var shape = ps.shape; shape.enabled = false;

        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (starMaterial != null) renderer.material = starMaterial;
        mat = renderer.material;                                // own copy, safe to change

        ps.Play();

        var rng = new System.Random(seed);
        var p = new ParticleSystem.EmitParams();
        for (int i = 0; i < starCount; i++)
        {
            // Random point on a sphere (same trick as the planet scatter)
            Vector3 dir;
            do
            {
                dir = new Vector3((float)(rng.NextDouble() * 2 - 1),
                                  (float)(rng.NextDouble() * 2 - 1),
                                  (float)(rng.NextDouble() * 2 - 1));
            } while (dir.sqrMagnitude > 1f || dir.sqrMagnitude < 0.0001f);

            float brightness = Mathf.Lerp(0.5f, 1f, (float)rng.NextDouble());
            p.position = dir.normalized * distance;
            p.startSize = Mathf.Lerp(sizeRange.x, sizeRange.y, (float)rng.NextDouble());
            p.startColor = new Color(brightness, brightness, brightness, 1f);
            p.startLifetime = 1000000f;                         // stars never die
            ps.Emit(p, 1);
        }
    }

    void LateUpdate()
    {
        if (cam == null || controller == null || mat == null) return;

        transform.position = cam.transform.position;           // follows the camera, never rotates

        // Visible when the sun is low or below the horizon (matches the sky color blend)
        float alpha = 1f - Mathf.InverseLerp(-0.25f, 0.1f, controller.Daylight);
        Color c = mat.color; c.a = alpha; mat.color = c;
    }
}