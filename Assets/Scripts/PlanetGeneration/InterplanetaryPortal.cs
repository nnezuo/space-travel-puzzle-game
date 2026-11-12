using UnityEngine;

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

    [Header("Settings")]
    public float activationDistance = 3.5f;
    public KeyCode useKey = KeyCode.E;

    bool IsUnlocked =>
        (requiredPuzzle == null || requiredPuzzle.IsComplete) &&
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
            GameHUD.Prompt("The portal is sealed. Defeat the enemies and touch the crystals.");
            return;
        }

        GameHUD.Prompt("Press [" + GameHUD.KeyLabel(useKey) + "] to travel to the next planet");
        if (Input.GetKeyDown(useKey)) TeleportPlayer();
    }

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