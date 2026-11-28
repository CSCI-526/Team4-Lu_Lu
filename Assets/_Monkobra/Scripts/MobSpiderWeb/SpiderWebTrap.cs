using System;
using UnityEngine;

/// <summary>
/// Prefab root of one spider web. The strands carry no colliders: a single
/// trigger <see cref="BoxCollider"/> on this root covers the whole web and
/// reaches outward along local +X, the web's face normal, far enough to
/// touch the climbing monkey's body.
/// <para>
/// The monkey's solid body touching the trigger hands this web to its
/// <see cref="SpiderWebStruggle"/>, which decides whether it traps. The
/// struggle pulses the web on each escape press, and releases it, removing
/// it, once the monkey breaks free.
/// </para>
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SpiderWebTrap: MonoBehaviour {
    // Public Members  #########################################################
    /// <summary>
    /// Raised once per web, the first time the monkey's body touches it,
    /// whether or not it traps. Static, so listeners need no web reference.
    /// </summary>
    public static event Action<SpiderWebTrap> Encountered;

    /// <summary>
    /// Whether the web catches. An unarmed web never traps nor raises
    /// <see cref="Encountered"/>, so one in flight, eg the spider king's
    /// intro throw, passes harmlessly; arming it while the monkey is inside
    /// its trigger catches at the next physics step.
    /// </summary>
    /// <returns>if the web catches the monkey it touches</returns>
    public bool IsArmed { get; set; } = true;

    /// <returns>where the web hub sits at rest, w/o any shake offset;
    /// world position</returns>
    public Vector3 RestWorldPosition =>
        pulseTimeLeftS > 0f ? restWorldPosition : transform.position;

    // Public Methods  #########################################################
    /// <summary>
    /// Plays a short shake, a fading wobble of the web in its face plane and
    /// a small twist about its normal, visual feedback for 1 escape press.
    /// The rest pose is captured only between shakes, so rapid presses
    /// restart the shake without ever drifting it.
    /// </summary>
    public void Pulse() {
        if (pulseTimeLeftS <= 0f) {
            restWorldPosition = transform.position;
            restRotation = transform.localRotation;
        }
        pulseTimeLeftS = pulseDurationS;
    }

    /// <summary>removes the web once the monkey breaks free of it</summary>
    public void Release() {
        // stop catching now, Destroy only lands at the end of the frame
        if (trigger != null) {
            trigger.enabled = false;
        }
        Destroy(gameObject);
    }

    // Inspector Fields  #######################################################
    [Header("Escape Press Shake")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "how far the web wobbles in its face plane at the start of a "
            + "shake, fading to 0; world u"
    )]
    private float shakeAmplitudeU = 0.12f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("wobble speed of the shake; Hz")]
    private float shakeFrequencyHz = 14f;

    [SerializeField]
    [Range(0f, 30f)]
    [Tooltip("how far the web twists about its face normal at the start of "
        + "a shake; deg")]
    private float pulseTwistDeg = 6f;

    [SerializeField]
    [Tooltip("length of 1 escape press shake; s")]
    private float pulseDurationS = 0.3f;

    [Header("Catch Zone")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "how far from the hub, in the face plane, the monkey's body may touch "
            + "and still be caught; world u. The box trigger is only the broad "
            + "phase, this makes the zone round like the drawn web"
    )]
    private float catchRadiusU = 1.8f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        trigger = GetComponent<BoxCollider>();
        if (trigger != null && !trigger.isTrigger) {
            Debug.LogWarning(
                "SpiderWebTrap:\tBoxCollider must be Is Trigger, switching "
                    + "it on",
                this
            );
            trigger.isTrigger = true;
        }
    }

    private void Update() {
        if (pulseTimeLeftS <= 0f) {
            return;
        }

        pulseTimeLeftS = Mathf.Max(0f, pulseTimeLeftS - Time.deltaTime);
        // back at rest exactly, so no shake ever leaves a drift behind
        if (pulseTimeLeftS <= 0f) {
            transform.SetPositionAndRotation(
                restWorldPosition,
                RestWorldRotation
            );
            return;
        }

        // t runs 0 to 1 over the shake, the fade makes both curves end at rest
        float t = 1f - pulseTimeLeftS / pulseDurationS;
        float fade = 1f - t;
        float phase = t * pulseDurationS * shakeFrequencyHz * 2f * Mathf.PI;

        // wobble in the face plane only, 2 unequal rates so it never traces a
        // plain line; local X is the normal into the bark and stays put
        Quaternion faceRotation = RestWorldRotation;
        Vector3 offset =
            faceRotation
            * new Vector3(
                0f,
                Mathf.Sin(phase),
                Mathf.Sin(phase * 1.3f + 1.7f)
            )
            * (shakeAmplitudeU * fade);
        // twist about the face normal, swinging both ways
        float twistDeg = pulseTwistDeg * fade * Mathf.Sin(phase * 0.9f);
        transform.SetPositionAndRotation(
            restWorldPosition + offset,
            faceRotation * Quaternion.Euler(twistDeg, 0f, 0f)
        );
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        pulseDurationS = Mathf.Max(0.01f, pulseDurationS);
        shakeAmplitudeU = Mathf.Max(0f, shakeAmplitudeU);
        shakeFrequencyHz = Mathf.Max(0f, shakeFrequencyHz);
    }

    // Event Handlers  #########################################################
    private void OnTriggerEnter(Collider other) {
        TryCatch(other);
    }

    // a monkey still inside when its post-escape protection ends is caught
    // then, Enter alone would have let it through
    private void OnTriggerStay(Collider other) {
        TryCatch(other);
    }

    // private members  ########################################################
    private bool isEncountered; // whether the monkey already touched this web
    private float pulseTimeLeftS;
    private Vector3 restWorldPosition; // hub position between shakes
    private Quaternion restRotation = Quaternion.identity; // local

    private Quaternion RestWorldRotation =>
        transform.parent != null
            ? transform.parent.rotation * restRotation
            : restRotation;

    // cached references  ------------------------------------------------------
    private BoxCollider trigger;

    // private methods  ########################################################
    // only the monkey's root body capsule counts: its trigger volumes (fruit
    // zones, branch sensor) reach metres past it, its tail capsules swing
    // out, and an arm has its own Rigidbody without a struggle, so a tail or
    // a hand brushing the web is ignored
    private void TryCatch(Collider other) {
        if (!IsArmed || other.isTrigger) {
            return;
        }

        // a leap passes through, not even an encounter for analytics
        if (RunController.I != null && RunController.I.IsLeaping) {
            return;
        }

        Rigidbody otherBody = other.attachedRigidbody;
        if (
            otherBody == null
            || !otherBody.TryGetComponent(out SpiderWebStruggle struggle)
        ) {
            return;
        }

        // only the root capsule is the body: tail capsules share its
        // Rigidbody, and an arm or hand is named out so it never catches
        if (
            other.gameObject != struggle.gameObject
            || other.GetComponentInParent<ArmController>() != null
        ) {
            return;
        }

        // the box has square corners past the drawn web, so a body there is
        // not caught
        if (!IsWithinCatchRadius(other)) {
            return;
        }

        if (!isEncountered) {
            isEncountered = true;
            Encountered?.Invoke(this);
        }

        struggle.TryTrap(this);
    }

    // iff the body's nearest point to the hub lies w/i catchRadiusU, measured
    // in the face plane (local YZ) only, so depth along the normal is free
    private bool IsWithinCatchRadius(Collider body) {
        Vector3 nearest = body.ClosestPoint(transform.position);
        Vector3 local = transform.InverseTransformPoint(nearest);
        Vector3 scale = transform.lossyScale;
        float planeDistU = new Vector2(
            local.y * scale.y,
            local.z * scale.z
        ).magnitude;
        return planeDistU <= catchRadiusU;
    }
}
