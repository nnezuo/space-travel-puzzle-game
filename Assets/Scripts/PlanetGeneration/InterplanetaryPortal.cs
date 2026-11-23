using UnityEngine;

/*
 * InterplanetaryPortal
 * --------------------
 * The portal that sends the player to the next planet. It is on the portal
 * prefab, and PortalSpawner fills the settings when it creates the portal.
 *
 * How it works:
 *  - Every frame we check the distance between the player and the portal.
 *  - If the player is close (activationDistance):
 *      - portal locked   -> show a message that says what to do first
 *      - portal unlocked -> show "Press [E]" and teleport when E is pressed
 *  - The portal is unlocked when the puzzle of planet 1 is complete (and
 *    if requireAllEnemiesDead is on, when all enemies are dead too).
 *
 * The teleport just moves the player to destinationSpawnPoint. The new
 * planet's gravity is found automatically by PlayerController
 * (GravitySource.GetBest), so we don't need destinationGravity.
 *
 * Things to know:
 *  - We have TWO portal scripts: this one (press E) and TriggerPortal (walk
 *    inside). We didn't have time to merge them into one script.
 *  - destinationGravity is not used by the code (see the comment).
 *  - While the player is not found, FindAnyObjectByType runs every frame.
 */
public class InterplanetaryPortal : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;                    // found automatically if empty
    [Tooltip("Empty object on the target planet. Its green (Y) arrow must point AWAY from that planet's centre")]
    public Transform destinationSpawnPoint;
    public GravitySource destinationGravity;    // not needed by the code; kept for reference

    [Header("Lock")]
    [Tooltip("The portal stays sealed until this puzzle is complete. Empty = always open")]
    public Planet1Puzzle requiredPuzzle;
    [Tooltip("Also require every enemy on the level to be dead")]
    public bool requireAllEnemiesDead = false;
    [Tooltip("Also require the Dream Garden to be completed (three memories and the castle exit)")]
    public bool requireGardenComplete = false;

    [Header("Settings")]
    public float activationDistance = 3.5f;
    public KeyCode useKey = KeyCode.E;

    // "=>" here makes a property that is calculated each time we read it
    bool IsUnlocked =>
        (requiredPuzzle == null || requiredPuzzle.IsComplete) &&
        (!requireGardenComplete || DreamProgression.Planet2Solved) &&
        (!requireAllEnemiesDead || PlanetEnemy.All.Count == 0);

    void Update()
    {
        if (player == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }
        if (player == null || destinationSpawnPoint == null) return;

        if (Vector3.Distance(player.position, transform.position) > activationDistance) return;

        if (!IsUnlocked)
        {
            if (requireGardenComplete && !DreamProgression.Planet2Solved)
                GameHUD.Prompt("The portal is sealed. Complete the Dream Garden first.");
            else
                GameHUD.Prompt("The portal is sealed. Defeat the enemies and touch the crystals.");
            return;
        }

        GameHUD.Prompt("Press [" + GameHUD.KeyLabel(useKey) + "] to travel to the next planet");
        if (Input.GetKeyDown(useKey)) TeleportPlayer();
    }

    // Move the player to the new planet.
    // We set the position on the Rigidbody AND on the Transform, so the
    // physics and the visual are both at the new place right now (if not,
    // the interpolation can show the player flying between the planets).
    // We also reset the gravity mode so he doesn't arrive in Reversed mode.
    void TeleportPlayer()
    {
        // Face the same way as before, but standing on the new planet
        Vector3 up = destinationSpawnPoint.up;
        Vector3 forward = Vector3.ProjectOnPlane(player.forward, up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(destinationSpawnPoint.forward, up);
        Quaternion rot = Quaternion.LookRotation(forward, up);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Kill the old speed so the player is not flung on arrival
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = destinationSpawnPoint.position;
            rb.rotation = rot;
        }
        player.SetPositionAndRotation(destinationSpawnPoint.position, rot);

        GravityMode mode = player.GetComponent<GravityMode>();
        if (mode != null) mode.ResetMode();

        GameHUD.Say("Travelled to the next planet", GameHUD.Kind.Success);
    }
}