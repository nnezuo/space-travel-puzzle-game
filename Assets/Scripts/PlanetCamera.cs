using UnityEngine;

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

    void Start()
    {
        frame = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(target.forward, target.up), target.up);
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (!target) return;
        float dt = Time.deltaTime;
        Vector3 up = target.up;

        // Re-align the frame's up with the player's up (smoothly)
        Quaternion aligned = Quaternion.FromToRotation(frame * Vector3.up, up) * frame;
        frame = Quaternion.Slerp(frame, aligned, 1f - Mathf.Exp(-alignSmooth * dt));

        // Mouse look
        frame = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * mouseSensitivity, up) * frame;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, minPitch, maxPitch);

        Quaternion rot = frame * Quaternion.Euler(pitch, 0f, 0f);
        Vector3 pivot = target.position + up * lookHeight;
        Vector3 desiredPos = pivot - rot * Vector3.forward * distance;

        transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followSmooth * dt));
        transform.rotation = rot;
    }
}