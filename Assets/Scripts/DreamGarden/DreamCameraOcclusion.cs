using UnityEngine;

// Run after PlanetCamera so collision correction uses its final camera position.
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
        // A sphere leaves clearance beside walls. World geometry must not use the player's layer.
        if (Physics.SphereCast(origin, .18f, delta.normalized, out RaycastHit hit, delta.magnitude,
            ~(1 << player.gameObject.layer), QueryTriggerInteraction.Ignore))
            transform.position = origin + delta.normalized * Mathf.Max(.3f, hit.distance - .15f);
    }
}
