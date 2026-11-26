using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static FallingCobraRig;

/// <summary>
/// <c>Monkobra/Build/Falling Cobra Body</c> bakes the falling cobra's body
/// tube into a skinned mesh, so the engine skins it from one bone per ring
/// instead of the CPU building its 1632 vertices each frame. The bind pose is
/// the tube stretched straight along +Y from the head's neck rim, with the
/// topology, UVs, colors, ring radii and collar loft the runtime tube used.
/// Saves <c>Meshes/MobFallingCobra/FallingCobraBody.asset</c> and
/// <c>Prefabs/MobFallingCobra/FallingCobraBody.prefab</c> (a
/// <see cref="SkinnedMeshRenderer"/>, the bone children and the snake
/// material) and assigns the prefab to <see cref="FallingCobraConfig"/>.
/// Run it again after changing a body or neck value of
/// <see cref="FallingCobraConfig"/>, never edit the two assets by hand.
/// Batch mode: <c>-executeMethod FallingCobraBodyBuilder.Run</c>.
/// </summary>
public static class FallingCobraBodyBuilder {
    // Public Methods  #########################################################
    /// <summary>builds the body; in batch mode exits with its result
    /// </summary>
    public static void Run() {
        bool isSucceeded = Build();
        if (Application.isBatchMode) {
            EditorApplication.Exit(isSucceeded ? 0 : 1);
        }
    }

    // private methods  ########################################################
    [MenuItem("Monkobra/Build/Falling Cobra Body", false, 70)]
    private static void RunFromMenu() {
        Build();
    }

    private static bool Build() {
        FallingCobraConfig config =
            AssetDatabase.LoadAssetAtPath<FallingCobraConfig>(CONFIG_PATH);
        GameObject reference = config != null
            ? config.CartoonAppearance.referenceBody : null;
        if (reference == null
            || !reference.TryGetComponent(out MeshFilter filter)
            || filter.sharedMesh == null
            || !reference.TryGetComponent(out MeshRenderer renderer)) {
            Debug.LogError("FallingCobraBodyBuilder:\tno reference body "
                + "(mesh & renderer) in " + CONFIG_PATH);
            return false;
        }
        Mesh source = filter.sharedMesh;
        float size = config.HitRadiusU * 2f * config.ReferenceSizeMultiplier
            / Mathf.Max(source.bounds.size.x, source.bounds.size.z);

        // the head template gives the neck rim the tube lofts out of
        GameObject scratch = new GameObject("FallingCobraBodyScratch");
        FallingCobraHeadMesh.HeadTemplate template;
        try {
            FallingCobraRig rig = new FallingCobraRig(scratch.transform,
                config);
            template = new FallingCobraHeadMesh(rig).CreateHeadTemplate(source,
                size);
            Debug.Log("FallingCobraBodyBuilder:\thead support "
                + template.supportPoints.Length + " points of "
                + template.vertices.Length + " vertices, worst depth error "
                + FallingCobraHeadMesh.WorstSupportError(template.vertices,
                    template.supportPoints) + " u");
        } finally {
            Object.DestroyImmediate(scratch);
        }
        Mesh body = BuildBodyMesh(config, template);
        Object.DestroyImmediate(template.mesh);

        EnsureFolder(MESH_FOLDER);
        body = SaveMesh(body);
        GameObject prefab = BuildPrefab(config, body,
            renderer.sharedMaterial);
        if (prefab != null) {
            SerializedObject asset = new SerializedObject(config);
            asset.FindProperty("bodyPrefab").objectReferenceValue = prefab;
            asset.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("FallingCobraBodyBuilder:\tbuilt " + body.vertexCount
            + " vertices on " + BodyRings + " bones: " + MESH_PATH + ", "
            + PREFAB_PATH + (prefab != null ? "" : " (prefab failed)"));
        return prefab != null;
    }

    // the bind-pose tube: ring r lies at y = r * step, ring 0 on the head's
    // neck rim, the neck rim lofting into the round tube over the collar
    private static Mesh BuildBodyMesh(FallingCobraConfig config,
        FallingCobraHeadMesh.HeadTemplate template) {
        int total = BodyRings * BodyStride + 2;
        int tip = total - 2, neckCap = total - 1;
        float step = config.SmoothBodyLengthU / (BodyRings - 1);
        Vector3[] vertices = new Vector3[total];
        Vector3[] normals = new Vector3[total];
        Vector2[] uv = new Vector2[total];
        Vector2[] neckUv = new Vector2[total];
        Color[] colors = new Color[total];
        BoneWeight[] weights = new BoneWeight[total];
        List<int> indices = new List<int>(BodyRings * BodySides * 6);
        for (int ring = 0; ring < BodyRings; ring++) {
            float along = ring / (float)(BodyRings - 1);
            float behind = ring * step;
            float radius = RingRadius(config, along);
            float loft = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(behind / config.NeckCollarLengthU));
            for (int side = 0; side <= BodySides; side++) {
                int vertex = ring * BodyStride + side;
                float angle = (side % BodySides) * Mathf.PI * 2f / BodySides;
                Vector3 radial =
                    new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 section = Vector3.Lerp(template.rim[side],
                    radial * radius, loft);
                vertices[vertex] = Vector3.up * behind + section
                    - Vector3.up * (NeckOverlapU * (1f - loft));
                normals[vertex] = Vector3.Slerp(template.rimNormals[side],
                    radial, loft).normalized;
                uv[vertex] = new Vector2(side / (float)BodySides, along);
                neckUv[vertex] = template.rimUV[side];
                colors[vertex] = BODY_COLOR;
                weights[vertex] = WeightsFor(ring);
                if (ring + 1 < BodyRings && side < BodySides) {
                    int next = vertex + BodyStride;
                    indices.AddRange(new[] { vertex, next, vertex + 1,
                        vertex + 1, next, next + 1 });
                }
            }
        }
        vertices[neckCap] = Vector3.down * NeckOverlapU;
        normals[neckCap] = Vector3.down;
        uv[neckCap] = new Vector2(.5f, 0f);
        neckUv[neckCap] = template.rimUV[0];
        colors[neckCap] = BODY_COLOR;
        weights[neckCap] = WeightsFor(0);
        for (int side = 0; side < BodySides; side++) {
            indices.AddRange(new[] { neckCap, side, side + 1 });
        }
        vertices[tip] =
            Vector3.up * ((BodyRings - 1) * step + TIP_LENGTH_U);
        normals[tip] = Vector3.up;
        uv[tip] = new Vector2(.5f, 1f);
        colors[tip] = BODY_COLOR;
        weights[tip] = WeightsFor(BodyRings - 1);
        for (int side = 0; side < BodySides; side++) {
            indices.AddRange(new[] { (BodyRings - 1) * BodyStride + side, tip,
                (BodyRings - 1) * BodyStride + side + 1 });
        }

        Matrix4x4[] bindposes = new Matrix4x4[BodyRings];
        for (int ring = 0; ring < BodyRings; ring++) {
            bindposes[ring] =
                Matrix4x4.Translate(Vector3.down * (ring * step));
        }
        return new Mesh {
            name = "FallingCobraBody", vertices = vertices, normals = normals,
            uv = uv, uv2 = neckUv, colors = colors,
            triangles = indices.ToArray(), boneWeights = weights,
            bindposes = bindposes,
        };
    }

