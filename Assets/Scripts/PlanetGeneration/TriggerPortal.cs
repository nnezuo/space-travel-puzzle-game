using UnityEngine;

/*
 * TriggerPortal
 * -------------
 * Another kind of portal: the player teleports just by WALKING INTO it
 * (no key to press). It needs a Collider with "Is Trigger" checked, and the
 * player must have the tag "Player".
 *
 * Special rule for the Dream Garden: we can't go there before planet 1 is
 * solved. When it is solved one time, we save it in DreamProgression so the
 * portal stays open after.
 *
 * The cooldown avoids a "ping-pong": when we arrive on a portal that sends
 * us back, without a cooldown we would be teleported again right away.
 * The cooldown is STATIC, so it is shared by all the TriggerPortals (this is
 * on purpose, it's what stops the ping-pong between 2 different portals).
 *
 * Things to improve:
 *  - When the portal is locked we only write in the Console (Debug.Log),
 *    the player doesn't see anything. We should use GameHUD.Say / Prompt
 *    like InterplanetaryPortal does.
 *  - DestinationIsDreamGarden has a fallback that checks the NAME of the
 *    spawn point ("dreamgarden" or "planet2"). This breaks if someone
 *    renames the object.
 *  - Unlike InterplanetaryPortal, it doesn't reset GravityMode after the
 *    teleport.
 *  - We have 2 portal scripts (this one and InterplanetaryPortal), it would
 *    be cleaner to have only one with an option "press key / walk in".
 */
public class TriggerPortal : MonoBehaviour
{
    [Header("Destination")]
    public Transform targetSpawnPoint;

    [Header("Settings")]
    public string playerTag = "Player";
    [Min(0f)] public float cooldownSeconds = 1f;

    private static float lastTeleportTime = -1000f;

    // Reset the static cooldown when we press Play (same idea as in GameHUD)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCooldown()
    {
        lastTeleportTime = -1000f;
    }

    // Called by Unity when something enters the trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || targetSpawnPoint == null) return;
        if (Time.time - lastTeleportTime < cooldownSeconds) return;

        // Lock: the Dream Garden is only open after planet 1 is solved
        if (DestinationIsDreamGarden() && !DreamProgression.Planet1Solved)
        {
            Planet1Puzzle puzzle = FindAnyObjectByType<Planet1Puzzle>();
            if (puzzle == null || !puzzle.IsComplete)
            {
                Debug.Log("portal locked: finish planet 1 before travelling to the dream garden.");
                return;
            }
            DreamProgression.MarkPlanet1Solved();
        }

        Teleport(other.transform);
        lastTeleportTime = Time.time;
    }

    // Is the destination of this portal the Dream Garden planet?
    // We check if the spawn point is close to the garden planet
    // (less than 1.7 x its radius, minimum 6 m).
    private bool DestinationIsDreamGarden()
    {
        DreamGardenGenerator garden = FindAnyObjectByType<DreamGardenGenerator>();
        if (garden != null)
        {
            // use the destination position, not an unreliable spawn name
            float limit = Mathf.Max(6f, garden.planetRadius * 1.7f);
            return Vector3.SqrMagnitude(targetSpawnPoint.position - garden.transform.position)
                < limit * limit;
        }

        // Fallback if there is no garden in the scene: check the name
        string name = targetSpawnPoint.name.ToLowerInvariant();
        return name.Contains("dreamgarden") || name.Contains("planet2");
    }

    // Move the player and keep his forward direction, only his "up" changes
    // to match the spawn point. Physics.SyncTransforms tells the physics
    // engine right now that the player moved (so the colliders are updated
    // before the next physics step).
    private void Teleport(Transform actor)
    {
        Rigidbody body = actor.GetComponent<Rigidbody>();
        Vector3 normal = targetSpawnPoint.up;
        Quaternion rotation = Quaternion.FromToRotation(actor.up, normal) * actor.rotation;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = targetSpawnPoint.position;
            body.rotation = rotation;
        }

        actor.SetPositionAndRotation(targetSpawnPoint.position, rotation);
        Physics.SyncTransforms();

        // initialize the saved garden without generating a second planet
        if (DestinationIsDreamGarden())
        {
            DreamGardenGenerator garden = FindAnyObjectByType<DreamGardenGenerator>();
            if (garden != null) garden.EnsureInitialized();
        }

        Debug.Log("portal travel completed.");
    }
}
