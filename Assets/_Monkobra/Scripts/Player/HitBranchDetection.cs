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
    /// <summary>
    /// Raised with the branch collider whenever this trigger touches one.
    /// </summary>
    public event Action<Collider> BranchHit;

    // Public Methods  #########################################################
    /// <summary>
    /// Applies a branch hit from a hazard that touched another part of the
    /// monkey, not this sensor: the same camera shake and
    /// <see cref="BranchHit"/> as a sensor contact.
    /// </summary>
    /// <param name="source">the hazard's collider</param>
    public void ReportHit(Collider source) {
        ApplyHit(source);
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    // Event Handlers  #########################################################
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag(BRANCH_TAG)) {
            return;
        }

        ApplyHit(other);
    }

    // private methods  ########################################################
    private void ApplyHit(Collider source) {
        if (Debug.isDebugBuild) {
            Debug.Log($"HitBranchDetection:\thit branch {source.name}");
        }
        impulseSource.GenerateImpulse();
        BranchHit?.Invoke(source);
    }

    // constants  ##############################################################
    private const string BRANCH_TAG = "Branch";

    // cached references  ------------------------------------------------------
    private CinemachineImpulseSource impulseSource;
}