    // a ring's radius: the body radius, tapered to the tail, narrowed at the
    // waist of the neck; the runtime used these same two curves
    private static float RingRadius(FallingCobraConfig config, float along) {
        float taper = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(.55f, 1f, along));
        float neckAlong = along * config.SmoothBodyLengthU;
        float neckWaist = Mathf.Lerp(config.NeckWaistScale, 1f,
            Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.35f, 1.25f, neckAlong)));
        return Mathf.Max(MIN_RING_RADIUS_U,
            config.SmoothBodyRadiusU * taper * neckWaist);
    }

    // a ring's vertices follow its own bone; the second influence is the
    // next ring's bone, at BLEND_WEIGHT, 0 keeping the tube exactly rigid per
    // ring
    private static BoneWeight WeightsFor(int ring) {
        int next = Mathf.Min(ring + 1, BodyRings - 1);
        return new BoneWeight {
            boneIndex0 = ring, weight0 = 1f - BLEND_WEIGHT,
            boneIndex1 = next, weight1 = BLEND_WEIGHT,
        };
    }

    // keeps the asset's identity when it exists, so references to it survive
    private static Mesh SaveMesh(Mesh built) {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
        if (existing == null) {
            AssetDatabase.CreateAsset(built, MESH_PATH);
            return built;
        }
        EditorUtility.CopySerialized(built, existing);
        Object.DestroyImmediate(built);
        return existing;
    }

    private static GameObject BuildPrefab(FallingCobraConfig config,
        Mesh body, Material material) {
        float step = config.SmoothBodyLengthU / (BodyRings - 1);
        int layer = LayerMask.NameToLayer(COBRA_LAYER);
        GameObject root = new GameObject("FallingCobraBody");
        try {
            Transform[] bones = new Transform[BodyRings];
            for (int ring = 0; ring < BodyRings; ring++) {
                Transform bone =
                    new GameObject("Bone_" + ring.ToString("D2")).transform;
                bone.SetParent(root.transform, false);
                bone.localPosition = Vector3.up * (ring * step);
                bones[ring] = bone;
            }
            SkinnedMeshRenderer skinned =
                root.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = body;
            skinned.sharedMaterial = material;
            skinned.bones = bones;
            skinned.rootBone = bones[0];
            // the tube reaches its whole length from the root bone, any way
            // it bends
            skinned.localBounds = new Bounds(Vector3.zero,
                Vector3.one * (config.SmoothBodyLengthU * 2f));
            skinned.updateWhenOffscreen = false;
            skinned.skinnedMotionVectors = false;
            if (layer >= 0) {
                foreach (Transform part in
                    root.GetComponentsInChildren<Transform>(true)) {
                    part.gameObject.layer = layer;
                }
            }
            return PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        } finally {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureFolder(string folder) {
        if (AssetDatabase.IsValidFolder(folder)) {
            return;
        }
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    // constants  ##############################################################
    private const string CONFIG_PATH =
        "Assets/_Monkobra/Settings/MobFallingCobra/FallingCobraConfig.asset";
    private const string MESH_FOLDER =
        "Assets/_Monkobra/Meshes/MobFallingCobra";
    private const string MESH_PATH = MESH_FOLDER + "/FallingCobraBody.asset";
    private const string PREFAB_PATH =
        "Assets/_Monkobra/Prefabs/MobFallingCobra/FallingCobraBody.prefab";
    private const string COBRA_LAYER = "FallingCobra";
    // length the tail cap reaches past the last ring; u
    private const float TIP_LENGTH_U = .005f;
    // share of a ring's vertices that follows the next ring's bone
    private const float BLEND_WEIGHT = 0f;
    private static readonly Color BODY_COLOR = new Color(1f, 0f, 0f, 1f);
}
