using UnityEngine;

namespace FlipGameDev.TerrainAndGrassPack.DayNightSystem
{
    /*
     * CelestialRotationModule
     * -----------------------
     * This module turns the objects in the sky with the angle of the time.
     *  - "rotator" is a parent object that holds the sun and the moon (the
     *    moon is on the other side). When we turn it on the Z axis, the sun
     *    goes up and down like a real day.
     *  - "starsRotator" turns the stars slowly at its own speed, it doesn't
     *    follow the time of the day.
     *
     * Things to know:
     *  - starsSpeed is 0 by default, so the stars don't move unless we change
     *    it in the Inspector.
     *  - In Initialize we read rotator.eulerAngles (WORLD rotation) but in Tick
     *    we write localRotation (LOCAL rotation). If the parent of the rotator
     *    is rotated, the start angle will be wrong. It's fine when the parent
     *    has no rotation, but it should use localEulerAngles to be correct.
     */
    public class CelestialRotationModule : MonoBehaviour, IDayNightModule
    {
        [Header("References")][SerializeField] private Transform rotator;
        [SerializeField] private Transform starsRotator;[Header("Settings")]
        [SerializeField] private float starsSpeed = 0f;

        private float _starsCurrentAngle;

        // We start the cycle from the rotation the rotator has in the scene,
        // so we can choose the start time just by turning the object in the editor.
        public void Initialize(DayNightState state)
        {
            if (rotator != null) 
                state.CurrentAngle = rotator.eulerAngles.z;
        }

        public void Tick(DayNightState state, float deltaTime)
        {
            // Sun and moon follow the time of the day
            if (rotator != null)
            {
                rotator.localRotation = Quaternion.Euler(0f, 0f, state.CurrentAngle);
            }

            // Stars turn on their own (degrees per second)
            if (starsRotator != null)
            {
                _starsCurrentAngle += starsSpeed * deltaTime;
                starsRotator.localRotation = Quaternion.Euler(0f, 0f, _starsCurrentAngle);
            }
        }

        public void Dispose() { }
    }
}