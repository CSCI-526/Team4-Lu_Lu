using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>Camera, decor, primitive and color helpers, and static fight
/// helpers.</summary>
public sealed class SpiderKingFeedback {
    // private members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingTuning tuning;

    public SpiderKingFeedback(SpiderKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // Camera & Helpers  -------------------------------------------------------
    // pulled back, aimed a little toward the spider, so both stay in view
    public void UpdateCamera() {
        // the parry close up is its own camera move (ParryShot), on top,
        // cut short once another attack comes
        rig.Parry.CutShotIfMoreComes();
        if (rig.cameraFraming == null) {
            return;
        }
        float focus =
            Mathf.Clamp((rig.spiderY - rig.monkey.position.y) * 0.3f, 0f, 3f);
        rig.cameraFraming.SetBossFraming(focus, tuning.CameraDistanceScale);
    }

    // if the action is wired & went down this frame; it is shared, so it is
    // switched back on here at the moment of use
    public static bool WasPressed(InputActionReference reference) {
        if (reference == null) {
            return false;
        }
        InputAction action = reference.action;
        if (!action.enabled) {
            action.Enable();
        }
        return action.WasPressedThisFrame();
    }

    public static void SetUntouchable(bool isUntouchable) {
        if (GameController.I != null) {
            GameController.I.SetLeaping(isUntouchable);
        }
    }

    // the usual stun & drop w/ the heavier shake, never fatal
    public static void KnockDown(Collider source) {
        HitBranchSensor sensor = HitBranchSensor.I;
        if (sensor != null && source != null) {
            sensor.ReportHit(
                source,
                HitBranchSensor.HitKind.FallingCobraController
            );
        }
    }

    // a decor primitive in the body's lit material, no collider
    public GameObject CreatePrimitive(
        PrimitiveType type,
        string objectName,
        Vector3 scale,
        Color color
    ) {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = objectName;
        // off at once & removed at the frame's end: an immediate destroy is
        // refused when a part is made in a physics callback, eg a save's
        // burst on the great cobra's bite
        Collider partCollider = part.GetComponent<Collider>();
        partCollider.enabled = false;
        UnityEngine.Object.Destroy(partCollider);
        part.transform.SetParent(rig.host, true);
        part.transform.localScale = scale;
        Renderer partRenderer = part.GetComponent<Renderer>();
        if (rig.body.PrimitiveMaterial != null) {
            partRenderer.sharedMaterial = rig.body.PrimitiveMaterial;
        }
        SetColor(partRenderer, color);
        return part;
    }

    public void SetColor(Renderer target, Color color) {
        if (target == null) {
            return;
        }
        target.GetPropertyBlock(rig.colorBlock);
        rig.colorBlock.SetColor(BASE_COLOR, color);
        rig.colorBlock.SetColor(COLOR, color);
        target.SetPropertyBlock(rig.colorBlock);
    }

    public static Transform FindChild(Transform root, string childName) {
        foreach (Transform child in
            root.GetComponentsInChildren<Transform>(true)) {
            if (child.name == childName) {
                return child;
            }
        }
        return null;
    }

    // a cylinder stretched b/t two world points
    public static void SetLine(Transform line, Vector3 from, Vector3 to,
        float thickness) {
        Vector3 span = to - from;
        if (span.sqrMagnitude < 0.000001f) {
            return;
        }
        line.SetPositionAndRotation(
            (from + to) * 0.5f,
            Quaternion.FromToRotation(Vector3.up, span.normalized)
        );
        line.localScale = new Vector3(thickness, span.magnitude * 0.5f,
            thickness);
    }

    // a prefab copy kept only as a picture: no script, no collider; the
    // scripts go first, as one may require its collider (SpiderWebTrap's
    // BoxCollider), which can't be removed while it is there
    public static void MakeDecor(GameObject decor) {
        // at once, so none wakes up when the copy is switched on
        foreach (MonoBehaviour script in
            decor.GetComponentsInChildren<MonoBehaviour>(true)) {
            UnityEngine.Object.DestroyImmediate(script);
        }
        foreach (Collider part in
            decor.GetComponentsInChildren<Collider>(true)) {
            UnityEngine.Object.DestroyImmediate(part);
        }
    }

}
