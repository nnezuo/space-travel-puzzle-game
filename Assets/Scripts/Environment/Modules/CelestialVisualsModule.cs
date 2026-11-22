using UnityEngine;

namespace FlipGameDev.TerrainAndGrassPack.DayNightSystem
{
    /*
     * CelestialVisualsModule
     * ----------------------
     * This module makes the sky look right for the time of the day:
     *  - the sun light fades out at night and the moon light fades in
     *  - the stars appear at night (we change the alpha of their material)
     *  - the moon glow and the clouds color follow a gradient
     *
     * The "ranges" are angles (see DayNightState: 0 = sunrise, 180 = sunset).
     * For example sunHiddenRange = (206, 334) means the sun light is off
     * between 206 and 334 degrees. These numbers are set by hand, so if
     * the sun / moon setup changes they may need to be changed too.
     *
     * Instead of switching on / off at once, each light has a "scale" between
     * 0 and 1 that moves slowly to the target (MoveTowards), so it takes
     * "fadeDuration" seconds to fully fade.
     *
     * Things to know:
     *  - For the stars to really fade, their material needs a shader that
     *    uses the alpha (transparent). With an opaque shader nothing happens.
     *  - We use .material (not .sharedMaterial), so Unity makes a COPY of each
     *    material. That's why we destroy them in Dispose, to not leak memory.
     *  - "_EmissionColor" and "_EMISSION" are names for the Lit shader. With
     *    another shader they may not exist.
     */
    public class CelestialVisualsModule : MonoBehaviour, IDayNightModule
    {
        [Header("References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;
        [SerializeField] private MeshRenderer starsRenderer;
        [SerializeField] private MeshRenderer moonRenderer;
        [SerializeField] private MeshRenderer cloudsRenderer;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 3f;
        
        [SerializeField] private Vector2 sunHiddenRange = new Vector2(206f, 334f);
        [SerializeField] private Vector2 moonHiddenRange = new Vector2(3f, 230f);
        [SerializeField] private Vector2 starsVisibleRange = new Vector2(180f, 355f);
        
        [Header("Clouds Settings")]
        [SerializeField] private Gradient cloudColorGradient;
        [SerializeField] private Gradient moonEmissionGradient;


        private float _sunInitialIntensity;
        private float _moonInitialIntensity;
        
        // 0 = hidden, 1 = fully visible
        private float _sunScale = 1f;
        private float _moonScale = 1f;
        private float _starsScale = 0f;

        private Material _moonMat;
        private Material _starsMat;
        private Material _cloudsMat;
        

        // We save the light intensities set in the Inspector (this is the
        // "max" value) and get a copy of the materials we want to change.
        public void Initialize(DayNightState state)
        {
            if (sunLight) _sunInitialIntensity = sunLight.intensity;
            if (moonLight) _moonInitialIntensity = moonLight.intensity;

            if (moonRenderer)
            {
                _moonMat = moonRenderer.material;
                _moonMat.EnableKeyword("_EMISSION");
            }

            if (starsRenderer) _starsMat = starsRenderer.material;
            if (cloudsRenderer)
            {
                _cloudsMat = cloudsRenderer.material;
                _cloudsMat.EnableKeyword("_EMISSION");
            }
        }

        public void Tick(DayNightState state, float deltaTime)
        {
            float angle = state.CurrentAngle;
            float delta = deltaTime / fadeDuration;

            // 1. What do we want? (0 or 1 for each one)
            float sunTarget = IsAngleInRange(angle, sunHiddenRange) ? 0f : 1f;
            float moonTarget = IsAngleInRange(angle, moonHiddenRange) ? 0f : 1f;
            float starsTarget = IsAngleInRange(angle, starsVisibleRange) ? 1f : 0f;

            // 2. Move slowly toward it
            _sunScale = Mathf.MoveTowards(_sunScale, sunTarget, delta);
            _moonScale = Mathf.MoveTowards(_moonScale, moonTarget, delta);
            _starsScale = Mathf.MoveTowards(_starsScale, starsTarget, delta);

            // 3. Apply it to the lights and materials
            if (sunLight) sunLight.intensity = Mathf.Lerp(0, _sunInitialIntensity, _sunScale);
            if (moonLight) moonLight.intensity = Mathf.Lerp(0, _moonInitialIntensity, _moonScale);

            if (_starsMat)
            {
                Color c = _starsMat.color;
                c.a = _starsScale;
                _starsMat.color = c;
            }

            if (_moonMat)
            {
                Color emissionRGB = moonEmissionGradient.Evaluate(state.NormalizedTime);
                _moonMat.SetColor("_EmissionColor", emissionRGB);
            }

            if (_cloudsMat)
            {
                Color cloudColor = cloudColorGradient.Evaluate(state.NormalizedTime);
                _cloudsMat.SetColor("_EmissionColor", cloudColor);
                _cloudsMat.color = cloudColor;
            }
        }

        // Destroy the material copies made by .material in Initialize
        public void Dispose()
        {
            if (_moonMat != null) Destroy(_moonMat);
            if (_starsMat != null) Destroy(_starsMat);
            if (_cloudsMat != null) Destroy(_cloudsMat);
        }

        /// <summary> Verifies if a circular angle falls within a range X-Y </summary>
        // The second case is when the range goes past 360 and back to 0,
        // for example (300, 40): then the angle must be >= 300 OR <= 40.
        private bool IsAngleInRange(float angle, Vector2 range)
        {
            if (range.x <= range.y) return angle >= range.x && angle <= range.y;
            return angle >= range.x || angle <= range.y;
        }
    }
}