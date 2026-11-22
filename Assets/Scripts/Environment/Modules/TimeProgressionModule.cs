using UnityEngine;

namespace FlipGameDev.TerrainAndGrassPack.DayNightSystem
{
    /*
     * TimeProgressionModule
     * ---------------------
     * This module makes the time go forward: it increases the angle of the
     * sun in DayNightState every frame. All the other modules only READ the
     * angle, so this one should be the first module in the hierarchy.
     *
     * The day is longer than the night: 2/3 of the cycle is day and 1/3 is
     * night. To do that, the angle moves slower during the day (0 to 180)
     * and faster during the night (180 to 360).
     * Example with 15 minutes: day = 10 min, night = 5 min.
     *
     * Small problem: if CycleDurationMinutes is 0 in the Inspector, we
     * divide by 0 and the angle becomes "Infinity" / NaN. We should check it.
     */
    public class TimeProgressionModule : MonoBehaviour, IDayNightModule
    {
        // Nothing to prepare here
        public void Initialize(DayNightState state) { }

        public void Tick(DayNightState state, float deltaTime)
        {
            float totalSeconds = state.CycleDurationMinutes * 60f;
            
            // Rule of thirds: 2/3 day, 1/3 night
            float dayDuration = totalSeconds * (2.0f / 3.0f);
            float nightDuration = totalSeconds * (1.0f / 3.0f);

            state.IsDaytime = state.CurrentAngle >= 0f && state.CurrentAngle < 180f;

            // Speed in degrees per second: 180 degrees must be done in the
            // day duration (or in the night duration)
            float speed = state.IsDaytime 
                ? (180f / dayDuration) 
                : (180f / nightDuration);

            state.CurrentAngle += speed * deltaTime;

            // Keep angle between 0 and 360
            if (state.CurrentAngle >= 360f) 
                state.CurrentAngle -= 360f;
        }

        public void Dispose() { }
    }
}