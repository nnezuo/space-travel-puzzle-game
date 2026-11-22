using UnityEngine;

// NOTE: the namespace name looks like it comes from a terrain / grass asset
// pack this day-night system was based on. It doesn't really match our game,
// but renaming it now could break things, so we left it.
namespace FlipGameDev.TerrainAndGrassPack.DayNightSystem
{
    /*
     * DayNightBrain
     * -------------
     * This is the "boss" of the day / night system. It doesn't do anything
     * by itself, it only calls the modules.
     *
     * How it works:
     *  - All the data (the angle of the sun, how long a day is...) is in ONE
     *    object: DayNightState. Every module reads it and can change it.
     *  - Each module is a script that implements IDayNightModule and is put
     *    on this object or on a child object.
     *  - In Awake we find all the modules, and every frame we call Tick()
     *    on each of them.
     *
     * The good thing is that we can add a new effect (for example rain at
     * night) just by making a new module, without touching the other scripts.
     *
     * Things to know / improve:
     *  - The modules are called in the order of the hierarchy. The
     *    TimeProgressionModule (which moves the angle) should be the first one,
     *    otherwise the other modules use the angle from the last frame (we
     *    don't really see it, but it's not clean).
     *  - The modules are found only once in Awake, so a module added later
     *    by code will not be used.
     *  - We also have DayNightController.cs and PlanetDayNight.cs, which do a
     *    similar job (moving the sun). We didn't have time to merge them into
     *    one system, so be careful to not use both in the same scene.
     */
    public class DayNightBrain : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private DayNightState state = new DayNightState();

        private IDayNightModule[] _modules;

        // Find every module (on this object and its children) and give them
        // the shared state so they can save their starting values.
        private void Awake()
        {
            _modules = GetComponentsInChildren<IDayNightModule>();

            foreach (var module in _modules)
            {
                module.Initialize(state);
            }
        }

        // Every frame: each module updates itself with the same state.
        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var module in _modules)
            {
                module.Tick(state, dt);
            }
        }

        // When the object is destroyed (scene change, stop Play), the modules
        // can clean what they created (for example the material copies).
        private void OnDestroy()
        {
            if (_modules == null) return;
            foreach (var module in _modules)
            {
                module.Dispose();
            }
        }
    }
}