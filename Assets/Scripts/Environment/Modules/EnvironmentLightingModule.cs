using UnityEngine;
using UnityEngine.Rendering;

/*
Customize the "Ambient Mode - Trilight (Gradient)" according to the time of day:"
- 0% (Left side): Sunrise (Angle 0).
- 25%: Midday (Angle 90).
- 50%: Sunset (Angle 180).
- 75%: Midnight (Angle 270).
- 100% (Right side): Sunrise (Angle 360/0) - Make sure this color matches 0% exactly for a seamless loop.
*/
namespace FlipGameDev.TerrainAndGrassPack.DayNightSystem
{
    /*
     * EnvironmentLightingModule
     * -------------------------
     * This module changes the general light of the scene with the time:
     *  - the color of the sun (orange at sunrise / sunset, white at midday)
     *  - the ambient light (the light that comes from "everywhere"), with
     *    3 colors: sky (top), equator (sides) and ground (bottom)
     *  - the fog color
     * All the colors come from Gradients in the Inspector (see the note above
     * to know which part of the gradient is which time of the day).
     *
     * Things to know:
     *  - This script changes RenderSettings, so it OVERRIDES what is set in
     *    the Lighting window of Unity. If you change the ambient there, it
     *    will be replaced when you press Play.
     *  - We set fog = false, so the fog distances and the fog color are
     *    useless right now. If we want fog, we must change it to true here
     *    (changing it in the Lighting window is not enough).
     *  - If ambientMode is not Trilight, only the sun color changes.
     */
    public class EnvironmentLightingModule : MonoBehaviour, IDayNightModule
    {
        [Header("Lighting Settings")]
        [SerializeField] private AmbientMode ambientMode = AmbientMode.Trilight;
        
        [Header("Colors (Driven by time 0 to 1)")]
        [SerializeField] private Gradient skyColor;
        [SerializeField] private Gradient equatorColor;
        [SerializeField] private Gradient groundColor;
        
        [Header("Sun Setup")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Gradient sunColor;
        [SerializeField] private Gradient fogColor;

        private bool _isGradientMode;

        // Setup of the scene lighting, done one time at the start
        public void Initialize(DayNightState state)
        {
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.fog = false;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 300f;
            _isGradientMode = ambientMode == AmbientMode.Trilight;
        }

        // Every frame we read the gradients at the current time (0 to 1)
        public void Tick(DayNightState state, float deltaTime)
        {
            float timeFraction = state.NormalizedTime;

            if (sunLight != null) 
                sunLight.color = sunColor.Evaluate(timeFraction);

            if (_isGradientMode)
            {
                RenderSettings.ambientSkyColor = skyColor.Evaluate(timeFraction);
                RenderSettings.ambientEquatorColor = equatorColor.Evaluate(timeFraction);
                RenderSettings.ambientGroundColor = groundColor.Evaluate(timeFraction);
                RenderSettings.fogColor = fogColor.Evaluate(timeFraction);
            }
        }

        public void Dispose() { }
    }
}