using UnityEngine;


/// <summary>
/// The moss mask of the <c>Monkobra/ToonBark</c> shader, the same math on the
/// CPU, so decorations such as <see cref="TrunkDecor"/> can find where the
/// shader paints moss and hang moss strands off its patches.
/// <para>
/// Keep in step with <c>MossMask</c> and <c>MossThreshold</c> in
/// <c>Shaders/Environment/ToonBark.shader</c>.
/// </para>
/// </summary>
public static class StylizedBarkNoise {
    // Public Methods  #########################################################
    /// <summary>
    /// Reads the moss tuning off a ToonBark material.
    /// </summary>
    public static MossSettings ReadMoss(Material material) {
        if (material == null || !material.HasProperty(MOSS_SCALE_ID)) {
            return new MossSettings(0.28f, 0.55f, 0f);
        }
        return new MossSettings(
            material.GetFloat(MOSS_SCALE_ID),
            material.GetFloat(MOSS_STRETCH_ID),
            material.GetFloat(MOSS_COVERAGE_ID)
        );
    }

    /// <returns>the moss mask at a world position, 0 to 1, about 0.5 on
    /// average; moss grows where it reaches <see cref="MossThreshold"/>
    /// </returns>
    public static float MossMask(Vector3 positionWS, MossSettings moss) {
        Vector3 p = Turn(new Vector3(
            positionWS.x * moss.scale,
            positionWS.y * moss.scale * moss.stretch,
            positionWS.z * moss.scale
        ));
        Vector3 p2 = Turn(p) * 2.03f + Vector3.one * 17.1f;
        Vector3 p3 = Turn(p2) * 2.01f + Vector3.one * 41.7f;
        return ValueNoise3(p) * 0.6f + ValueNoise3(p2) * 0.28f
            + ValueNoise3(p3) * 0.12f;
    }

    /// <returns>mask value at which moss starts</returns>
    public static float MossThreshold(MossSettings moss) {
        return Mathf.Lerp(0.78f, 0.22f, moss.coverage);
    }

    /// <returns>if the shader paints moss at a world position</returns>
    public static bool IsMossy(Vector3 positionWS, MossSettings moss) {
        return moss.coverage > 0.001f
            && MossMask(positionWS, moss) >= MossThreshold(moss);
    }

    // Nested Types  ###########################################################
    /// <summary>moss tuning of one ToonBark material</summary>
    public readonly struct MossSettings {
        public readonly float scale;
        public readonly float stretch;
        public readonly float coverage;

        public MossSettings(float scale, float stretch, float coverage) {
            this.scale = scale;
            this.stretch = stretch;
            this.coverage = coverage;
        }
    }

    // private members  ########################################################
    private static readonly int MOSS_SCALE_ID =
        Shader.PropertyToID("_MossScale");
    private static readonly int MOSS_STRETCH_ID =
        Shader.PropertyToID("_MossStretch");
    private static readonly int MOSS_COVERAGE_ID =
        Shader.PropertyToID("_MossCoverage");

    // the shader's NOISE_TURN, turning noise off the world axes
    private static Vector3 Turn(Vector3 p) {
        return new Vector3(
            0.00f * p.x + 0.80f * p.y + 0.60f * p.z,
            -0.80f * p.x + 0.36f * p.y - 0.48f * p.z,
            -0.60f * p.x - 0.48f * p.y + 0.64f * p.z
        );
    }

    private static float Frac(float value) {
        return value - Mathf.Floor(value);
    }

    // hash without sine, as the shader's Hash13
    private static float Hash13(Vector3 p) {
        float x = Frac(p.x * 0.1031f);
        float y = Frac(p.y * 0.1031f);
        float z = Frac(p.z * 0.1031f);
        float d = x * (z + 31.32f) + y * (y + 31.32f) + z * (x + 31.32f);
        x += d;
        y += d;
        z += d;
        return Frac((x + y) * z);
    }

    private static float ValueNoise3(Vector3 p) {
        Vector3 i =
            new Vector3(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z));
        Vector3 f = p - i;
        Vector3 u = new Vector3(
            f.x * f.x * (3f - 2f * f.x),
            f.y * f.y * (3f - 2f * f.y),
            f.z * f.z * (3f - 2f * f.z)
        );
        float n000 = Hash13(i);
        float n100 = Hash13(i + new Vector3(1, 0, 0));
        float n010 = Hash13(i + new Vector3(0, 1, 0));
        float n110 = Hash13(i + new Vector3(1, 1, 0));
        float n001 = Hash13(i + new Vector3(0, 0, 1));
        float n101 = Hash13(i + new Vector3(1, 0, 1));
        float n011 = Hash13(i + new Vector3(0, 1, 1));
        float n111 = Hash13(i + new Vector3(1, 1, 1));
        return Mathf.Lerp(
            Mathf.Lerp(
                Mathf.Lerp(n000, n100, u.x), Mathf.Lerp(n010, n110, u.x), u.y
            ),
            Mathf.Lerp(
                Mathf.Lerp(n001, n101, u.x), Mathf.Lerp(n011, n111, u.x), u.y
            ),
            u.z
        );
    }
}
