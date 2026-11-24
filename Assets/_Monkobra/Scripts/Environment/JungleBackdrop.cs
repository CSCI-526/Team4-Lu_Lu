using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The Amazon backdrop round the tree, changing with how high the monkey has
/// climbed: at the start round-crowned trees stand all round, their tops
/// just below the monkey, over a forest running out to layered hills; as it
/// climbs the forest drops away into a carpet of crowns, the hills sink
/// toward the horizon, clouds gather and the Amazon river shows, fully from
/// <see cref="riverFullClimbedU"/>; from about 500 m small clouds float past
/// and flocks of birds circle the tree.
/// <para>
/// On the <c>JungleBackdrop</c> prefab under <c>Envs</c>, centred on the
/// trunk's axis, any height. The far world (sky, clouds, hills,
/// canopy, river) is painted by the BackdropSky shader on a dome round the
/// camera; only the ring of near trees is a mesh, built once
/// in <c>Start</c>. Each frame only sets a few shader globals from the
/// camera's height.
/// </para>
/// <para>
/// Pure scenery: no colliders, no shadows, never touches the scene's light
/// or ambient. Nothing stands within <see cref="clearRadiusU"/> of the trunk
/// but a few low trees under the start, kept <see cref="underClearU"/> from
/// the trunk & <see cref="underTopBelowStartU"/> under the start, so the
/// monkey never looks down onto bare ground, yet nothing ever comes between
/// the camera and the monkey or the cobra on the trunk.
/// </para>
/// </summary>
public class JungleBackdrop: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("Monkobra/BackdropSky material: sky, hills, canopy, river")]
    private Material skyMaterial;

    [SerializeField]
    [Tooltip("Monkobra/BackdropSurface material: the near trees")]
    private Material forestMaterial;

    [SerializeField]
    [Tooltip("Monkobra/BackdropClouds material: the small puffy clouds up high")]
    private Material cloudMaterial;

    [SerializeField]
    [Tooltip("Monkobra/BackdropBirds material: the flocks up high")]
    private Material birdMaterial;

    [Header("Heights")]
    [SerializeField]
    [Tooltip("how far below the monkey's start the forest canopy's top lies; u")]
    private float canopyBelowStartU = 18f;

    [SerializeField]
    [Min(5f)]
    [Tooltip("the forest floor lies this far below the canopy's top; u")]
    private float forestHeightU = 50f;

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip(
        "the forest falls away at this fraction of the climb: the backdrop "
            + "rises with the monkey for the rest, so the view changes slowly"
    )]
    private float apparentClimbScale = 0.35f;

    [SerializeField]
    [Tooltip("climbed height the river starts to show from; m")]
    private float riverStartClimbedU = 250f;

    [SerializeField]
    [Tooltip("climbed height the river shows fully from; m")]
    private float riverFullClimbedU = 700f;

    [SerializeField]
    [Tooltip("climbed height the sky & haze reach their high look at; m")]
    private float highClimbedU = 1000f;

    [SerializeField]
    [Tooltip("climbed heights over which clouds gather, from / to; m")]
    private Vector2 cloudClimbedU = new Vector2(300f, 1100f);

    [SerializeField]
    [Tooltip("the start height, used only if no RampedDifficultyManager is found; world u")]
    private float fallbackStartY = 0f;

    [Header("Layout")]
    [SerializeField]
    [Min(40f)]
    [Tooltip(
        "nothing of the backdrop comes nearer the trunk's axis than this, "
            + "clear of the camera; u"
    )]
    private float clearRadiusU = 75f;

    [SerializeField]
    [Min(0)]
    [Tooltip("low trees in the clearing, under the monkey's start")]
    private int underTrees = 60;

    [SerializeField]
    [Min(22f)]
    [Tooltip(
        "no part of a low tree comes nearer the trunk's axis than this, "
            + "clear of the camera's orbit; u"
    )]
    private float underClearU = 26f;

    [SerializeField]
    [Min(8f)]
    [Tooltip("the low trees' crowns stay at least this far under the start; u")]
    private float underTopBelowStartU = 14f;

    [SerializeField]
    [Tooltip("the ring of real trees reaches this far out, the painted forest beyond; u")]
    private float nearRadiusU = 420f;

    [SerializeField]
    [Tooltip(
        "the painted canopy starts this far out, hiding the near trees "
            + "beyond, which sink into it & take on its colour toward nearRadiusU; u"
    )]
    private float paintedFromU = 220f;

    [SerializeField]
    [Min(0)]
    [Tooltip("round-crowned trees in the near ring")]
    private int nearTrees = 560;

    [SerializeField]
    [Tooltip("near trees' crown radius, smallest / largest; u")]
    private Vector2 crownRadiusU = new Vector2(10f, 17f);

    [SerializeField]
    [Tooltip("sky dome radius, inside the camera's far clip; u")]
    private float skyRadiusU = 4600f;

    [Header("Clouds & Birds")]
    [SerializeField]
    [Tooltip("climbed heights the clouds & birds fade in over, from / to; m")]
    private Vector2 flyersShowClimbedU = new Vector2(450f, 580f);

    [SerializeField]
    [Min(0)]
    [Tooltip("small puffy clouds")]
    private int clouds = 70;

    [SerializeField]
    [Tooltip("climbed heights the clouds float at, lowest / highest; m")]
    private Vector2 cloudBandClimbedU = new Vector2(500f, 1700f);

    [SerializeField]
    [Tooltip("how far out the clouds float, nearest / farthest; u")]
    private Vector2 cloudReachU = new Vector2(150f, 900f);

    [SerializeField]
    [Tooltip("cloud width, smallest / largest; u")]
    private Vector2 cloudWidthU = new Vector2(30f, 80f);

    [SerializeField]
    [Min(0)]
    [Tooltip("flocks of birds, each circling the tree its own way")]
    private int flocks = 4;

    [SerializeField]
    [Min(1)]
    private int birdsPerFlock = 5;

    [SerializeField]
    [Tooltip("how far out the flocks circle, nearest / farthest; u")]
    private Vector2 flockReachU = new Vector2(160f, 600f);

    [SerializeField]
    [Tooltip("flocks' height over the camera, lowest / highest; u")]
    private Vector2 flockHeightU = new Vector2(-50f, 30f);

    [SerializeField]
    [Tooltip("bird wing span; u")]
    private float birdSpanU = 3f;

    [Header("Look")]
    [SerializeField]
    private Color lowZenith = new Color(0.47f, 0.67f, 0.88f);

    [SerializeField]
    private Color highZenith = new Color(0.36f, 0.58f, 0.88f);

    [SerializeField]
    private Color lowHorizon = new Color(0.80f, 0.87f, 0.91f);

    [SerializeField]
    private Color highHorizon = new Color(0.85f, 0.90f, 0.94f);

    [SerializeField]
    private Color hazeColor = new Color(0.74f, 0.83f, 0.88f);

    [SerializeField]
    [Tooltip("haze per u near the ground & up high: thinner higher, the view opens out")]
    private Vector2 hazeDensity = new Vector2(1f / 6000f, 1f / 10000f);

    [SerializeField]
    [Tooltip("cloud cover low / high, 0 to 1")]
    private Vector2 cloudAmount = new Vector2(0.25f, 0.6f);

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (skyMaterial == null || forestMaterial == null || cloudMaterial == null
            || birdMaterial == null) {
            Debug.LogWarning(
                "JungleBackdrop:\tmust assign Inspector Fields: skyMaterial, "
                    + "forestMaterial, cloudMaterial, birdMaterial",
                this
            );
            enabled = false;
            return;
        }
        isReady = true;
    }

    // the start height is known once the difficulty manager woke
    private void Start() {
        if (!isReady) {
            return;
        }
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        startY = ramp != null ? ramp.PlayerGameStartYPosition : fallbackStartY;
        Build();
    }

    private void OnDestroy() {
        foreach (Mesh mesh in meshes) {
            if (mesh != null) {
                Destroy(mesh);
            }
        }
    }

    private void LateUpdate() {
        if (!isReady || sky == null) {
            return;
        }
        if (viewer == null) {
            viewer = Camera.main;
            if (viewer == null) {
                return;
            }
        }
        Vector3 eye = viewer.transform.position;
        sky.position = eye;
        float climbedU = eye.y - startY;
        // the forest & its floor rise with the monkey for all but
        // apparentClimbScale of the climb
        float lift = Mathf.Max(climbedU, 0f) * (1f - apparentClimbScale);
        trees.localPosition = Vector3.up * lift;
        Shader.SetGlobalVector(LAYOUT_ID, new Vector4(canopyY + lift, nearRadiusU, floorY + lift, paintedFromU));
        Apply(climbedU);
    }

    // private members  ########################################################
    private static readonly int ZENITH_ID = Shader.PropertyToID("_BackdropSkyZenith");
    private static readonly int HORIZON_ID = Shader.PropertyToID("_BackdropSkyHorizon");
    private static readonly int HAZE_COLOR_ID = Shader.PropertyToID("_BackdropHazeColor");
    private static readonly int HAZE_DENSITY_ID = Shader.PropertyToID("_BackdropHazeDensity");
    private static readonly int CLOUD_ID = Shader.PropertyToID("_BackdropCloudAmount");
    private static readonly int RIVER_ID = Shader.PropertyToID("_BackdropRiverReveal");
    private static readonly int SUN_ID = Shader.PropertyToID("_BackdropSunDir");
    private static readonly int LAYOUT_ID = Shader.PropertyToID("_BackdropLayout");
    private static readonly int CENTRE_ID = Shader.PropertyToID("_BackdropCentre");
    private static readonly int PUFFS_ID = Shader.PropertyToID("_BackdropPuffs");
    private static readonly int BIRDS_ID = Shader.PropertyToID("_BackdropBirds");

    // crown greens, matched to Backdrop_CanopyDetail, so near crowns melt
    // into the painted forest
    private static readonly Color[] CROWN_COLORS = {
        new Color(0.34f, 0.52f, 0.27f), new Color(0.38f, 0.56f, 0.28f),
        new Color(0.31f, 0.49f, 0.30f), new Color(0.43f, 0.58f, 0.29f),
        new Color(0.33f, 0.50f, 0.34f), new Color(0.46f, 0.60f, 0.31f),
    };
    private static readonly Color BARK = new Color(0.40f, 0.34f, 0.28f);
    private const int TREE_WEDGES = 8;

    private bool isReady;
    private float startY;
    private Transform sky;
    private Transform trees;
    private float canopyY;
    private float floorY;
    private Camera viewer;
    private Light sun;
    private bool hasLookedForSun;
    private readonly List<Mesh> meshes = new List<Mesh>();

    // Build  ------------------------------------------------------------------
    private void Build() {
        System.Random random = new System.Random(526);
        // both from the start height, the floor never from this transform's
        // own height: the start lies wherever the scene puts the monkey
        canopyY = startY - canopyBelowStartU;
        floorY = canopyY - forestHeightU;
        float canopyLocal = canopyY - transform.position.y;
        float floorLocal = floorY - transform.position.y;
        Shader.SetGlobalVector(LAYOUT_ID, new Vector4(canopyY, nearRadiusU, floorY, paintedFromU));
        Shader.SetGlobalVector(CENTRE_ID, transform.position);

        sky = Layer("Sky", BackdropMeshes.Dome(skyRadiusU), skyMaterial).transform;

        // the ring in wedges, each its own mesh, so Unity culls the ones
        // out of view, most of all the ones behind the camera
        BackdropMeshes[] wedges = new BackdropMeshes[TREE_WEDGES];
        for (int i = 0; i < TREE_WEDGES; i++) {
            wedges[i] = new BackdropMeshes();
        }
        List<Vector3> placed = new List<Vector3>(); // x, z, footprint radius
        float startLocal = canopyLocal + canopyBelowStartU;
        // the ring first, then the clearing's low trees between its gaps
        AddTrees(
            wedges, placed, random, nearTrees, clearRadiusU, nearRadiusU,
            startLocal - 3f, canopyLocal, floorLocal
        );
        AddTrees(
            wedges, placed, random, underTrees, underClearU, clearRadiusU + 10f,
            startLocal - underTopBelowStartU, canopyLocal, floorLocal
        );
        trees = new GameObject("Trees").transform;
        trees.SetParent(transform, false);
        for (int i = 0; i < TREE_WEDGES; i++) {
            Layer("Trees" + i, wedges[i].Build("BackdropTrees" + i), forestMaterial)
                .transform.SetParent(trees, false);
        }

        AddClouds(random);
        AddBirds(random);
    }

    // puffs scattered through a band high over the start, real heights, so
    // the monkey climbs up past them
    private void AddClouds(System.Random random) {
        List<Vector4> puffs = new(clouds);
        for (int i = 0; i < clouds; i++) {
            float distance = Mathf.Lerp(cloudReachU.x, cloudReachU.y, (float)random.NextDouble());
            float climbed = Mathf.Lerp(cloudBandClimbedU.x, cloudBandClimbedU.y, (float)random.NextDouble());
            Vector3 centre = Around(random, distance) + Vector3.up * (startY + climbed - transform.position.y);
            puffs.Add(new Vector4(
                centre.x, centre.y, centre.z,
                Mathf.Lerp(cloudWidthU.x, cloudWidthU.y, (float)random.NextDouble())
            ));
        }
        Layer("Clouds", BackdropMeshes.Clouds(puffs, random), cloudMaterial);
    }

    // flocks circling at about the camera's height, a loose V each
    private void AddBirds(System.Random random) {
        List<Vector4> paths = new();
        List<Vector2> flapSpans = new();
        for (int f = 0; f < flocks; f++) {
            float radius = Mathf.Lerp(flockReachU.x, flockReachU.y, (float)random.NextDouble());
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float height = Mathf.Lerp(flockHeightU.x, flockHeightU.y, (float)random.NextDouble());
            // about 7 u/s, either way round
            float speed = 7f / radius * (random.Next(2) == 0 ? -1f : 1f);
            for (int b = 0; b < birdsPerFlock; b++) {
                // the leader first, the rest trailing alternately out to the sides
                int rank = (b + 1) / 2;
                float side = b % 2 == 0 ? 1f : -1f;
                paths.Add(new Vector4(
                    radius + side * rank * 4f,
                    angle - Mathf.Sign(speed) * rank * 5f / radius,
                    height + rank * 0.8f,
                    speed
                ));
                flapSpans.Add(new Vector2(
                    (float)random.NextDouble() * 6.28f,
                    birdSpanU * Mathf.Lerp(0.85f, 1.15f, (float)random.NextDouble())
                ));
            }
        }
        if (paths.Count > 0) {
            Layer("Birds", BackdropMeshes.Birds(paths, flapSpans), birdMaterial);
        }
    }

    // trees between innerU & outerU from the trunk, wholly outside innerU,
    // spread evenly, each its own height, girth & shape: mostly round
    // crowns, some tall & slender, some broad with a crown of 3 clumps; no
    // crown top above topLimit
    private void AddTrees(
        BackdropMeshes[] wedges,
        List<Vector3> placed,
        System.Random random,
        int count,
        float innerU,
        float outerU,
        float topLimit,
        float canopyLocal,
        float floorLocal
    ) {
        int planted = 0;
        int attempts = count * 20;
        while (planted < count && attempts-- > 0) {
            float kind = (float)random.NextDouble();
            bool isSlender = kind < 0.2f;
            bool isBroad = kind > 0.78f;
            float radius = Mathf.Lerp(crownRadiusU.x, crownRadiusU.y, (float)random.NextDouble());
            if (isSlender) {
                radius *= 0.6f;
            } else if (isBroad) {
                radius *= 1.15f;
            }
            float footprint = isBroad ? radius * 1.4f : radius;
            // the lean below shifts a crown up to 0.15 of its radius
            float inner = innerU + footprint * 1.2f;
            if (inner >= outerU) {
                continue;
            }
            // even over the ring's area
            float distance = Mathf.Sqrt(
                Mathf.Lerp(inner * inner, outerU * outerU, (float)random.NextDouble())
            );
            Vector3 foot = Around(random, distance) + Vector3.up * floorLocal;
            if (Crowds(placed, foot, footprint)) {
                continue;
            }
            placed.Add(new Vector3(foot.x, foot.z, footprint));
            planted++;

            float squash = isSlender
                ? Mathf.Lerp(1.1f, 1.5f, (float)random.NextDouble())
                : isBroad
                    ? Mathf.Lerp(0.5f, 0.68f, (float)random.NextDouble())
                    : Mathf.Lerp(0.65f, 1f, (float)random.NextDouble());
            // heights spread wide, slender ones standing tallest
            float rise = isSlender
                ? Mathf.Lerp(-2f, 8f, (float)random.NextDouble())
                : Mathf.Lerp(-15f, 1f, (float)random.NextDouble());
            // crowns break the canopy's top, sinking under it toward the
            // ring's edge, so the ring melts into the painting
            float sink = 18f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(paintedFromU + 30f, nearRadiusU, distance));
            float centreY = Mathf.Min(canopyLocal + rise, topLimit - radius * squash) - sink;
            // a slight lean
            Vector3 lean = new Vector3((float)random.NextDouble() - 0.5f, 0f, (float)random.NextDouble() - 0.5f)
                * radius * 0.3f;
            Vector3 centre = new Vector3(foot.x, centreY, foot.z) + lean;

            float girth = isSlender ? 0.13f : isBroad ? 0.17f : Mathf.Lerp(0.09f, 0.15f, (float)random.NextDouble());
            Color green = CrownColor(random);
            BackdropMeshes wedge = wedges[Wedge(foot)];
            wedge.AddTrunk(foot, centre, radius * girth, isBroad ? 0.8f : 0.5f, 5, 2, BARK);
            wedge.AddCrown(centre, radius, squash, 11, 6, green, random);
            if (isBroad) {
                float spin = (float)random.NextDouble() * Mathf.PI * 2f;
                for (int k = 0; k < 2; k++) {
                    float a = spin + k * Mathf.PI * 0.85f;
                    Vector3 side = new Vector3(Mathf.Cos(a), -0.25f, Mathf.Sin(a)) * radius * 0.75f;
                    wedge.AddCrown(centre + side, radius * 0.62f, squash * 1.1f, 9, 5, green * 0.93f, random);
                }
            }
        }
    }

    private static int Wedge(Vector3 foot) {
        float turn = Mathf.Repeat(Mathf.Atan2(foot.z, foot.x) / (Mathf.PI * 2f), 1f);
        return Mathf.Min((int)(turn * TREE_WEDGES), TREE_WEDGES - 1);
    }

    private static bool Crowds(List<Vector3> placed, Vector3 foot, float radius) {
        foreach (Vector3 other in placed) {
            float dx = other.x - foot.x;
            float dz = other.y - foot.z;
            float apart = 0.66f * (radius + other.z);
            if (dx * dx + dz * dz < apart * apart) {
                return true;
            }
        }
        return false;
    }

    private static Vector3 Around(System.Random random, float distanceU) {
        float angle = (float)random.NextDouble() * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(angle) * distanceU, 0f, Mathf.Sin(angle) * distanceU);
    }

    private static Color CrownColor(System.Random random) {
        Color color = CROWN_COLORS[random.Next(CROWN_COLORS.Length)];
        return color * Mathf.Lerp(0.92f, 1.08f, (float)random.NextDouble());
    }

    private GameObject Layer(string layerName, Mesh mesh, Material material) {
        meshes.Add(mesh);
        GameObject layer = new GameObject(layerName);
        layer.transform.SetParent(transform, false);
        layer.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer layerRenderer = layer.AddComponent<MeshRenderer>();
        layerRenderer.sharedMaterial = material;
        layerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        layerRenderer.receiveShadows = false;
        layerRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        layerRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return layer;
    }

    // Apply  ------------------------------------------------------------------
    // everything that changes with height, as a few shader globals
    private void Apply(float climbedU) {
        float high = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, highClimbedU, climbedU));
        Shader.SetGlobalColor(ZENITH_ID, Color.Lerp(lowZenith, highZenith, high));
        Shader.SetGlobalColor(HORIZON_ID, Color.Lerp(lowHorizon, highHorizon, high));
        Shader.SetGlobalColor(HAZE_COLOR_ID, hazeColor);
        Shader.SetGlobalFloat(HAZE_DENSITY_ID, Mathf.Lerp(hazeDensity.x, hazeDensity.y, high));
        Shader.SetGlobalFloat(
            CLOUD_ID,
            Mathf.Lerp(cloudAmount.x, cloudAmount.y, Mathf.InverseLerp(cloudClimbedU.x, cloudClimbedU.y, climbedU))
        );
        float flyers = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flyersShowClimbedU.x, flyersShowClimbedU.y, climbedU));
        Shader.SetGlobalFloat(PUFFS_ID, flyers);
        Shader.SetGlobalFloat(BIRDS_ID, flyers);
        Shader.SetGlobalFloat(
            RIVER_ID,
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(riverStartClimbedU, riverFullClimbedU, climbedU))
        );

        if (sun == null && !hasLookedForSun) {
            sun = RenderSettings.sun != null ? RenderSettings.sun : FindSun();
            hasLookedForSun = true;
        }
        Vector3 toSun = sun != null ? -sun.transform.forward : new Vector3(-0.4f, 0.8f, -0.4f).normalized;
        Shader.SetGlobalVector(SUN_ID, toSun);
    }

    private static Light FindSun() {
        foreach (Light candidate in FindObjectsByType<Light>(FindObjectsSortMode.None)) {
            if (candidate.type == LightType.Directional) {
                return candidate;
            }
        }
        return null;
    }
}
