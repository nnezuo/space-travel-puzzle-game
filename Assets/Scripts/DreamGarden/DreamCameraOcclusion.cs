using UnityEngine;

// Scene-local companion to Ana's camera; does not change PlanetCamera.cs.
/*
 * DreamCameraOcclusion
 * --------------------
 * Problem: PlanetCamera has no collision, so in the Dream Garden (with walls,
 * trees, rocks...) the camera can go INSIDE objects and then we can't see
 * the player anymore.
 *
 * Fix: after PlanetCamera has moved the camera, we check if something is
 * between the player's head and the camera. If yes, we move the camera
 * forward, just in front of that object.
 *
 * How:
 *  - DefaultExecutionOrder(1000) makes this LateUpdate run AFTER the
 *    LateUpdate of PlanetCamera (which has the default order 0).
 *  - We use a SphereCast (a "thick" raycast, radius 0.18) instead of a thin
 *    Raycast, so the camera doesn't touch the walls on the sides.
 *  - "~(1 << player.gameObject.layer)" = every layer EXCEPT the player's
 *    layer, so the ray doesn't hit the player himself.
 *  - The camera stays at least 0.3 m from the head, and 0.15 m before the hit.
 *
 * It does nothing during a cutscene / transition (DreamSceneFlow.Busy),
 * when the player is not in the garden, or when a dialogue is open.
 *
 * Limits:
 *  - Only for the Dream Garden scene. The other planets still have the
 *    camera going through things, we could reuse this script there.
 *  - The camera "jumps" forward when something blocks the view (no smoothing),
 *    and PlanetCamera then smooths it back from this new position.
 *  - 1.4 (head height) is written by hand, it should be the same as
 *    PlanetCamera.lookHeight (1.5).
 */
[DefaultExecutionOrder(1000)]
public class DreamCameraOcclusion : MonoBehaviour
{
    public Transform player;
    public DreamPuzzleController puzzle;
    private void LateUpdate()
    {
        if (player == null || DreamSceneFlow.Busy || (puzzle != null && (!puzzle.PlayerInGarden || puzzle.DialogueOpen))) return;
        Vector3 origin = player.position + player.up * 1.4f;
        Vector3 delta = transform.position - origin;
        if (Physics.SphereCast(origin, .18f, delta.normalized, out RaycastHit hit, delta.magnitude,
            ~(1 << player.gameObject.layer), QueryTriggerInteraction.Ignore))
            transform.position = origin + delta.normalized * Mathf.Max(.3f, hit.distance - .15f);
    }
}
