using UnityEngine;

/*
 * PlayerController
 * ----------------
 * This is the main script to move the player on the planets.
 * Because our planets are round, we can't use the normal Unity gravity
 * (it always goes down on the Y axis). So we turn it off and we do our own:
 *
 *   - Every physics step we ask GravitySource.GetBest() which planet is the
 *     closest / strongest, and we pull the player toward its centre.
 *   - "Up" for the player is the direction from the planet centre to the player.
 *   - The movement (WASD / arrows) is relative to the camera, but projected
 *     on the surface of the planet (the "tangent plane").
 *   - At the end we rotate the player so his feet point to the planet and
 *     he looks where he is walking.
 *
 * If the player also has a GravityMode component, the keys 1/2/3 can change
 * the gravity (Low / Normal / Reversed). See GravityMode.cs.
 *
 * Other scripts use CurrentSource (the planet we are on) and IsGrounded,
 * for example GameHUD and the portals.
 *
 * The player object needs a Rigidbody (RequireComponent adds it for us).
 */
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

    // Setup of the Rigidbody:
    //  - useGravity = false because we do our own gravity toward the planet
    //  - FreezeRotation so the physics doesn't make the player fall over,
    //    we rotate him ourself at the end of FixedUpdate
    //  - Interpolate so the movement looks smooth with the camera
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>(); // Automatically finds it on the child model
        rb.useGravity = false;                       // we apply our own gravity
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        gravityMode = GetComponent<GravityMode>();
    }

    // We read the keyboard in Update (every frame) because GetButtonDown can be
    // missed in FixedUpdate. The jump is "queued" and done in the next FixedUpdate.
    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetButtonDown("Jump")) jumpQueued = true;
    }

    /*
     * All the physics is done here, step by step.
     * NOTE: the step numbers in the comments jump (1, 2, 5, 6, 7) because
     * some steps were removed or merged when we put our code together.
     * We didn't renumber them.
     */
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

        // Camera-relative movement on the tangent plane
        // We take the camera forward and remove the part that goes "up", so we
        // get a direction that is flat on the ground. If the camera looks
        // straight down (fwd is almost zero) we use the player forward instead.
        // right = cross(up, forward) gives the direction for the A/D keys.
        Vector3 fwd = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, up) : Vector3.zero;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.ProjectOnPlane(transform.forward, up);
        fwd.Normalize();
        Vector3 right = Vector3.Cross(up, fwd);

        Vector3 moveDir = Vector3.ClampMagnitude(fwd * input.y + right * input.x, 1f);
        Vector3 desired = moveDir * moveSpeed;

        // Split velocity into vertical (along up) and horizontal parts
        // We only change the horizontal part with the input (MoveTowards makes
        // a small acceleration so it's not instant). The vertical part is kept,
        // so the gravity and the jump still work.
        Vector3 vel = rb.linearVelocity;                 
        Vector3 vertical = Vector3.Project(vel, up);
        Vector3 horizontal = vel - vertical;
        horizontal = Vector3.MoveTowards(horizontal, desired, acceleration * Time.fixedDeltaTime);

        // 5. Jump
        if (jumpQueued && IsGrounded)
            vertical = up * jumpSpeed;
        jumpQueued = false;

        /*
         * MISTAKE (left from merging the animation code, not fixed yet):
         * The lines below are repeated. linearVelocity is set 3 times (not a
         * big problem, it's the same value) but AddForce(gravity) is called
         * 2 TIMES, so the gravity is actually doubled every physics step.
         * Also "anim = GetComponentInChildren..." is called here every
         * FixedUpdate even if the comment says "Inside Awake()". It was copy
         * pasted from a tutorial in the wrong place. It is already done in Awake,
         * so the second one should be removed.
         * If we remove the duplicate AddForce, we will need to check the jump
         * height and the gravity values again because the game was tuned with
         * the double gravity.
         */
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
        // This part is for the character animation, which DOES NOT WORK yet.
        // - VelX / VelY: the direction of the input in the local space of the
        //   player. But we rotate the player to face moveDir (step 7), so VelX
        //   is always almost 0 and VelY is almost 1 when we walk. A strafe
        //   blend tree will never really be used.
        // - Speed: here it is the real speed (0 to moveSpeed), but
        //   PlayerAnimatorController.cs also sets "Speed" between 0 and 1.
        //   Only one of them should do it. See the notes in that file.
        if (anim != null)
        {
            // Convert world movement direction into local character space
            Vector3 localMoveDir = transform.InverseTransformDirection(moveDir);

            anim.SetFloat("VelX", localMoveDir.x * input.magnitude);
            anim.SetFloat("VelY", localMoveDir.z * input.magnitude);
            anim.SetFloat("Speed", horizontal.magnitude);
        }

        // 7. Orient feet to the surface, face movement direction
        // LookRotation(face, up) builds a rotation where the player looks at
        // "face" and his head points to "up". Slerp makes it turn smoothly
        // instead of snapping. If we don't move, we keep the old forward.
        Vector3 face = moveDir.sqrMagnitude > 0.01f ? moveDir : Vector3.ProjectOnPlane(transform.forward, up);
        if (face.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(face, up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, alignSpeed * Time.fixedDeltaTime));
        }


    }
}