using UnityEngine;


/// <summary>
/// On a trunk segment (the cylinder): dresses it in cartoon foliage, moss
/// strands hanging off the bottom edges of the moss patches its
/// <c>Monkobra/ToonBark</c> material paints, and leaf rosettes growing out
/// of the moss.
/// <para>
/// The patches come from world position, so when a pooled segment is moved
/// up the trunk the strands are rebuilt to hang off its new patches.
/// </para>
/// <para>
/// Pure visuals: one mesh with no collider, on a child this script makes,
/// so the segment's collider, height and branch placement are untouched.
/// </para>
/// </summary>
public class TrunkDecor: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("ToonBark material with vertex colours on, for strands & leaves")]
    private Material decorMaterial;

    [SerializeField]
    [Tooltip(
        "the segment's own renderer, its ToonBark material says where moss "
            + "grows"
    )]
    private MeshRenderer trunkRenderer;

    [Header("Moss Strands")]
    [SerializeField]
    [Min(0.1f)]
    [Tooltip("spacing of the spots tested for a patch's bottom edge; u")]
    private float strandSpacingU = 0.35f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("share of the bottom edge spots that grow a strand")]
    private float strandChance = 0.45f;

    [SerializeField]
    [Tooltip("strand length, min / max; u")]
    private Vector2 strandLengthU = new Vector2(0.3f, 0.85f);

    [SerializeField]
    [Min(1)]
    [Tooltip("most strands on one segment, keeping its mesh light")]
    private int maxStrands = 70;

    [Header("Leaves")]
    [SerializeField]
    [Tooltip("leaf rosettes on one segment, min / max")]
    private Vector2Int rosettes = new Vector2Int(3, 6);

    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "rosette leaf length; u, kept short so leaves hug the bark and never "
            + "hide the monkey"
    )]
    private float leafLengthU = 0.45f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (decorMaterial == null || trunkRenderer == null) {
            Debug.LogWarning(
                "TrunkDecor:\tmust assign Inspector Fields: decorMaterial, "
                    + "trunkRenderer",
                this
            );
            enabled = false;
            return;
        }

        GameObject holder = new GameObject("TrunkDecor");
        decor = holder.transform;
        decor.SetParent(transform, false);
        mesh = new Mesh { name = "TrunkDecor" };
        holder.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer decorRenderer = holder.AddComponent<MeshRenderer>();
        decorRenderer.sharedMaterial = decorMaterial;
    }

    private void OnDestroy() {
        if (mesh != null) {
            Destroy(mesh);
        }
    }

    // a pooled segment moves in its owner's Restart; rebuild once it lands
    private void LateUpdate() {
        if (
            !hasBuilt
                || (transform.position - builtPosition).sqrMagnitude > 0.0001f
        ) {
            Rebuild();
        }
    }

    // private members  ########################################################
    private Transform decor;
    private Mesh mesh;
    private bool hasBuilt;
    private Vector3 builtPosition;
    private readonly StylizedMeshBuilder builder = new StylizedMeshBuilder();

    private void Rebuild() {
        hasBuilt = true;
        builtPosition = transform.position;

        // the decor child undoes the segment's stretch, so it builds in u
        Vector3 scale = transform.lossyScale;
        decor.localScale =
            new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        decor.localRotation = Quaternion.identity;
        decor.localPosition = Vector3.zero;

        // a unit cylinder: radius 0.5, 2 tall
        float radiusU = 0.5f * scale.x;
        float halfHeightU = scale.y;
        StylizedBarkNoise.MossSettings moss =
            StylizedBarkNoise.ReadMoss(trunkRenderer.sharedMaterial);

        // seeded by place, so a segment looks the same each time it lands there
        int seed = Mathf.RoundToInt(builtPosition.y * 10f) * 73856093
            ^ Mathf.RoundToInt(builtPosition.x * 10f) * 19349663;
        System.Random random = new System.Random(seed);

        builder.Clear();
        AddStrands(radiusU, halfHeightU, moss, random);
        AddRosettes(radiusU, halfHeightU, moss, random);
        builder.Fill(mesh);
    }

    // a strand hangs where moss above meets bare bark below
    private void AddStrands(
        float radiusU,
        float halfHeightU,
        StylizedBarkNoise.MossSettings moss,
        System.Random random
    ) {
        if (moss.coverage <= 0.001f) {
            return;
        }
        int around = Mathf.Max(
            8, Mathf.RoundToInt(2f * Mathf.PI * radiusU / strandSpacingU)
        );
        int rows =
            Mathf.Max(2, Mathf.RoundToInt(2f * halfHeightU / strandSpacingU));
        int strands = 0;
        for (int row = 1; row < rows && strands < maxStrands; row++) {
            float y = -halfHeightU + row * (2f * halfHeightU / rows);
            for (int i = 0; i < around && strands < maxStrands; i++) {
                float angle = (i + 0.5f * (row % 2)) * Mathf.PI * 2f / around;
                Vector3 outward =
                    new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 local = outward * radiusU + Vector3.up * y;
                Vector3 world = decor.TransformPoint(local);
                bool isMossy = StylizedBarkNoise.IsMossy(world, moss);
                bool isBareBelow = !StylizedBarkNoise.IsMossy(
                    world + Vector3.down * strandSpacingU,
                    moss
                );
                if (
                    !isMossy
                        || !isBareBelow
                        || random.NextDouble() > strandChance
                ) {
                    continue;
                }
                float lengthU = Mathf.Lerp(
                    strandLengthU.x, strandLengthU.y, (float)random.NextDouble()
                );
                // keep the strand clear of the segment's ends, the joins
                if (y - lengthU < -halfHeightU + 0.1f) {
                    continue;
                }
                builder.AddMossStrand(
                    local + outward * 0.02f,
                    outward * 0.07f,
                    lengthU,
                    0.055f,
                    random
                );
                strands++;
            }
        }
    }

    // rosettes grow out of the moss, never on bare bark
    private void AddRosettes(
        float radiusU,
        float halfHeightU,
        StylizedBarkNoise.MossSettings moss,
        System.Random random
    ) {
        int wanted = random.Next(rosettes.x, rosettes.y + 1);
        int placed = 0;
        for (
            int attempt = 0; attempt < wanted * 8 && placed < wanted; attempt++
        ) {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float y = Mathf.Lerp(
                -halfHeightU + 0.6f,
                halfHeightU - 0.6f,
                (float)random.NextDouble()
            );
            Vector3 outward =
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 local = outward * (radiusU - 0.04f) + Vector3.up * y;
            if (moss.coverage > 0.001f
                && !StylizedBarkNoise.IsMossy(
                    decor.TransformPoint(local), moss
                )) {
                continue;
            }
            builder.AddRosette(
                local, outward, 4 + random.Next(3), leafLengthU, random
            );
            placed++;
        }
    }
}
