using UnityEngine;

// A sphere of static stars around the camera, visible only at night.
// Stars are far away, so they follow the camera position (but not its rotation).
/*
 * StarField
 * ---------
 * We use a Particle System, but not in the normal way: we don't let it
 * spawn particles by itself. In Start we create every star ourself with
 * Emit(), at a random point on a big sphere, and they live "forever".
 * This is faster than making 600 small GameObjects.
 *
 * Every frame:
 *  - the star sphere moves with the camera (so we can never get closer
 *    to the stars, they look infinitely far)
 *  - the alpha of the material follows DayNightController.Daylight, so the
 *    stars appear when the sun goes down and disappear in the day.
 *
 * We use System.Random with a seed, so the stars are always at the same
 * place every time we play.
 *
 * Things to know:
 *  - The star material must be transparent (URP Particles/Unlit), or the
 *    alpha change does nothing.
 *  - startLifetime = 1000000 seconds is about 11 days, so "forever" for a
 *    game session, but not really forever.
 */
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

        // Setup of the particle system by code, so we don't have to do it
        // by hand in the Inspector
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
            // We take a random point in a cube and keep it only if it is
            // inside the ball (otherwise the corners of the cube would have
            // more stars). Then normalize puts it on the sphere surface.
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
        // Daylight <= -0.25 -> alpha 1 (full stars), Daylight >= 0.1 -> alpha 0
        float alpha = 1f - Mathf.InverseLerp(-0.25f, 0.1f, controller.Daylight);
        Color c = mat.color; c.a = alpha; mat.color = c;
    }
}