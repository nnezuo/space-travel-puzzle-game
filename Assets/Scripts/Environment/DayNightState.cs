using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlipGameDev.TerrainAndGrassPack.DayNightSystem
{
    /*
     * DayNightState
     * -------------
     * A simple class that only holds the data of the day / night cycle.
     * It is shared by DayNightBrain with all the modules.
     * [Serializable] lets us see and change it in the Inspector of DayNightBrain.
     *
     * The time of the day is stored as an ANGLE (like the sun turning around):
     *    0   = sunrise
     *    90  = midday
     *    180 = sunset
     *    270 = midnight
     *
     * NOTE: the tooltips below are still in Portuguese (from the first version).
     * We didn't translate them. In English they say:
     *   CurrentAngle:         "Current angle of the cycle. 0-360"
     *   CycleDurationMinutes: "Total time of the cycle in minutes."
     * Also "using UnityEngine.Rendering" is not used in this file.
     */
    [Serializable]
    public class DayNightState
    {
        [Tooltip("Ângulo atual do ciclo. 0-360")]
        public float CurrentAngle;
        
        [Tooltip("Tempo total do ciclo em minutos.")]
        public float CycleDurationMinutes = 15f;

        /// <summary> Returns the time of the day between 0.0 and 1.0 (used to read the gradients) </summary>
        public float NormalizedTime => CurrentAngle / 360f;
        
        /// <summary> True if it is day now (angle between 0 and 180). Set by TimeProgressionModule. </summary>
        public bool IsDaytime { get; set; }
    }
}