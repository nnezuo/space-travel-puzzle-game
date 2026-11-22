namespace FlipGameDev.TerrainAndGrassPack.DayNightSystem
{
    /*
     * IDayNightModule
     * ---------------
     * An interface = a list of methods that a class promises to have.
     * Every module of the day / night system (time, rotation, lights...)
     * implements it, so DayNightBrain can call all of them in the same way
     * without knowing what each one really does.
     *
     *  - Initialize: called one time at the start (in Awake of the brain)
     *  - Tick:       called every frame with the time since the last frame
     *  - Dispose:    called when the brain is destroyed, to clean things
     */
    public interface IDayNightModule
    {
        void Initialize(DayNightState state);
        void Tick(DayNightState state, float deltaTime);
        void Dispose();
    }
}