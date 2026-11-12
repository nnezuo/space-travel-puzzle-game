using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    private Animator anim; // Added reference

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 40f;
    public float jumpSpeed = 9f;          // fixed speed => low gravity gives higher jumps automatically
    public float alignSpeed = 12f;

    [Header("Ground check")]
    public float halfHeight = 1f;         // distance from pivot to feet (cylinder/capsule of height 2 = 1)
    public float groundTolerance = 0.2f;
    public LayerMask groundMask = ~0;     // exclude the Player layer in the Inspector

    public GravitySource CurrentSource { get; private set; }
    public bool IsGrounded { get; private set; }

    Rigidbody rb;
    GravityMode gravityMode;
    Vector2 input;
    bool jumpQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>(); // Automatically finds it on the child model
        rb.useGravity = false;                       // we apply our own gravity
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        gravityMode = GetComponent<GravityMode>();
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetButtonDown("Jump")) jumpQueued = true;
    }

    void FixedUpdate()
    {
        // 1. Gravity from the best source
        CurrentSource = GravitySource.GetBest(rb.position);

        Vector3 gravity = Vector3.zero;
        Vector3 up = transform.up;
        if (CurrentSource != null)
        {
            // If the player has a GravityMode, his keys choose Low / Normal / Reversed
            gravity = gravityMode != null
                ? gravityMode.GetGravity(CurrentSource, rb.position)
                : CurrentSource.GetGravity(rb.position);

            // "Up" = away from the planet's centre. It no longer flips when gravity is reversed,
            // so the player (and the camera) stay upright while floating up
            Vector3 away = rb.position - CurrentSource.transform.position;
            if (away.sqrMagnitude > 0.0001f) up = away.normalized;
        }


        // 2. Ground check (raycast along -up, ignoring triggers)
        IsGrounded = Physics.Raycast(rb.position, -up, halfHeight + groundTolerance,
                                     groundMask, QueryTriggerInteraction.Ignore);

        // 3. Camera-relative movement on the tangent plane
        Vector3 fwd = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, up) : Vector3.zero;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.ProjectOnPlane(transform.forward, up);
        fwd.Normalize();
        Vector3 right = Vector3.Cross(up, fwd);

        Vector3 moveDir = Vector3.ClampMagnitude(fwd * input.y + right * input.x, 1f);
        Vector3 desired = moveDir * moveSpeed;

        // 4. Split velocity into vertical (along up) and horizontal parts
        Vector3 vel = rb.linearVelocity;                 // Unity 6 name (was rb.velocity)
        Vector3 vertical = Vector3.Project(vel, up);
        Vector3 horizontal = vel - vertical;
        horizontal = Vector3.MoveTowards(horizontal, desired, acceleration * Time.fixedDeltaTime);

        // 5. Jump
        if (jumpQueued && IsGrounded)
            vertical = up * jumpSpeed;
        jumpQueued = false;

        rb.linearVelocity = horizontal + vertical;

        rb.linearVelocity = horizontal + vertical;

        // 6. Apply gravity
        rb.AddForce(gravity, ForceMode.Acceleration);

        // Inside Awake():
        anim = GetComponentInChildren<Animator>(); // Finds the Animator on the child model

        // At the end of FixedUpdate(), right after setting rb.linearVelocity:
        rb.linearVelocity = horizontal + vertical;
        rb.AddForce(gravity, ForceMode.Acceleration);

        // --- FEED ANIMATOR BLEND TREE ---
        if (anim != null)
        {
            // Convert world movement direction into local character space
            Vector3 localMoveDir = transform.InverseTransformDirection(moveDir);

            anim.SetFloat("VelX", localMoveDir.x * input.magnitude);
            anim.SetFloat("VelY", localMoveDir.z * input.magnitude);
            anim.SetFloat("Speed", horizontal.magnitude);
        }

        // 7. Orient feet to the surface, face movement direction
        Vector3 face = moveDir.sqrMagnitude > 0.01f ? moveDir : Vector3.ProjectOnPlane(transform.forward, up);
        if (face.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(face, up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, alignSpeed * Time.fixedDeltaTime));
        }


    }
}