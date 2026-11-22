using UnityEngine;
using UnityEngine.Rendering;

// Put this ONCE in the scene, on the Directional Light.
/*
 * DayNightController
 * ------------------
 * This is the day / night for the PLANETS (the other system, DayNightBrain,
 * is for a normal flat world with a sky that turns).
 *
 * The problem on a round planet: it can be day on one side and night on the
 * other side at the same time. So we can't just turn the light with a timer.
 * Instead, every frame:
 *   1. we find the planet the player is on (with his GravitySource)
 *   2. we ask that planet where its sun is (PlanetDayNight.SunDirection)
 *   3. we compare it with the "up" of the player (dot product):
 *        +1 = the sun is right above the player (noon)
 *         0 = the sun is on the horizon (sunrise / sunset)
 *        -1 = the sun is under the planet (midnight)
 *   4. with this number we choose the light direction, color, intensity,
 *      ambient light and the sky color.
 *
 * There is only ONE real light in the scene. At night we just turn it to
 * the other side and make it blue and weak, so it looks like moonlight.
 *
 * Other scripts read "Daylight" (StarField) and "CurrentPlanet" (CelestialBodies).
 *
 * WARNING: this script and the DayNightBrain modules both change
 * RenderSettings (ambient mode, ambient colors, fog). If both are in the
 * same scene they will fight each other, so only use one of them per scene.
 * Also the camera is set to "Solid Color", so a Skybox material is not shown.
 */
[RequireComponent(typeof(Light))]
public class DayNightController : MonoBehaviour
{
    public PlayerController player;
    [Tooltip("Used when the player is not inside any planet's gravity zone")]
    public PlanetDayNight fallbackPlanet;

    Light sun;
    public float Daylight { get; private set; }   // read by StarField

    public Camera cam;

    public PlanetDayNight CurrentPlanet { get; private set; }   // read by CelestialBodies

    // Setup: the light becomes the "sun" of the scene, and we take control of
    // the ambient light and the camera background ourself.
    void Awake()
    {
        sun = GetComponent<Light>();
        sun.type = LightType.Directional;

        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Flat;   // one flat ambient color, we control it
        RenderSettings.fog = false;                      // we are in space

        if (player == null) player = FindAnyObjectByType<PlayerController>();

        if (cam == null) cam = Camera.main;
        if (cam != null) cam.clearFlags = CameraClearFlags.SolidColor;   // the camera paints a flat color as the sky
    }

    // LateUpdate so the player has already moved this frame.
    // Note: the player is only searched in Awake. If the player is spawned
    // later (or a new scene is loaded), player stays null and nothing happens.
    void LateUpdate()
    {
        if (player == null) return;

        // 1. Which planet is the player on? (GravitySource already knows)
        PlanetDayNight planet = fallbackPlanet;
        if (player.CurrentSource != null)
        {
            var p = player.CurrentSource.GetComponentInParent<PlanetDayNight>();
            if (p != null) planet = p;
        }
        if (planet == null) return;

        CurrentPlanet = planet;

        // 2. How high is the sun above the PLAYER's horizon? +1 = overhead, -1 = directly below
        Vector3 sunDir = planet.SunDirection();
        float daylight = Vector3.Dot(player.transform.up, sunDir);
        Daylight = daylight;
        bool isDay = daylight >= 0f;

        // 3. The light shines FROM the sun by day and from a "moon" on the opposite side by night
        // (a directional light shines along its forward, so we look at -from)
        Vector3 from = isDay ? sunDir : -sunDir;
        transform.rotation = Quaternion.LookRotation(-from);

        // 4. Intensity and color (fade to 0 at the horizon, so the switch is invisible)
        // "amount" is 0 at the horizon and 1 when the sun is a bit higher
        // (the * 4 makes it reach 1 quickly). "dusk" is 1 at the horizon.
        // The 4 and 3 were chosen by hand, just to look nice.
        float amount = Mathf.Clamp01(Mathf.Abs(daylight) * 4f);
        float dusk = 1f - Mathf.Clamp01(Mathf.Abs(daylight) * 3f);

        sun.color = isDay ? Color.Lerp(planet.daySunColor, planet.duskColor, dusk) : planet.nightMoonColor;
        sun.intensity = (isDay ? planet.daySunIntensity : planet.nightMoonIntensity) * amount;

        // 5. Ambient light blends smoothly from night to day around the horizon
        float dayBlend = Mathf.InverseLerp(-0.25f, 0.25f, daylight);
        RenderSettings.ambientLight = Color.Lerp(planet.nightAmbient, planet.dayAmbient, dayBlend);

        // 6. Sky color: night -> day, plus an orange tint around sunrise and sunset
        if (cam != null)
        {
            Color sky = Color.Lerp(planet.nightSky, planet.daySky, dayBlend);
            float duskAmount = 1f - Mathf.Clamp01(Mathf.Abs(daylight) * 4f);   // 1 at the horizon, 0 high or low
            sky = Color.Lerp(sky, planet.duskSky, duskAmount * 0.85f);
            cam.backgroundColor = sky;
        }
    }
}