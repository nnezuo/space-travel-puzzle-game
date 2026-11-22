using UnityEngine;

/*
 * HUDZoneFilter
 * -------------
 * Hides the GameHUD when the player is on some planets.
 * We need it because planet 2 (the Dream Garden) has its own puzzle, so
 * the HUD of planet 1 (enemies, crystals, gravity keys) doesn't make sense
 * there.
 *
 * Put it on the same object as GameHUD, and drag in "hideOn" the
 * GravitySource of each planet where the HUD should be hidden.
 * We just turn the GameHUD component off (enabled = false), so OnGUI is
 * not called anymore and nothing is drawn.
 *
 * Things to clean:
 *  - The Debug.Log when the planet changes was for testing, it can be removed.
 *  - If the player is not found, FindAnyObjectByType is called EVERY frame,
 *    which is slow. GameHUD does it only once per second, we could do the same.
 *  - When the HUD is disabled, GameHUD.Say() still adds messages, so they
 *    can appear all at once when we come back to a normal planet.
 */
public class HUDZoneFilter : MonoBehaviour
{
    public GameHUD hud;
    public PlayerController player;

    [Tooltip("Drag Planet 2's GravitySource here. The HUD is hidden while the player is on it.")]
    public GravitySource[] hideOn;

    GravitySource last;

    void Update()
    {
        // Find the references if they are not set in the Inspector
        if (hud == null) hud = GetComponent<GameHUD>();
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (hud == null || player == null) return;

        // Debug: print the planet name only when it changes (not every frame)
        GravitySource src = player.CurrentSource;
        if (src != last)
        {
            last = src;
            Debug.Log("HUDZoneFilter: player is on " + (src ? src.name : "no planet"));
        }

        // Is the current planet in the "hideOn" list?
        bool hide = false;
        if (src != null)
            foreach (var g in hideOn)
                if (g == src) { hide = true; break; }

        hud.enabled = !hide;
    }
}
