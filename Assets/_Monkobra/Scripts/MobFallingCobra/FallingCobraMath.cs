using UnityEngine;

/// <summary>Pure spine curve and smoothing math shared by the cobra
/// roles.</summary>
public static class FallingCobraMath {
    // Public Methods  #########################################################
    public static float SmoothVisual01(float value) {
        float t = Mathf.Clamp01(value);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    public static Vector3 SpineCurveDirection(Vector3 start, Vector3 end,
        Vector3 first, Vector3 last, float t) {
        return ((6f * t * t - 6f * t) * start + (3f * t * t - 4f * t + 1f)
            * first
            +(-6f * t * t + 6f * t) * end + (3f * t * t - 2f * t)
                * last).normalized;
    }

    public static Vector3 SpineCurve(Vector3 start, Vector3 end,
        Vector3 startTangent, Vector3 endTangent, float t) {
        float t2 = t * t, t3 = t2 * t;
        return (2f * t3 - 3f * t2 + 1f) * start + (t3 - 2f * t2 + t)
            * startTangent
            + (-2f * t3 + 3f * t2) * end + (t3 - t2) * endTangent;
    }

}
