using UnityEngine;
using UnityEngine.Rendering;

// Put this ONCE in the scene, on the Directional Light.
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
        Vector3 from = isDay ? sunDir : -sunDir;
        transform.rotation = Quaternion.LookRotation(-from);

        // 4. Intensity and color (fade to 0 at the horizon, so the switch is invisible)
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