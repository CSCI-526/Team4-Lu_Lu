using UnityEngine;

/// <summary>
/// Who may draw on the screen edge right now.
/// </summary>
public enum VignetteOwner {
    /// <summary>the black low-stamina pulse</summary>
    Stamina,

    /// <summary>the green great cobra proximity beat</summary>
    Cobra,
}

/// <summary>
/// The one slot both screen vignettes share, so they never draw over each
/// other: the black stamina pulse (<see cref="CameraCoolDownFeedback"/>) and
/// the green cobra proximity beat (<see cref="CobraProximityVignette"/>).
/// A pulse asks with <see cref="TryAcquire"/> and frees the slot with
/// <see cref="Release"/> when it ends. While one holds the slot the other is
/// refused, but its ask is remembered for a short while: once the slot is
/// free, an owner that played last yields to a waiting other, so the two
/// alternate instead of one starving the other.
/// <para>
/// One per gameplay scene; without it both vignettes run unchecked, as
/// before.
/// </para>
/// </summary>
public class ScreenVignetteArbiter: MonoBehaviour {
    // Public Members  #########################################################
    public static ScreenVignetteArbiter I {
        get; private set;
    }

    /// <returns>if a vignette pulse holds the slot now</returns>
    public bool IsHeld => isHeld;

    // Public Methods  #########################################################
    /// <summary>
    /// Asks for the slot; a refused ask is remembered, so the other owner's
    /// next release hands the slot over.
    /// </summary>
    /// <param name="owner">the vignette asking</param>
    /// <returns>if the slot is now held by <paramref name="owner"/>; true
    /// again for the owner that already holds it</returns>
    public bool TryAcquire(VignetteOwner owner) {
        if (isHeld) {
            if (holder == owner) {
                return true;
            }
            MarkWaiting(owner);
            return false;
        }

        // the owner that played last yields to a recent ask of the other
        if (hasPlayed && owner == lastHolder && IsWaiting(Other(owner))) {
            MarkWaiting(owner);
            return false;
        }

        isHeld = true;
        holder = owner;
        waitingSinceS[(int)owner] = float.NegativeInfinity;
        return true;
    }

    /// <summary>
    /// Frees the slot; ignored unless <paramref name="owner"/> holds it.
    /// </summary>
    /// <param name="owner">the vignette that ends its pulse</param>
    public void Release(VignetteOwner owner) {
        if (!isHeld || holder != owner) {
            return;
        }
        isHeld = false;
        lastHolder = owner;
        hasPlayed = true;
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (I != null && I != this) {
            Debug.LogWarning(
                "ScreenVignetteArbiter:\tduplicate instance, destroying this",
                this
            );
            Destroy(this);
            return;
        }
        I = this;
        waitingSinceS[0] = float.NegativeInfinity;
        waitingSinceS[1] = float.NegativeInfinity;
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    // constants  ##############################################################
    // an ask older than this no longer counts as waiting; s
    private const float WAIT_MEMORY_S = 3f;

    // private members  ########################################################
    private readonly float[] waitingSinceS = new float[2];
    private bool isHeld;
    private bool hasPlayed;
    private VignetteOwner holder;
    private VignetteOwner lastHolder;

    // private methods  ########################################################
    private static VignetteOwner Other(VignetteOwner owner) {
        return owner == VignetteOwner.Stamina
            ? VignetteOwner.Cobra
            : VignetteOwner.Stamina;
    }

    private void MarkWaiting(VignetteOwner owner) {
        waitingSinceS[(int)owner] = Time.unscaledTime;
    }

    private bool IsWaiting(VignetteOwner owner) {
        return Time.unscaledTime - waitingSinceS[(int)owner] <= WAIT_MEMORY_S;
    }
}
