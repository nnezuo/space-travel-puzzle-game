using UnityEngine;

// LateUpdate already follows every Update; the order keeps this correction
// after PlanetCamera's LateUpdate, which writes the final camera position.
[DefaultExecutionOrder(1000)]
public class DreamCameraOcclusion : MonoBehaviour
{
    public Transform player;
    public DreamPuzzleController puzzle;
    private readonly RaycastHit[] hits = new RaycastHit[32];

    private void LateUpdate()
    {
        if (player == null || DreamSceneFlow.Busy ||
            (puzzle != null && (!puzzle.PlayerInGarden || puzzle.DialogueOpen || puzzle.IntroOpen))) return;
        Vector3 origin = player.position + player.up * 1.4f;
        Vector3 delta = transform.position - origin;
        float distance = delta.magnitude;
        if (distance < .01f) return;
        Vector3 direction = delta / distance;
        // The player and the garden walls share layer 0, so a layer mask
        // cannot separate them. Ignore only the player's own colliders.
        int count = Physics.SphereCastNonAlloc(origin, .18f, direction, hits, distance,
            ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null || IsPlayerCollider(collider)) continue;
            if (hits[i].distance < nearest) nearest = hits[i].distance;
        }
        if (nearest < float.PositiveInfinity)
            transform.position = origin + direction * Mathf.Max(.3f, nearest - .15f);
    }

    private bool IsPlayerCollider(Collider collider)
    {
        Transform shape = collider.transform;
        if (shape == player || shape.IsChildOf(player)) return true;
        Rigidbody body = collider.attachedRigidbody;
        return body != null && body.transform == player;
    }
}
