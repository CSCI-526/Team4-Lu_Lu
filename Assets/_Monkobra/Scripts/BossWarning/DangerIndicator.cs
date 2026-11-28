using UnityEngine;

/// <summary>Where a danger indicator's texture lies on its primitive; each
/// value is the shader's _PlaneMode.</summary>
public enum DangerIndicatorPlane {
    // the flat cap of a cylinder, eg a trunk zone
    Disc = 0,
    // the front of a cube or the side of a cylinder, along its Y, eg a lane
    Line = 1,
    // the side wall of a cylinder round the trunk, eg the sweep band
    Wall = 2
}

/// <summary>Dresses a boss's warning primitive in a Monkobra/DangerIndicator
/// material & keeps its look through a property block: the texture's own
/// colour stays, and only the opacity, a mild tint and the urgency are set.
/// </summary>
public static class DangerIndicator {
    // constants  #############################################################
    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int PLANE_MODE = Shader.PropertyToID("_PlaneMode");
    private static readonly int DIRECTION = Shader.PropertyToID("_Direction");
    private static readonly int TINT_STRENGTH =
        Shader.PropertyToID("_TintStrength");
    private static readonly int URGENCY = Shader.PropertyToID("_Urgency");

    // Public Methods  #########################################################
    // puts the material & its per-object plane, direction & tint on the
    // renderer; direction 1 runs the arrows and the scroll along the
    // primitive's +Y, -1 against it
    public static void Dress(
        Renderer target,
        MaterialPropertyBlock block,
        Material material,
        DangerIndicatorPlane plane,
        float direction,
        float tintStrength
    ) {
        if (target == null || material == null) {
            return;
        }
        target.sharedMaterial = material;
        target.GetPropertyBlock(block);
        block.SetFloat(PLANE_MODE, (float) plane);
        block.SetFloat(DIRECTION, direction);
        block.SetFloat(TINT_STRENGTH, tintStrength);
        target.SetPropertyBlock(block);
    }

    // the hue as the tint, w/ the opacity of the renderer's material times
    // the config's global opacity, so a lerp or a dim never changes alpha
    public static void SetColor(
        Renderer target,
        MaterialPropertyBlock block,
        Color hue,
        float alphaScale
    ) {
        if (target == null) {
            return;
        }
        float alpha = alphaScale;
        Material material = target.sharedMaterial;
        if (material != null && material.HasProperty(BASE_COLOR)) {
            alpha *= material.GetColor(BASE_COLOR).a;
        }
        target.GetPropertyBlock(block);
        block.SetColor(BASE_COLOR, new Color(hue.r, hue.g, hue.b, alpha));
        target.SetPropertyBlock(block);
    }

    // how near the hit is, 0~1: faster pulse & more glow in the shader
    public static void SetUrgency(
        Renderer target,
        MaterialPropertyBlock block,
        float urgency
    ) {
        if (target == null) {
            return;
        }
        target.GetPropertyBlock(block);
        block.SetFloat(URGENCY, Mathf.Clamp01(urgency));
        target.SetPropertyBlock(block);
    }

}
