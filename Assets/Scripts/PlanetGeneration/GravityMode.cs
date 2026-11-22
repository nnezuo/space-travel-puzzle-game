using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player choose the gravity with three keys. Put it on the Player, next to PlayerController.
/*
 * GravityMode
 * -----------
 * This is one of the main mechanics of the game: with the keys 1 / 2 / 3
 * the player chooses Low, Normal or Reversed gravity.
 *   - Low:      the gravity is weaker, so you jump much higher
 *   - Normal:   the planet's normal gravity
 *   - Reversed: you slowly float away from the planet (to see from above),
 *               but only up to reversedMaxHeight, then you fall back
 *
 * How it is connected:
 *   PlayerController.FixedUpdate  ->  GravityMode.GetGravity(source, pos)
 *                                 ->  GravitySource.GetGravity(pos, Mode)
 * GameHUD reads Mode, Available and HeightFraction to draw the keys and
 * the altitude bar.
 *
 * Some planets don't allow the keys (playerCanChangeGravity = false on
 * the GravitySource). Then "Available" is false and the keys do nothing.
 *
 * Things to improve:
 *  - "using UnityEngine.InputSystem" is not used, we still use the old
 *    Input class (Input.GetKeyDown). We could remove it or move everything
 *    to the new Input System.
 *  - When we arrive on a new planet, the mode goes back to Normal without
 *    any message, so the player doesn't really know why.
 *  - Available is only updated when PlayerController calls GetGravity, so
 *    when the player is in no gravity zone, it keeps the old value.
 */
[RequireComponent(typeof(PlayerController))]
public class GravityMode : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode lowKey = KeyCode.Alpha1;
    public KeyCode normalKey = KeyCode.Alpha2;
    public KeyCode reversedKey = KeyCode.Alpha3;

    [Header("Reversed gravity (a gentle float, not a launch)")]
    [Tooltip("Fraction of the planet's gravity used to push the player away. Lower = slower rise")]
    [Range(0.1f, 1f)] public float reversedStrength = 0.4f;
    [Tooltip("Height above the ground where reversed gravity stops working and the player falls")]
    public float reversedMaxHeight = 15f;

    public GravitySource.Rule Mode { get; private set; } = GravitySource.Rule.Normal;
    public bool Available { get; private set; }       // false on planets that do not allow the keys
    public float HeightFraction { get; private set; } // 0..1, how close to the limit (for the UI bar)

    PlayerController controller;
    GravitySource lastSource;

    void Awake() => controller = GetComponent<PlayerController>();

    // Read the 3 keys (only if this planet allows it)
    void Update()
    {
        if (!Available) return;
        if (Input.GetKeyDown(lowKey)) Select(GravitySource.Rule.Low);
        if (Input.GetKeyDown(normalKey)) Select(GravitySource.Rule.Normal);
        if (Input.GetKeyDown(reversedKey)) Select(GravitySource.Rule.Reversed);
    }

    // Change the mode and show a message on the HUD so the player knows
    void Select(GravitySource.Rule rule)
    {
        if (rule == Mode) return;

        // No point starting a float if we are already at the limit
        if (rule == GravitySource.Rule.Reversed && controller.CurrentSource != null &&
            HeightAboveGround(controller.CurrentSource, transform.position) > reversedMaxHeight * 0.9f)
        {
            GameHUD.Say("Too high to reverse gravity", GameHUD.Kind.Warning);
            return;
        }

        Mode = rule;
        switch (rule)
        {
            case GravitySource.Rule.Low: GameHUD.Say("Low gravity: you jump much higher", GameHUD.Kind.Info); break;
            case GravitySource.Rule.Normal: GameHUD.Say("Normal gravity", GameHUD.Kind.Info); break;
            case GravitySource.Rule.Reversed: GameHUD.Say("Reversed gravity: you float up. Do not go too far!", GameHUD.Kind.Info); break;
        }
    }

    // Called by PlayerController every physics step instead of source.GetGravity
    // In Reversed mode we measure the height every step. If the player is
    // higher than the limit, we switch back to Normal so he falls back down
    // (this way he can't fly away to space forever).
    // The reversed force is multiplied by reversedStrength so the player
    // floats up slowly instead of being shot up.
    public Vector3 GetGravity(GravitySource source, Vector3 pos)
    {
        // New planet: start again in normal gravity
        if (source != lastSource) { lastSource = source; Mode = GravitySource.Rule.Normal; }

        Available = source.playerCanChangeGravity;
        HeightFraction = 0f;
        if (!Available) return source.GetGravity(pos);   // this planet decides by itself

        if (Mode == GravitySource.Rule.Reversed)
        {
            float height = HeightAboveGround(source, pos);
            HeightFraction = Mathf.Clamp01(height / reversedMaxHeight);

            if (height > reversedMaxHeight)   // too far from the planet: fall back
            {
                Mode = GravitySource.Rule.Normal;
                HeightFraction = 0f;
                GameHUD.Say("Too far from the planet! Gravity is back to normal.", GameHUD.Kind.Warning);
            }
        }

        float factor = Mode == GravitySource.Rule.Reversed ? reversedStrength : 1f;
        return source.GetGravity(pos, Mode) * factor;
    }

    // Used by other scripts (for example after a portal) to go back to normal
    public void ResetMode() { Mode = GravitySource.Rule.Normal; HeightFraction = 0f; }

    // Distance to the ground, measured from the player toward the planet's centre
    // We shoot a ray (max 500 m) to the centre and take the distance of the hit.
    // It uses the same groundMask as PlayerController, so the player layer
    // must be excluded there, or the ray will hit the player himself.
    float HeightAboveGround(GravitySource source, Vector3 pos)
    {
        Vector3 toCenter = (source.transform.position - pos).normalized;
        if (Physics.Raycast(pos, toCenter, out RaycastHit hit, 500f,
                            controller.groundMask, QueryTriggerInteraction.Ignore))
            return hit.distance;
        return float.MaxValue;   // no ground below at all = definitely too far
    }
}