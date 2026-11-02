using System;
using UnityEngine;

/// <summary>
/// Sits on the Cobra King's root beside its kinematic Rigidbody, so every
/// bead and the head report here. Raises <see cref="Touched"/> while any
/// physical part of the monkey (body, tail or hand) overlaps the king; its
/// sensing triggers do not count.
/// </summary>
public class BossKingContact: MonoBehaviour {
    // Public Members  #########################################################
    public event Action Touched;

    // MonoBehaviour Lifecycle  ################################################
    private void OnTriggerEnter(Collider other) {
        ReportIfMonkey(other);
    }

    private void OnTriggerStay(Collider other) {
        ReportIfMonkey(other);
    }

    // private methods  ########################################################
    private void ReportIfMonkey(Collider other) {
        if (IsMonkeyPart(other)) {
            Touched?.Invoke();
        }
    }

    /// <returns>true for the monkey's body, tail and hands; false for its
    /// sensing volumes, which reach far past the body</returns>
    private static bool IsMonkeyPart(Collider other) {
        // a hand is a trigger, but still a real part of the monkey
        if (other.GetComponentInParent<ArmController>() != null) {
            return true;
        }
        if (other.isTrigger) {
            return false;
        }
        for (Transform t = other.transform; t != null; t = t.parent) {
            if (t.CompareTag("Player")) {
                return true;
            }
        }
        return false;
    }
}
