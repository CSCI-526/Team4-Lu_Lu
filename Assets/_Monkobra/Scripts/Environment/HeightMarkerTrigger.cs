using System;
using UnityEngine;

/// <summary>
/// The detection half of one height marker, on the root of the
/// <c>HeightMarker</c> prefab. The root sits at the exact height the marker
/// stands for, and its trigger collider, a thin slab wider than the monkey's
/// orbit ring, raises <see cref="Passed"/> when a physical part of the
/// monkey touches it. The marker's visual is placed on the
/// <see cref="LabelAnchor"/> child instead, a fixed way above the root, so
/// the plate can sit higher than the line that counts as passing it.
/// <para>
/// <see cref="HeightMarkerSystem"/> pools these, moves them to the heights
/// in view and sets <see cref="HeightU"/>. Only the monkey counts, see
/// <see cref="CobraSensor.IsMonkeyPart"/>.
/// </para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class HeightMarkerTrigger: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>where the marker's visual sits, a fixed way above this
    /// root</returns>
    public Transform LabelAnchor => labelAnchor;

    /// <summary>
    /// the climbed height this marker stands for, set by the system; u
    /// </summary>
    public int HeightU {
        get; set;
    }

    /// <summary>
    /// Raised whenever a part of the monkey enters the slab, so more than
    /// once per pass; the listener must ignore repeats.
    /// </summary>
    public event Action<HeightMarkerTrigger> Passed;

    // Inspector Fields  #######################################################
    [Tooltip("child holding the marker's visual spot, above this root")]
    [SerializeField]
    private Transform labelAnchor;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (labelAnchor == null) {
            Debug.LogWarning(
                "HeightMarkerTrigger:\tmust assign Inspector Field: "
                    + "labelAnchor, the label falls back to this root",
                this
            );
            labelAnchor = transform;
        }

        Collider slab = GetComponent<Collider>();
        if (!slab.isTrigger) {
            Debug.LogWarning(
                "HeightMarkerTrigger:\tcollider must be a trigger",
                this
            );
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (CobraSensor.IsMonkeyPart(other)) {
            Passed?.Invoke(this);
        }
    }
}
