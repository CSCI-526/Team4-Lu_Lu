using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// On the branch prefab root: draws the branch as a stylized log instead of
/// its box, thick and flared where it leaves the trunk, tapering to its tip,
/// slightly arched, with a twig, leaf rosettes on top and moss strands
/// hanging off its sides; and grows a fruit stalk from its underside down to
/// the fruit in place of the bare stem capsule. Drawn with the
/// <c>Monkobra/ToonBark</c> shader.
/// <para>
/// Pure visuals: the box and the stem keep their colliders, tags and
/// transforms, and the fruit stays exactly where <see cref="BranchController"/>
/// hangs it, so hits, grabs and placement work as before; only the box's
/// and the stem's renderers are turned off.
/// </para>
/// <para>
/// A few log looks are built once and shared by every branch; each stalk is
/// built for its own branch, since the fruit hangs at a random depth.
/// </para>
/// </summary>
public class StylizedBranchArt: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("the branch box child: its transform shapes the log, its renderer "
        + "is hidden")]
    private Transform branchBox;

    [SerializeField]
    [Tooltip(
        "the fruit stem capsule: the stalk runs to its lower tip, where the "
            + "fruit hangs; its renderer is hidden"
    )]
    private Transform fruitStem;

    [SerializeField]
    [Tooltip("ToonBark material with vertex colours on")]
    private Material artMaterial;

    [Header("Shape")]
    [SerializeField]
    [Range(0.5f, 2.5f)]
    [Tooltip("log thickness at its inner end, inside the trunk, as a share of "
        + "the box's")]
    private float baseThickness = 1.45f;

    [SerializeField]
    [Range(0.2f, 1.5f)]
    [Tooltip("log thickness at the tip, as a share of the box's")]
    private float tipThickness = 0.6f;

    [SerializeField]
    [Range(0f, 1.5f)]
    [Tooltip("extra swelling where the log leaves the trunk, as a share of the "
        + "box's")]
    private float collarFlare = 0.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("how far the log arches up in its middle; u")]
    private float archU = 0.1f;

    [SerializeField]
    [Range(1, 8)]
    [Tooltip("different looks built and shared by all branches")]
    private int looks = 4;

    [Header("Fruit Stalk")]
    [SerializeField]
    [Min(0.01f)]
    [Tooltip("stalk radius where it leaves the branch; u")]
    private float stalkRadiusU = 0.075f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (branchBox == null || fruitStem == null || artMaterial == null) {
            Debug.LogWarning(
                "StylizedBranchArt:\tmust assign Inspector Fields: branchBox, "
                    + "fruitStem, artMaterial",
                this
            );
            return;
        }

        ReadBox();
        int look = Random.Range(0, looks);
        MakeChild("BranchArt", SharedMesh(look));

        HideRenderer(branchBox);
        HideRenderer(fruitStem);
        isReady = true;
    }

    // the spawner calls BranchController.Setup right after spawning, which
    // sizes the stem and hangs the fruit; by now both are final
    private void Start() {
        if (!isReady || !fruitStem.gameObject.activeInHierarchy) {
            return;
        }
        StylizedMeshBuilder builder = new StylizedMeshBuilder();
        BuildStalk(builder, new System.Random(GetInstanceID()));
        stalkMesh = new Mesh { name = "FruitStalk" };
        builder.Fill(stalkMesh);
        MakeChild("FruitStalk", stalkMesh);
    }

    private void OnDestroy() {
        if (stalkMesh != null) {
            Destroy(stalkMesh);
        }
    }

    // private members  ########################################################
    private const int LENGTH_RINGS = 14;
    private const int SIDES = 10;

    // where the collar swell fades out, as a share of the length
    private const float COLLAR_END = 0.32f;

    // shared by every branch; the key holds the box's shape & the look
    private static readonly Dictionary<string, Mesh> cache =
        new Dictionary<string, Mesh>();

    private bool isReady;
    private Mesh stalkMesh;

    // the box, in this root's frame
    private Vector3 centre;
    private Vector3 outward;
    private Vector3 up;
    private Vector3 side;
    private float lengthU;
    private float halfHeightU;
    private float halfDepthU;

    private void ReadBox() {
        centre = branchBox.localPosition;
        Vector3 lengthAxis = branchBox.localRotation * Vector3.right;
        // the outer end is the one pointing away from the trunk, along +z
        outward = Vector3.Dot(lengthAxis, Vector3.forward) >= 0f
            ? lengthAxis : -lengthAxis;
        up = branchBox.localRotation * Vector3.up;
        if (Vector3.Dot(up, Vector3.up) < 0f) {
            up = -up;
        }
        side = Vector3.Cross(outward, up).normalized;
        lengthU = branchBox.localScale.x;
        halfHeightU = branchBox.localScale.y * 0.5f;
        halfDepthU = branchBox.localScale.z * 0.5f;
    }

    private void MakeChild(string childName, Mesh mesh) {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = artMaterial;
    }

    private static void HideRenderer(Transform part) {
        MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
        if (partRenderer != null) {
            partRenderer.enabled = false;
        }
    }

    // t 0 at the inner end, 1 at the tip
    private Vector3 AxisPoint(float t) {
        return centre + outward * ((t - 0.5f) * lengthU)
            + up * (archU * Mathf.Sin(Mathf.PI * t));
    }

    // thick at the trunk, a swollen collar, thin at the tip
    private float Thickness(float t) {
        float collar = Mathf.Clamp01(1f - t / COLLAR_END);
        return Mathf.Lerp(baseThickness, tipThickness, Mathf.Pow(t, 0.8f))
            + collarFlare * collar * collar;
    }

    private Mesh SharedMesh(int look) {
        string key =
            $"{centre}|{outward}|{branchBox.localScale}|{baseThickness}|"
            + $"{tipThickness}|{collarFlare}|{archU}|{look}";
        // a scene unload may have freed it
        if (cache.TryGetValue(key, out Mesh cached) && cached != null) {
            return cached;
        }
        Mesh mesh = new Mesh { name = "BranchArt_" + look };
        BuildLog(new System.Random(look * 7919 + 17)).Fill(mesh);
        cache[key] = mesh;
        return mesh;
    }

    private StylizedMeshBuilder BuildLog(System.Random random) {
        // Log  ----------------------------------------------------------------
        List<Vector3> centres = new List<Vector3>(LENGTH_RINGS);
        List<Vector3> ups = new List<Vector3>(LENGTH_RINGS);
        List<float> radiiUp = new List<float>(LENGTH_RINGS);
        List<float> radiiSide = new List<float>(LENGTH_RINGS);
        float wobblePhase = (float)random.NextDouble() * Mathf.PI * 2f;
        for (int r = 0; r < LENGTH_RINGS; r++) {
            float t = (float)r / (LENGTH_RINGS - 1);
            float wobble = 0.04f * Mathf.Sin(wobblePhase + t * 6f) * t;
            float thickness = Thickness(t)
                * (0.95f + 0.1f * (float)random.NextDouble());
            centres.Add(AxisPoint(t) + side * wobble);
            ups.Add(up);
            radiiUp.Add(halfHeightU * thickness);
            radiiSide.Add(halfDepthU * thickness * 1.15f);
        }
        StylizedMeshBuilder builder = new StylizedMeshBuilder();
        builder.AddTube(
            centres,
            ups,
            radiiUp,
            radiiSide,
            SIDES,
            StylizedMeshBuilder.BARK,
            true
        );

        // Twig  ---------------------------------------------------------------
        int twigRing = Mathf.RoundToInt(
            (0.6f + 0.12f * (float)random.NextDouble()) * (LENGTH_RINGS - 1)
        );
        float twigSide = random.NextDouble() < 0.5 ? -1f : 1f;
        Vector3 twigBase = centres[twigRing] + up * (radiiUp[twigRing] * 0.6f);
        Vector3 twigDirection =
            (outward * 0.7f + up * 0.9f + side * (0.35f * twigSide)).normalized;
        float twigLengthU = 0.45f + 0.2f * (float)random.NextDouble();
        builder.AddTaperedTube(
            twigBase, twigDirection, outward * 0.06f, twigLengthU,
            radiiUp[twigRing] * 0.35f, 0.025f, 4, 6,
            new Color(0f, 0f, 0f, 0.5f)
        );
        builder.AddRosette(
            twigBase + twigDirection * twigLengthU,
            twigDirection,
            3,
            0.28f,
            random
        );

        // Leaves  -------------------------------------------------------------
        int nearTip = LENGTH_RINGS - 2;
        builder.AddRosette(
            centres[nearTip] + up * (radiiUp[nearTip] * 0.85f),
            (up + outward * 0.2f).normalized,
            5,
            0.4f,
            random
        );
        if (random.NextDouble() < 0.6) {
            int middle = 5 + random.Next(3);
            builder.AddRosette(
                centres[middle] + up * (radiiUp[middle] * 0.85f),
                (up - side * 0.3f * twigSide).normalized,
                4,
                0.32f,
                random
            );
        }

        // Moss Strands  -------------------------------------------------------
        // along the sides, clear of the fruit stalk under the tip
        int strands = 5 + random.Next(4);
        for (int i = 0; i < strands; i++) {
            float t = 0.22f + 0.55f * (float)random.NextDouble();
            int ring = Mathf.RoundToInt(t * (LENGTH_RINGS - 1));
            float edge = random.NextDouble() < 0.5 ? -1f : 1f;
            Vector3 top = centres[ring] - up * (radiiUp[ring] * 0.55f)
                + side * (radiiSide[ring] * 0.8f * edge);
            builder.AddMossStrand(
                top,
                side * (0.05f * edge),
                0.22f + 0.4f * (float)random.NextDouble(),
                0.05f,
                random
            );
        }

        return builder;
    }

    // a green stalk from the log's underside, arcing down to the fruit, two
    // leaves where it sprouts and a little crown of sepals over the fruit
    private void BuildStalk(StylizedMeshBuilder builder, System.Random random) {
        Vector3 tip = transform.InverseTransformPoint(
            fruitStem.TransformPoint(Vector3.down)
        );

        // sprout from inside the log, right above where the fruit hangs:
        // matched by level distance only, the stem's own top may poke up
        // through the log when it is stretched long
        Vector3 levelOutward = Vector3.ProjectOnPlane(outward, Vector3.up);
        float levelPerU = Mathf.Max(levelOutward.sqrMagnitude, 0.0001f);
        float t = Mathf.Clamp01(
            Vector3.Dot(
                Vector3.ProjectOnPlane(tip - centre, Vector3.up), levelOutward
            )
                / (levelPerU * lengthU)
                + 0.5f
        );
        Vector3 axis = AxisPoint(t);
        float radiusUpU = halfHeightU * Thickness(t);
        Vector3 sprout = axis - Vector3.up * (radiusUpU * 0.5f);
        Vector3 underside = axis - Vector3.up * (radiusUpU * 0.9f);
        // the fruit's top tucks a little into the stalk's end
        Vector3 end = tip + Vector3.up * 0.04f;

        // a gentle arc: out from under the log first, then down to the fruit
        const int RINGS = 10;
        float leanSide = random.NextDouble() < 0.5 ? -1f : 1f;
        Vector3 bend = underside + outward * 0.18f + side * (0.08f * leanSide);
        List<Vector3> centres = new List<Vector3>(RINGS);
        List<Vector3> ups = new List<Vector3>(RINGS);
        List<float> radii = new List<float>(RINGS);
        for (int r = 0; r < RINGS; r++) {
            float s = (float)r / (RINGS - 1);
            // quadratic bezier sprout, bend, end
            Vector3 point =
                (1 - s) * (1 - s) * sprout + 2 * (1 - s) * s * bend
                + s * s * end;
            centres.Add(point);
            ups.Add(outward);
            // tapers, then swells into a little node over the fruit
            float node = Mathf.Exp(-Mathf.Pow((s - 0.96f) / 0.05f, 2f)) * 0.35f;
            radii.Add(stalkRadiusU * (Mathf.Lerp(1f, 0.65f, s) + node));
        }
        builder.AddTube(
            centres, ups, radii, radii, 7, StylizedMeshBuilder.VINE, false
        );

        // 2 leaves where the stalk leaves the log
        for (int i = 0; i < 2; i++) {
            float leafSide = i == 0 ? leanSide : -leanSide;
            Vector3 direction = (
                side * leafSide + Vector3.down * 0.35f + outward * 0.25f
            ).normalized;
            builder.AddLeaf(
                underside + side * (stalkRadiusU * leafSide),
                direction,
                Vector3.up,
                0.3f + 0.08f * (float)random.NextDouble(),
                0.13f,
                -0.12f,
                (float)random.NextDouble()
            );
        }

        // sepals cupping the fruit's top
        Vector3 down = (end - centres[RINGS - 2]).normalized;
        Vector3 radial = Vector3.ProjectOnPlane(outward, down).normalized;
        int sepals = 5;
        float start = (float)random.NextDouble() * Mathf.PI * 2f;
        for (int i = 0; i < sepals; i++) {
            Vector3 spoke = Quaternion.AngleAxis(
                start * Mathf.Rad2Deg + i * 360f / sepals, down
            ) * radial;
            builder.AddLeaf(
                end - down * 0.02f,
                (spoke * 0.85f + down * 0.5f).normalized,
                spoke,
                0.16f,
                0.09f,
                0.25f,
                0.3f
            );
        }
    }
}
