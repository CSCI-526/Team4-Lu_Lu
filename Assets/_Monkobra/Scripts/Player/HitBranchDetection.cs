using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Attach to a detection trigger collider; fires a camera shake whenever it
/// touches a collider tagged <c>Branch</c>. Uses the
/// <c>CinemachineImpulseSource</c> on the same object.
/// </summary>
[RequireComponent(typeof(CinemachineImpulseSource))]
public class HitBranchDetection: MonoBehaviour {
    // Public Members  #########################################################
    public static HitBranchDetection I {
        get; private set;
    }

    /// <summary>
    /// Raised with the branch collider whenever this trigger touches one.
    /// </summary>
    public event Action<Collider> BranchHit;

    /// <summary>
    /// Raised with the hazard collider and what struck the monkey on every
    /// hit, so camera efx can pick a shake per kind.
    /// </summary>
    public event Action<Collider, HitKind> Hit;

    /// <summary>
    /// What struck the monkey; picks which camera shake plays.
    /// </summary>
    public enum HitKind {
        Branch,
        FallingCobra,
    }

    // Public Methods  #########################################################
    /// <summary>
    /// Applies a branch hit from a hazard that touched another part of the
    /// monkey, not this sensor: the same camera shake and
    /// <see cref="BranchHit"/> as a sensor contact.
    /// </summary>
    /// <param name="source">the hazard's collider</param>
    public void ReportHit(Collider source) {
        ApplyHit(source, HitKind.Branch);
    }

    /// <summary>
    /// Same as <see cref="ReportHit(Collider)"/>, tagged with what struck
    /// the monkey.
    /// </summary>
    /// <param name="source">the hazard's collider</param>
    /// <param name="kind">the hazard type, picks the camera shake</param>
    public void ReportHit(Collider source, HitKind kind) {
        ApplyHit(source, kind);
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        I = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    // Event Handlers  #########################################################
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag(BRANCH_TAG)) {
            return;
        }

        ApplyHit(other, HitKind.Branch);
    }

    // private methods  ########################################################
    private void ApplyHit(Collider source, HitKind kind) {
        if (Debug.isDebugBuild) {
            Debug.Log($"HitBranchDetection:\thit {kind} {source.name}");
        }
        impulseSource.GenerateImpulse();
        Hit?.Invoke(source, kind);
        BranchHit?.Invoke(source);
    }

    // constants  ##############################################################
    private const string BRANCH_TAG = "Branch";

    // cached references  ------------------------------------------------------
    private CinemachineImpulseSource impulseSource;
}
