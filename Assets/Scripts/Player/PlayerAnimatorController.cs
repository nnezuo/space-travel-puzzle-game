using UnityEngine;

/*
 * PlayerAnimatorController
 * ------------------------
 * This script is supposed to play the walk / run animation of the character.
 * It goes on the character model (the child of the Player object). It reads
 * how fast the Rigidbody of the player is moving and sends this speed to the
 * Animator, so the blend tree can choose between idle, walk and run.
 *
 * !!! IMPORTANT: THIS DOES NOT WORK RIGHT NOW !!!
 * We tried a lot of things but the character never played the animation
 * correctly, and we didn't have time to fix it before the deadline.
 * Here is what we think is wrong (so someone can fix it later):
 *
 *  1. The speed is calculated with the world X and Z of the velocity
 *     (see Update below). This works on a flat ground, but our player walks
 *     on a ROUND planet. When the player is on the "side" or the "bottom" of
 *     the planet, the world Y is not "up" anymore, so the speed we compute is
 *     wrong (it can be almost 0 while the player is running, or it counts the
 *     falling speed). We should remove the part along transform.up, like
 *     PlayerController does with Vector3.ProjectOnPlane(vel, up).
 *
 *  2. PlayerController.cs ALSO sets the "Speed" parameter in FixedUpdate.
 *     This script sends a value between 0 and 1, and PlayerController sends
 *     the real speed (0 to 6). So the two scripts fight and the value jumps
 *     all the time. Only ONE script should set the Animator parameters.
 *
 *  3. The max speed 6 is written by hand here. If someone changes moveSpeed
 *     in PlayerController, this will be wrong. It should read moveSpeed.
 *
 *  4. Things to check in the Unity Editor (maybe the problem is there and not
 *     in the code):
 *       - Does the Animator Controller really have a float parameter called
 *         exactly "Speed" (and "VelX" / "VelY" used by PlayerController)?
 *         The name must match, with the capital letters.
 *       - Do the thresholds in the blend tree match the values we send
 *         (0..1 or 0..6)?
 *       - "Apply Root Motion" should be OFF, because the Rigidbody moves the
 *         player, not the animation.
 *       - Is the Avatar / Rig of the model set correctly (Humanoid or Generic)?
 *       - GetComponentInChildren<Animator>() takes the FIRST Animator it finds,
 *         maybe it is not the one on the model we see.
 */
public class PlayerAnimatorController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rb;

    // We get the Animator (on the model) and the Rigidbody (on the parent Player).
    // If one of them is not found, Update does nothing (no error message, so it
    // fails silently, it would be better to add a Debug.LogWarning here).
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponentInParent<Rigidbody>(); // Grabs Rigidbody from the parent Player object
    }

    void Update()
    {
        if (anim == null || rb == null) return;

        // Calculate horizontal movement speed magnitude
        // MISTAKE: this "horizontal" uses the world Y as up, but on a planet
        // up = transform.up. Should be Vector3.ProjectOnPlane(rb.linearVelocity, transform.up).
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = horizontalVel.magnitude;

        // Normalize speed based on max movement speed (e.g., 6)
        float speedPercent = Mathf.Clamp01(currentSpeed / 6f);

        // Send values to the Animator parameters
        // WARNING: PlayerController also writes "Speed" (with another scale), see the note at the top.
        anim.SetFloat("Speed", speedPercent);
    }
}