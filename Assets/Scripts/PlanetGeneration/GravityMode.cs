using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player choose the gravity with three keys. Put it on the Player, next to PlayerController.
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

    void Update()
    {
        if (!Available) return;
        if (Input.GetKeyDown(lowKey)) Select(GravitySource.Rule.Low);
        if (Input.GetKeyDown(normalKey)) Select(GravitySource.Rule.Normal);
        if (Input.GetKeyDown(reversedKey)) Select(GravitySource.Rule.Reversed);
    }

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

    public void ResetMode() { Mode = GravitySource.Rule.Normal; HeightFraction = 0f; }

    // Distance to the ground, measured from the player toward the planet's centre
    float HeightAboveGround(GravitySource source, Vector3 pos)
    {
        Vector3 toCenter = (source.transform.position - pos).normalized;
        if (Physics.Raycast(pos, toCenter, out RaycastHit hit, 500f,
                            controller.groundMask, QueryTriggerInteraction.Ignore))
            return hit.distance;
        return float.MaxValue;   // no ground below at all = definitely too far
    }
}