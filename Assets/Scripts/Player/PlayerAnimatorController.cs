using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rb;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponentInParent<Rigidbody>(); // Grabs Rigidbody from the parent Player object
    }

    void Update()
    {
        if (anim == null || rb == null) return;

        // Calculate horizontal movement speed magnitude
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = horizontalVel.magnitude;

        // Normalize speed based on max movement speed (e.g., 6)
        float speedPercent = Mathf.Clamp01(currentSpeed / 6f);

        // Send values to the Animator parameters
        anim.SetFloat("Speed", speedPercent);
    }
}