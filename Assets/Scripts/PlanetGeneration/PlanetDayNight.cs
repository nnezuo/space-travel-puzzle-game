using UnityEngine;

// Put this on EACH planet (on the same object as its GravitySource).
// It only holds that planet's clock and look; DayNightController does the lighting.
public class PlanetDayNight : MonoBehaviour
{
    [Header("Clock")]
    [Range(0f, 1f)] public float timeOfDay = 0.35f;   // 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset
    [Tooltip("Minutes for one full day. 0 = time stays frozen")]
    public float cycleMinutes = 6f;

    [Header("Sun path")]
    [Tooltip("Tilts the sun's path so it does not always cross the same spot (degrees)")]
    public Vector3 sunPathTilt = new Vector3(0f, 30f, 20f);

    [Header("Sky")]
    public bool hasMoon = true;     // turn off for planets without a moon

    [Header("Day look")]
    public Color daySunColor = new Color(1f, 0.96f, 0.84f);
    public float daySunIntensity = 1.2f;
    public Color dayAmbient = new Color(0.55f, 0.62f, 0.72f);
    public Color duskColor = new Color(1f, 0.55f, 0.3f);      // sunrise / sunset tint

    [Header("Night look")]
    public Color nightMoonColor = new Color(0.45f, 0.55f, 0.9f);
    public float nightMoonIntensity = 0.25f;                  // keep above 0 so the puzzle stays playable
    public Color nightAmbient = new Color(0.08f, 0.10f, 0.20f);

    [Header("Sky color (camera background)")]
    public Color daySky = new Color(0.35f, 0.62f, 0.95f);   // blue
    public Color duskSky = new Color(0.95f, 0.50f, 0.30f);   // orange at sunrise / sunset
    public Color nightSky = new Color(0.02f, 0.03f, 0.08f);   // almost black, slightly blue
    void Update()
    {
        if (cycleMinutes <= 0f) return;   // frozen
        timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / (cycleMinutes * 60f), 1f);
    }

    // Direction from the planet's centre TOWARD the sun (world space)
    public Vector3 SunDirection()
    {
        float a = timeOfDay * Mathf.PI * 2f;
        // midnight = straight down, sunrise = +X, noon = straight up, sunset = -X
        Vector3 local = new Vector3(Mathf.Sin(a), -Mathf.Cos(a), 0f);
        return transform.rotation * Quaternion.Euler(sunPathTilt) * local;
    }
}