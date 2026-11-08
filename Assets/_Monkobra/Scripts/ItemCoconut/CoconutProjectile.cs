using System;
using UnityEngine;

/// <summary>
/// A coconut thrown by the monkey: falls straight down at a fixed speed and
/// ends on touching the great cobra (any body segment) or once it has fallen a
/// fixed distance, whichever comes first. <see cref="CoconutManager"/> starts
/// the freeze from the end callback, so the freeze begins at the impact, not
/// at the double-tap.
/// <para>
/// Moves with game time, so a pause holds it in the air. Put a trigger
/// collider and a kinematic Rigidbody on the prefab root, the cobra's own
/// sensor trigger colliders then report the touch.
/// </para>
/// </summary>
public class CoconutProjectile: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Starts the fall. Call once, right after spawning.
    /// </summary>
    /// <param name="speedU">fall speed; u per s</param>
    /// <param name="maxDistanceU">fall distance after which it ends w/o a
    /// hit; u</param>
    /// <param name="onEnded">called once w/ whether the cobra was hit, just
    /// b4 this object is destroyed</param>
    public void Launch(float speedU, float maxDistanceU, Action<bool> onEnded) {
        fallSpeedU = speedU;
        maxFallU = maxDistanceU;
        this.onEnded = onEnded;
        isLaunched = true;
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Update() {
        if (!isLaunched || isEnded) {
            return;
        }

        float stepU = fallSpeedU * Time.deltaTime;
        transform.position += Vector3.down * stepU;
        fallenU += stepU;
        if (fallenU >= maxFallU) {
            End(false);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!isLaunched || isEnded) {
            return;
        }
        if (other.GetComponentInParent<CobraMotor>() != null) {
            End(true);
        }
    }

    // private members  ########################################################
    private float fallSpeedU;
    private float maxFallU;
    private float fallenU;
    private bool isLaunched;
    private bool isEnded;
    private Action<bool> onEnded;

    // private methods  ########################################################
    private void End(bool isHit) {
        isEnded = true;
        onEnded?.Invoke(isHit);
        Destroy(gameObject);
    }
}
