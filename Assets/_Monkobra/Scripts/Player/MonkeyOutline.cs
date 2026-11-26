using UnityEngine;

/// <summary>
/// Keeps the skinned monkey model outlined at all times, with the same
/// world-unit inverted-hull outline a fruit wears, in cream.
/// The outline is an extra material slot on every renderer under the model.
/// </summary>
public class MonkeyOutline: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("root of the skinned monkey model; empty = this object")]
    private Transform modelRoot;

    [SerializeField]
    [Tooltip("always-on outline material, cream, world-unit width")]
    private Material outlineMat;

    [SerializeField]
    [Tooltip("stencil mask, outlines the whole monkey not each part")]
    private Material maskMat;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (outlineMat == null) {
            Debug.LogWarning(
                "MonkeyOutline:\tmust assign Inspector Field: outlineMat",
                this
            );
            return;
        }

        Transform root = modelRoot != null ? modelRoot : transform;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers) {
            if (
                renderer is not SkinnedMeshRenderer
                && renderer is not MeshRenderer
            ) {
                continue;
            }

            // keep slot 0 only, so a leftover outline slot never doubles
            Material baseMat = renderer.sharedMaterials[0];
            renderer.sharedMaterials = maskMat != null
                ? new Material[] { baseMat, maskMat, outlineMat }
                : new Material[] { baseMat, outlineMat };
        }
    }
}
