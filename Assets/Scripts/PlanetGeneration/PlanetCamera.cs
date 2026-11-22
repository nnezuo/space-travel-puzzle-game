using UnityEngine;

/*
 * PlanetCamera
 * ------------
 * Third person camera that follows the player around a ROUND planet.
 * Put it on the Main Camera and drag the player in "target".
 *
 * The hard part: on a planet the "up" changes all the time (when you walk
 * around the planet, at some point you are upside down compared to the
 * world). A normal follow camera uses Vector3.up so it would flip.
 * So we keep our own rotation called "frame":
 *   - frame = where the camera looks horizontally (yaw), without the pitch
 *   - every frame we turn it a little so its up matches the player's up
 *   - the mouse X turns the frame around the player's up
 *   - the mouse Y changes the pitch (looking up / down), clamped
 * Then the camera is put "distance" meters behind the player.
 *
 * The "1f - Mathf.Exp(-speed * dt)" is a smoothing trick: it gives the same
 * smoothness whatever the frame rate is (better than Lerp(a, b, speed * dt)).
 *
 * Things to improve:
 *  - In Start we use target without checking if it's null, so if we forget
 *    to set it we get a NullReferenceException.
 *  - The cursor is locked at the start and there is no key to unlock it
 *    (you need to press Esc in the editor).
 *  - The camera has no collision, so it can go through the ground / walls.
 *    In the Dream Garden scene DreamCameraOcclusion fixes this, but on the
 *    other planets there is nothing.
 *  - It uses the old Input Manager (Input.GetAxis) like the other scripts.
 */
public class PlanetCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 8f;
    public float lookHeight = 1.5f;
    public float mouseSensitivity = 2.5f;
    public float minPitch = -20f, maxPitch = 70f;
    public float followSmooth = 12f;
    public float alignSmooth = 8f;

    Quaternion frame;   // camera yaw frame (no pitch)
    float pitch = 20f;

    // Start looking in the same direction as the player
    void Start()
    {
        frame = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(target.forward, target.up), target.up);
        Cursor.lockState = CursorLockMode.Locked;
    }

    // LateUpdate = after the player moved, so the camera doesn't shake
    void LateUpdate()
    {
        if (!target) return;
        float dt = Time.deltaTime;
        Vector3 up = target.up;

        // Re-align the frame's up with the player's up (smoothly)
        // FromToRotation gives the small rotation from the old up to the new up
        Quaternion aligned = Quaternion.FromToRotation(frame * Vector3.up, up) * frame;
        frame = Quaternion.Slerp(frame, aligned, 1f - Mathf.Exp(-alignSmooth * dt));

        // Mouse look
        frame = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * mouseSensitivity, up) * frame;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, minPitch, maxPitch);

        // Final rotation = yaw frame + pitch. The camera goes back from the
        // pivot (a point above the head of the player) along its forward.
        Quaternion rot = frame * Quaternion.Euler(pitch, 0f, 0f);
        Vector3 pivot = target.position + up * lookHeight;
        Vector3 desiredPos = pivot - rot * Vector3.forward * distance;

        // Position is smoothed, but rotation is not (the frame is already smoothed above)
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followSmooth * dt));
        transform.rotation = rot;
    }
}