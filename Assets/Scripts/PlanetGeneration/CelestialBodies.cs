using UnityEngine;

// Moves a visual sun and moon across the sky. They do not orbit anything:
// they sit far away from the camera, in the direction the day-night system says.
/*
 * CelestialBodies
 * ---------------
 * The sun and the moon we see are only "decoration". The real light comes
 * from DayNightController. This script puts the sun sphere far away in the
 * direction of the sun of the current planet, and the moon on the opposite
 * side. Because they are placed relative to the CAMERA, they always stay at
 * the same distance, so the player can never reach them (like a real sky).
 *
 * The sun color changes too: orange when it is low (sunrise / sunset) and
 * pale yellow when it is high.
 *
 * Things to know:
 *  - "distance" must be smaller than the Far Clipping Plane of the camera,
 *    otherwise the sun is not drawn.
 *  - controller.player is used without a null check (if there is no player
 *    the sun color line will throw an error).
 *  - sunRenderer.material makes a copy of the material (only the first time),
 *    this copy is never destroyed but it's only one so it's ok.
 */
[DefaultExecutionOrder(100)]   // run after the camera has moved
public class CelestialBodies : MonoBehaviour
{
    public DayNightController controller;
    public Camera cam;
    public Transform sun;
    public Transform moon;
    public Renderer sunRenderer;   // drag the Sun's Mesh Renderer here

    [Header("Placement")]
    [Tooltip("Must be smaller than the camera's Far Clipping Plane")]
    public float distance = 500f;
    public float sunSize = 60f;     // diameter in world units (at 500 distance, about 7 degrees)
    public float moonSize = 25f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (controller == null || cam == null) return;
        PlanetDayNight planet = controller.CurrentPlanet;
        if (planet == null) return;

        Vector3 dir = planet.SunDirection();           // from the planet centre toward the sun

        if (sun != null)
        {
            sun.position = cam.transform.position + dir * distance;
            sun.localScale = Vector3.one * sunSize;
            // Orange near the horizon, pale yellow when high in the sky
            // (height * 2 means it is already fully yellow at half way up)
            if (sunRenderer != null)
            {
                float height = Mathf.Clamp01(Vector3.Dot(controller.player.transform.up, dir));   // 0 = horizon, 1 = overhead
                Color noon = new Color(1f, 0.96f, 0.72f);
                sunRenderer.material.color = Color.Lerp(planet.duskColor, noon, height * 2f);
            }
        }

        // The moon is hidden on planets that don't have one (hasMoon = false)
        if (moon != null)
        {
            moon.gameObject.SetActive(planet.hasMoon);
            moon.position = cam.transform.position - dir * distance;   // opposite side, like the night light
            moon.localScale = Vector3.one * moonSize;
        }
    }
}