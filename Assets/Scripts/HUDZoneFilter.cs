using UnityEngine;

public class HUDZoneFilter : MonoBehaviour
{
    public GameHUD hud;
    public PlayerController player;

    [Tooltip("Drag Planet 2's GravitySource here. The HUD is hidden while the player is on it.")]
    public GravitySource[] hideOn;

    GravitySource last;

    void Update()
    {
        if (hud == null) hud = GetComponent<GameHUD>();
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (hud == null || player == null) return;

        GravitySource src = player.CurrentSource;
        if (src != last)
        {
            last = src;
            Debug.Log("HUDZoneFilter: player is on " + (src ? src.name : "no planet"));
        }

        bool hide = false;
        if (src != null)
            foreach (var g in hideOn)
                if (g == src) { hide = true; break; }

        hud.enabled = !hide;
    }
}
