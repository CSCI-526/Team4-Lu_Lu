using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sits on the "FruitCollider" object of a banana. Listens to
/// <see cref="UpwardFruitSensor"/> and outlines every child mesh renderer
/// in 1 of 3 tiers, grabbable winning over reachable winning over normal:
/// <list type="bullet">
/// <item>normal: worn at rest</item>
/// <item>reachable: the fruit is the one a reach would aim at</item>
/// <item>grabbable: a hand overlaps the fruit, so a release grabs it</item>
/// </list>
/// The tier materials come from the shared <see cref="FruitOutlineStyle"/>
/// by fruit kind. W/o a style the fruit stays bare, with a warning. The outline is an extra
/// material slot, and a see-through ghost is a third one: it shows only
/// where the trunk, a branch or the monkey hides the fruit, in the
/// Reachable color, or the Grabbable one while grabbable.
/// </summary>
public class FruitOutlineListener: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("object whose child meshes get outlined; empty = this object")]
    private Transform meshRoot;

    [Header("Outline Style")]
    [SerializeField]
    [Tooltip("shared widths and colors of every outline tier")]
    private FruitOutlineStyle style;

    [SerializeField]
    [Tooltip("which fruit this is; picks the hue")]
    private FruitKind kind;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        Material normalMat = null;
        Material ghostMat = null;
        Material ghostGrabMat = null;
        Material reachMat = null;
        Material grabMat = null;
        if (style != null) {
            normalMat = style.GetTierMaterial(kind, OutlineTier.Normal);
            reachMat = style.GetTierMaterial(kind, OutlineTier.Reachable);
            grabMat = style.GetTierMaterial(kind, OutlineTier.Grabbable);
            ghostMat = style.GetGhostMaterial(kind, false);
            ghostGrabMat = style.GetGhostMaterial(kind, true);
        } else {
            Debug.LogWarning(
                "FruitOutlineListener:	"
                    + "must assign Inspector Field: style, "
                    + "fruit stays without an outline",
                this
            );
        }

        fruitCollider = GetComponent<Collider>();
        if (fruitCollider == null) {
            Debug.LogError(
                "FruitOutlineListener:\tfail to get Component: Collider",
                this
            );
        }

        Transform meshSearchRoot = meshRoot != null ? meshRoot : transform;
        meshes = meshSearchRoot.GetComponentsInChildren<MeshRenderer>(true);
        normalMats = new Material[meshes.Length][];
        reachableMats = new Material[meshes.Length][];
        grabbableMats = new Material[meshes.Length][];
        for (int i = 0; i < meshes.Length; i++) {
            // keep slot 0 only, so a leftover outline slot never sticks
            Material baseMat = meshes[i].sharedMaterials[0];
            normalMats[i] = BuildMats(baseMat, normalMat, ghostMat);
            reachableMats[i] = BuildMats(baseMat, reachMat, ghostMat);
            grabbableMats[i] = BuildMats(baseMat, grabMat, ghostGrabMat);
        }
        ApplyOutline();
    }

    // read in Start, so Awake order never matters
    private void Start() {
        UpwardFruitSensor detection = UpwardFruitSensor.I;
        if (detection == null) {
            Debug.LogError(
                "FruitOutlineListener:\t"
                    + "fail to get singleton: UpwardFruitSensor",
                this
            );
            return;
        }

        detection.ReachTargetChanged += OnReachTargetChanged;
        detection.GrabbableFruitChanged += OnGrabbableFruitChanged;
        isReachable = detection.ReachTarget == fruitCollider;
        isGrabbable = detection.GrabbableFruit == fruitCollider;
        ApplyOutline();
    }

    private void OnDestroy() {
        UpwardFruitSensor detection = UpwardFruitSensor.I;
        if (detection != null) {
            detection.ReachTargetChanged -= OnReachTargetChanged;
            detection.GrabbableFruitChanged -= OnGrabbableFruitChanged;
        }
    }

    // Event Handlers  #########################################################
    private void OnReachTargetChanged(Collider previous, Collider current) {
        if (current == fruitCollider) {
            isReachable = true;
        } else if (previous == fruitCollider) {
            isReachable = false;
        } else {
            return;
        }
        ApplyOutline();
    }

    private void OnGrabbableFruitChanged(Collider previous, Collider current) {
        if (current == fruitCollider) {
            isGrabbable = true;
        } else if (previous == fruitCollider) {
            isGrabbable = false;
        } else {
            return;
        }
        ApplyOutline();
    }

    // private members  ########################################################
    private bool isReachable;
    private bool isGrabbable;

    // cached references  ------------------------------------------------------
    private Collider fruitCollider;
    private MeshRenderer[] meshes;

    // per mesh: slot 0 alone, or slot 0 plus a tier's outline
    private Material[][] normalMats;
    private Material[][] reachableMats;
    private Material[][] grabbableMats;

    // private methods  ########################################################
    // w/o an outline or ghost material that slot drops, never a null slot
    private static Material[] BuildMats(
        Material baseMat, Material outlineMat, Material ghostMat
    ) {
        var mats = new List<Material> { baseMat };
        if (outlineMat != null) {
            mats.Add(outlineMat);
        }
        if (ghostMat != null) {
            mats.Add(ghostMat);
        }
        return mats.ToArray();
    }

    // grabbable beats reachable beats normal
    private void ApplyOutline() {
        Material[][] tierMats =
            isGrabbable ? grabbableMats
            : isReachable ? reachableMats
            : normalMats;
        for (int i = 0; i < meshes.Length; i++) {
            meshes[i].sharedMaterials = tierMats[i];
        }
    }
}
