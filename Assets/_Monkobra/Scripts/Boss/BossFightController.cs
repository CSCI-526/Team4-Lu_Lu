using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the boss fight against the Cobra King. Once the monkey has climbed
/// <see cref="GameBalanceConfig.BossTriggerHeightU"/> the screen flashes and
/// the run enters the boss arena: the king coils round the tree top above,
/// the great cobra stops chasing and guards below, and the spawners of webs,
/// falling cobras and wet bark stop. Coconuts hang on the trunk in between; a
/// grabbed one flies at the king's crown jewel by itself, so the fight uses
/// only the grab the player already knows.
/// <para>
/// The fight loops: the king rests, then attacks; a coconut hurts it at any
/// time. Before the first hit it only rests, and the first coconut carries a
/// key prompt and a dotted line to the jewel. Its attacks grow with each lost third of its health: a bite
/// at a marked spot, a tail sweep through a marked ring (both fatal, both
/// warned), and minions dropping on marked spots (a knock-down). A golden
/// banana refills stamina and gives a gold shield that turns one fatal hit
/// into a knock-down.
/// </para>
/// <para>
/// The monkey can never climb close to the king: a height ceiling holds it
/// below the chin however long up is held, and a monkey at the ceiling, or
/// touching the king, is butted down often ("Stay away from me!"), never
/// fatally. The great cobra guards below without moving, peeking up now &
/// then; a monkey below the arena hears "Nowhere to run!".
/// The camera tilts up so the king stays in view. At zero health the king
/// shatters into cubes, its crown flies onto the monkey's head and the run
/// is won (<see cref="GameController.WinGame"/>).
/// </para>
/// </summary>
[RequireComponent(typeof(BossKingBody), typeof(BossFightHud))]
public class BossFightController: MonoBehaviour {
    // Public Members  #########################################################
    public static BossFightController I {
        get; private set;
    }

    /// <returns>if the monkey is in the boss arena, fighting or done
    /// </returns>
    public bool IsInArena => phase != Phase.Waiting;

    // Public Methods  #########################################################
    /// <summary>
    /// Called by <see cref="ArmController"/> on a grab: a coconut of the
    /// arena is thrown at the king, the golden banana gives its shield,
    /// instead of either being eaten.
    /// </summary>
    /// <returns>true if the grabbed fruit belonged to the arena</returns>
    public bool TryThrow(Collider fruit, Vector3 from) {
        if (phase != Phase.Fighting || fruit == null) {
            return false;
        }
        int index = fruits.FindIndex(f => f.trigger == fruit);
        if (index < 0) {
            return false;
        }

        ArenaFruit picked = fruits[index];
        picked.root.SetActive(false);
        picked.respawnTime = picked.isGolden
            ? -1f
            : Time.time + coconutRespawnS * CoconutRespawnScale;
        fruits[index] = picked;

        if (picked.isGolden) {
            HideGolden();
            nextGoldenTime = Time.time + goldenIntervalS;
            TakeGoldenBanana();
            return true;
        }

        Transform projectile = CreatePrimitive(
            PrimitiveType.Sphere, "ThrownCoconut",
            Vector3.one * coconutRadiusU * 2f, coconutColor
        ).transform;
        projectile.position = from;
        projectiles.Add(projectile);
        return true;
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("balance numbers: trigger height, king health, coconut damage")]
    private GameBalanceConfig balanceConfig;

    [Header("Arena")]
    [SerializeField]
    [Tooltip("name shown over the health bar")]
    private string bossName = "COBRA KING";

    [SerializeField]
    [Tooltip("height of the king's coil above where the monkey entered; u")]
    private float nestHeightU = 11f;

    [SerializeField]
    [Tooltip("the great cobra's head is held this far below the entry; u")]
    private float guardDepthU = 12f;

    [SerializeField]
    [Tooltip("a monkey this far below the entry hears the king's warning; u")]
    private float retreatLimitU = 6f;


    [SerializeField]
    [Tooltip("hazards & branches this close below the coil are cleared, so "
        + "none pokes through the king; u")]
    private float nestClearanceU = 2f;

    [SerializeField]
    [Tooltip("trunk radius, matching the tree segments; u")]
    private float trunkRadiusU = 5f;

    [SerializeField]
    [Tooltip("the camera pulls back to this many times its normal distance "
        + "in the arena, to show the king & the monkey together")]
    private float cameraDistanceScale = 1.7f;

    [SerializeField]
    [Tooltip("share of the height from the monkey up to the king the camera "
        + "aims up by; kept low so a knocked-down monkey stays in view")]
    private float cameraFocusShare = 0.3f;

    [SerializeField]
    [Tooltip("most the camera aims up toward the king; u")]
    private float maxCameraFocusU = 3f;

    [Header("Keep Away (ceiling & head butt)")]
    [SerializeField]
    [Tooltip("the monkey's climb is capped this far below the king's chin; u")]
    private float ceilingGapU = 1.5f;

    [SerializeField]
    [Tooltip("a monkey within this below the ceiling gets butted down; u")]
    private float pushRangeU = 1.2f;

    [SerializeField]
    [Tooltip("the head rears back this long before a butt; s")]
    private float pushWindUpS = 0.25f;

    [SerializeField]
    [Tooltip("least time between two butts; s")]
    private float pushCooldownS = 0.8f;

    [Header("Fight Loop")]
    [SerializeField]
    [Tooltip("rest with the jewel lit between attack rounds; s")]
    private float restS = 2.5f;

    [SerializeField]
    [Tooltip("rest in the last third of the king's health; s")]
    private float restFinalS = 1.8f;

    [SerializeField]
    [Tooltip("warnings in the last third last this share of the normal; "
        + "0~1")]
    private float finalWarnScale = 0.75f;

    [Header("Bite (fatal)")]
    [SerializeField]
    [Tooltip("the target is marked this long before the bite lands; s")]
    private float biteWarnS = 1.2f;

    [SerializeField]
    [Tooltip("a monkey within this of the marked spot is bitten; u")]
    private float biteRadiusU = 2.2f;

    [Header("Tail Sweep (fatal)")]
    [SerializeField]
    [Tooltip("the ring is marked this long before the tail sweeps; s")]
    private float sweepWarnS = 1.3f;

    [SerializeField]
    [Tooltip("time the tail takes for a full loop round the trunk; s")]
    private float sweepTimeS = 0.9f;

    [SerializeField]
    [Tooltip("a monkey within this above or below the ring is swept; u")]
    private float sweepBandHalfU = 1.2f;

    [SerializeField]
    [Min(1)]
    [Tooltip("beads in the sweeping tail")]
    private int tailBeadCount = 7;

    [Header("Minions (knock-down)")]
    [SerializeField]
    [Min(1)]
    [Tooltip("small snakes dropped per minion attack")]
    private int minionCount = 3;

    [SerializeField]
    [Tooltip("angle between neighbouring minions round the trunk; deg")]
    private float minionSpreadDeg = 35f;

    [SerializeField]
    [Tooltip("drop spots are marked this long before the minions fall; s")]
    private float minionWarnS = 0.9f;

    [SerializeField]
    [Tooltip("minion fall speed; u/s")]
    private float minionFallSpeedU = 9f;

    [SerializeField]
    [Tooltip("a minion this close knocks the monkey down; u")]
    private float minionHitRadiusU = 1.1f;

    [Header("Spider Webs")]
    [SerializeField]
    [Tooltip("the web prefab the tree segments use")]
    private SpiderWebTrap webPrefab;

    [SerializeField]
    [Min(0)]
    [Tooltip("webs on the trunk when the fight begins")]
    private int webCount = 2;

    [SerializeField]
    [Min(0)]
    [Tooltip("webs added each time the king loses another third of its "
        + "health")]
    private int websPerThird = 1;

    [SerializeField]
    [Tooltip("webs keep this far round the trunk from any fruit; deg")]
    private float webFruitGapDeg = 25f;


    [Header("Coconuts")]
    [SerializeField]
    [Min(1)]
    [Tooltip("coconuts hanging in the arena")]
    private int coconutCount = 5;

    [SerializeField]
    [Min(0)]
    [Tooltip("coconuts added each time the king loses another third of "
        + "its health")]
    private int coconutsPerThird = 2;

    [SerializeField]
    [Tooltip("coconut grow-back time as a share of the normal, once 1 and "
        + "2 thirds of the health are lost")]
    private Vector2 coconutRespawnScaleByThird = new Vector2(0.75f, 0.5f);

    [SerializeField]
    [Tooltip("lowest & highest coconut height above the entry; u")]
    private Vector2 coconutHeightRangeU = new Vector2(2f, 5.5f);

    [SerializeField]
    [Tooltip("radius of a fruit branch's root, as the tree segments' "
        + "BranchFruitPlacementConfig.branchRadius; u")]
    private float branchRadiusU = 6.6f;

    [SerializeField]
    [Tooltip("coconut radius; u")]
    private float coconutRadiusU = 0.55f;

    [SerializeField]
    [Tooltip("a thrown coconut grows back on its spot after this; s")]
    private float coconutRespawnS = 6f;

    [SerializeField]
    [Tooltip("flight speed of a thrown coconut; u/s")]
    private float throwSpeedU = 20f;

    [Header("Golden Banana")]
    [SerializeField]
    [Tooltip("the golden banana first shows up this long into the fight; s")]
    private float goldenFirstDelayS = 8f;

    [SerializeField]
    [Tooltip("time between two golden banana visits, once one is gone; s")]
    private float goldenIntervalS = 15f;

    [SerializeField]
    [Tooltip("how long a golden banana stays before it vanishes; s")]
    private float goldenShowS = 8f;

    [SerializeField]
    [Tooltip("it grows this far round the trunk from the monkey, so in "
        + "view; deg")]
    private float goldenSideDeg = 30f;

    [SerializeField]
    [Tooltip("it grows this far above the monkey, kept under the ceiling; u")]
    private float goldenAboveMonkeyU = 3f;

    [SerializeField]
    [Tooltip("the armor's back plate sits this far out from the monkey's "
        + "center; u")]
    private float armorOutU = 0.45f;

    [SerializeField]
    [Tooltip("the armor's helmet sits this far above the monkey's center; u")]
    private float armorHelmetU = 0.95f;

    [Header("Victory")]
    [SerializeField]
    [Tooltip("time the crown takes to fly onto the monkey; s")]
    private float crownFlyS = 1.2f;

    [SerializeField]
    [Tooltip("crown size on the monkey, as a share of the king's")]
    private float crownScaleOnMonkey = 0.55f;

    [SerializeField]
    [Tooltip("crown height above the monkey's center; u")]
    private float crownAboveMonkeyU = 1.3f;

    [SerializeField]
    [Tooltip("pause after the crown lands before the victory panel; s")]
    private float victoryDelayS = 0.8f;

    [Header("Dotted Line")]
    [SerializeField]
    [Min(2)]
    [Tooltip("dots from the first coconut to the jewel")]
    private int dotCount = 12;

    [SerializeField]
    [Tooltip("dot radius; u")]
    private float dotRadiusU = 0.12f;

    [Header("Colors")]
    [SerializeField]
    private Color coconutColor = new Color(0.42f, 0.25f, 0.1f);

    [SerializeField]
    private Color fruitTargetedColor = new Color(0.75f, 0.5f, 0.2f);

    [SerializeField]
    private Color goldenColor = new Color(1f, 0.82f, 0.15f);

    [SerializeField]
    private Color warningColor = new Color(1f, 0.15f, 0.15f);

    [SerializeField]
    private Color warningDimColor = new Color(0.45f, 0.05f, 0.05f);

    [SerializeField]
    private Color branchColor = new Color(0.27f, 0.31f, 0.38f);

    [SerializeField]
    private Color stemColor = new Color(0.22f, 0.14f, 0.09f);

    [SerializeField]
    private Color minionColor = new Color(0.45f, 0.8f, 0.45f);

    [SerializeField]
    private Color tailColor = new Color(0.72f, 0.56f, 0.95f);

    [Header("Speech")]
    [SerializeField]
    private string approachLine = "Stay away from me!";

    [SerializeField]
    private string retreatLine = "Nowhere to run!";

    [SerializeField]
    private string firstHitLine = "How dare you!";


    [SerializeField]
    private string finalThirdLine = "Minions, help me!";

    [SerializeField]
    private string defeatLine = "Nooo... my crown!";

    [SerializeField]
    [Tooltip("the same line is not repeated within this; s")]
    private float speechCooldownS = 4f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (balanceConfig == null) {
            Debug.LogError(
                "BossFightController:\tmust assign Inspector Field: "
                    + "balanceConfig",
                this
            );
            enabled = false;
            return;
        }

        if (I != null && I != this) {
            Debug.LogWarning(
                "BossFightController:\tduplicate instance destroyed",
                this
            );
            Destroy(gameObject);
            return;
        }
        I = this;

        body = GetComponent<BossKingBody>();
        body.MonkeyTouched += OnKingTouched;
        hud = GetComponent<BossFightHud>();
        colorBlock = new MaterialPropertyBlock();
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
        TreeSegmentController.BranchesGenerated -= OnBranchesGenerated;
    }

    private void OnDisable() {
        if (monkeyController != null) {
            monkeyController.ClearHeightCeiling();
        }
        if (cameraLookUp != null) {
            cameraLookUp.ExtraAimOffsetU = 0f;
            cameraLookUp.ViewDistanceScale = 1f;
        }
    }

    private void Update() {
        GameController game = GameController.I;
        if (game == null) {
            return;
        }
        if (phase == Phase.Waiting) {
            if (game.State == GameState.Playing && HasReachedTrigger()) {
                BeginFight();
            }
            return;
        }
        if (!game.IsRunLive || monkey == null) {
            return;
        }

        body.Track(monkey.position);
        hud.SpeechAnchor = body.HeadPosition;
        UpdateProjectiles();
        UpdateFruits();
        UpdateGolden();
        UpdateShieldRing();
        UpdateCameraFocus();
        if (phase == Phase.Fighting) {
            UpdateWarnings();
            UpdateFirstThrowHint();
            UpdateFightLoop();
        } else if (phase == Phase.Defeated) {
            UpdateVictory();
        }
    }

    // private methods  ########################################################
    // Setup  ------------------------------------------------------------------
    private bool HasReachedTrigger() {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        return ramp != null
            && ramp.CurrentPlayerClimbedYDistance
                >= balanceConfig.BossTriggerHeightU;
    }

    private void BeginFight() {
        MonkeyController monkeyController =
            FindFirstObjectByType<MonkeyController>();
        if (monkeyController == null
            || monkeyController.transform.parent == null) {
            Debug.LogError(
                "BossFightController:\tno MonkeyController under a trunk "
                    + "root found",
                this
            );
            enabled = false;
            return;
        }
        monkey = monkeyController.transform;
        this.monkeyController = monkeyController;
        trunkCenter = monkey.parent.position;
        entryY = monkey.position.y;
        coilBottomY = entryY + nestHeightU;
        health = balanceConfig.BossMaxHealth;
        phase = Phase.Fighting;
        loop = Loop.Intro;

        StopHazardSpawners();
        ClearAbove(entryY - 3f);
        TreeSegmentController.BranchesGenerated += OnBranchesGenerated;

        cameraLookUp = FindFirstObjectByType<CameraLookUp>();
        greatCobra = FindFirstObjectByType<CobraMotor>();
        if (greatCobra != null) {
            greatCobra.StartGuard(entryY - guardDepthU);
        }

        body.Build(trunkCenter, coilBottomY, AngleOf(monkey.position));
        body.SetWeakSpotLit(true);
        SpawnFruits();
        SpawnWebs(webCount);
        BuildDots();
        BuildShieldRing();

        hud.Show(bossName);
        hud.Flash();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "BossFightController:\tboss fight begins at y " + entryY,
                this
            );
        }
    }

    // webs, falling cobras & wet bark stay out of the arena
    private void StopHazardSpawners() {
        foreach (SpiderWebSpawner spawner in
            FindObjectsByType<SpiderWebSpawner>(FindObjectsSortMode.None)) {
            spawner.enabled = false;
        }
        foreach (FallingCobraSpawner spawner in
            FindObjectsByType<FallingCobraSpawner>(FindObjectsSortMode.None)) {
            spawner.enabled = false;
        }
        foreach (SlipperyBarkSpawner spawner in
            FindObjectsByType<SlipperyBarkSpawner>(FindObjectsSortMode.None)) {
            spawner.enabled = false;
        }
    }

    // hazards already placed in the arena go, & branches near the nest
    private void ClearAbove(float minY) {
        foreach (SpiderWebTrap web in
            FindObjectsByType<SpiderWebTrap>(FindObjectsSortMode.None)) {
            if (web.transform.position.y > minY) {
                web.gameObject.SetActive(false);
            }
        }
        foreach (FallingCobraController cobra in
            FindObjectsByType<FallingCobraController>(FindObjectsSortMode.None)) {
            if (cobra.transform.position.y > minY) {
                cobra.gameObject.SetActive(false);
            }
        }
        foreach (SlipperyBarkZone zone in
            FindObjectsByType<SlipperyBarkZone>(FindObjectsSortMode.None)) {
            if (zone.transform.position.y > minY) {
                zone.gameObject.SetActive(false);
            }
        }
        ClearBranchesNearNest(
            FindObjectsByType<BranchController>(FindObjectsSortMode.None)
        );
    }

    // a segment recycled over the top during the fight rolls new branches
    private void OnBranchesGenerated(TreeSegmentController segment) {
        ClearBranchesNearNest(
            segment.GetComponentsInChildren<BranchController>()
        );
    }

    private void ClearBranchesNearNest(BranchController[] branches) {
        float minY = coilBottomY - body.HangDepthU - nestClearanceU;
        foreach (BranchController branch in branches) {
            if (branch.transform.position.y > minY) {
                branch.gameObject.SetActive(false);
            }
        }
    }

    private void SpawnFruits() {
        float startAngle = AngleOf(monkey.position);
        for (int i = 0; i < coconutCount; i++) {
            // spread round the trunk, the first one right above the monkey
            float t = coconutCount > 1 ? i / (coconutCount - 1f) : 0f;
            AddFruit(
                "ArenaCoconut" + i,
                startAngle + i * 360f / coconutCount,
                entryY + Mathf.Lerp(
                    coconutHeightRangeU.x, coconutHeightRangeU.y, t
                ),
                false
            );
        }
        // the golden banana starts hidden & shows up now & then
        goldenIndex = AddFruit("GoldenBanana", startAngle, entryY, true);
        HideGolden();
        nextGoldenTime = Time.time + goldenFirstDelayS;
    }

    // extra coconuts at random spots round the trunk, kept apart
    private void AddCoconuts(int count) {
        for (int i = 0; i < count; i++) {
            float angle = Random.Range(0f, 360f);
            float y = entryY + Random.Range(
                coconutHeightRangeU.x, coconutHeightRangeU.y
            );
            AddFruit("ArenaCoconut" + fruits.Count, angle, y, false);
        }
    }

    // webs on the trunk in the arena, never on a fruit's branch line
    private void SpawnWebs(int count) {
        if (webPrefab == null) {
            return;
        }
        float topY = body.LowestY - ceilingGapU - 1.5f;
        for (int i = 0; i < count; i++) {
            for (int attempt = 0; attempt < 12; attempt++) {
                float angle = Random.Range(0f, 360f);
                if (IsNearFruit(angle)) {
                    continue;
                }
                float y = Random.Range(entryY + 0.5f, Mathf.Max(entryY + 1f, topY));
                // never right on the monkey
                bool isOnMonkey = Mathf.Abs(Mathf.DeltaAngle(
                    angle, AngleOf(monkey.position)
                )) < 30f && Mathf.Abs(y - monkey.position.y) < 3f;
                if (isOnMonkey) {
                    continue;
                }
                // as the segments' spawner: the web's face looks away from
                // the trunk
                SpiderWebTrap web = Instantiate(
                    webPrefab,
                    PointOnTrunk(angle, y, trunkRadiusU + 0.02f),
                    Quaternion.Euler(0f, -angle, 0f),
                    transform
                );
                webs.Add(web);
                break;
            }
        }
    }

    private bool IsNearFruit(float angle) {
        foreach (ArenaFruit fruit in fruits) {
            if (fruit.isGolden) {
                continue;
            }
            float fruitAngle = AngleOf(fruit.root.transform.position);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, fruitAngle)) < webFruitGapDeg) {
                return true;
            }
        }
        return false;
    }

    private float CoconutRespawnScale => Third >= 2
        ? coconutRespawnScaleByThird.y
        : Third >= 1
            ? coconutRespawnScaleByThird.x
            : 1f;

    /// <returns>the new fruit's index in the fruit list</returns>
    private int AddFruit(string fruitName, float angle, float y, bool isGolden) {
        GameObject root = isGolden
            ? CreatePrimitive(
                PrimitiveType.Capsule, fruitName,
                new Vector3(0.5f, 0.75f, 0.5f), goldenColor
            )
            : CreatePrimitive(
                PrimitiveType.Sphere, fruitName,
                Vector3.one * coconutRadiusU * 2f, coconutColor
            );
        GameObject[] stub = AddBranchStub(angle, y, out Vector3 fruitSpot);
        root.transform.position = fruitSpot;
        if (isGolden) {
            root.transform.rotation = Quaternion.Euler(0f, -angle, 30f);
        }

        // the grab sensors look for a "FruitCollider" trigger
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.6f;
        root.tag = FRUIT_TAG;

        fruits.Add(new ArenaFruit {
            root = root,
            trigger = trigger,
            renderer = root.GetComponent<Renderer>(),
            isGolden = isGolden,
            stub = stub,
            respawnTime = -1f
        });
        return fruits.Count - 1;
    }

    // Golden Banana Visits  ---------------------------------------------------
    // now & then the golden banana grows in sight, beside the monkey, for a
    // short while
    private void UpdateGolden() {
        if (goldenIndex < 0 || phase != Phase.Fighting) {
            return;
        }
        ArenaFruit golden = fruits[goldenIndex];
        if (golden.root.activeSelf) {
            if (Time.time >= goldenHideTime) {
                HideGolden();
                nextGoldenTime = Time.time + goldenIntervalS;
            }
            return;
        }
        if (Time.time >= nextGoldenTime && !hasShield) {
            ShowGolden();
        }
    }

    private void ShowGolden() {
        ArenaFruit golden = fruits[goldenIndex];
        float side = Random.value < 0.5f ? -1f : 1f;
        float angle = AngleOf(monkey.position) + side * goldenSideDeg;
        float y = Mathf.Clamp(
            monkey.position.y + goldenAboveMonkeyU,
            entryY + coconutHeightRangeU.x,
            body.LowestY - ceilingGapU - 0.5f
        );
        golden.root.transform.position =
            PlaceBranchStub(golden.stub, angle, y);
        golden.root.transform.rotation = Quaternion.Euler(0f, -angle, 30f);
        golden.root.SetActive(true);
        foreach (GameObject part in golden.stub) {
            part.SetActive(true);
        }
        goldenHideTime = Time.time + goldenShowS;
        hud.PopText("Golden banana!", golden.root.transform.position, goldenColor);
    }

    private void HideGolden() {
        ArenaFruit golden = fruits[goldenIndex];
        golden.root.SetActive(false);
        foreach (GameObject part in golden.stub) {
            part.SetActive(false);
        }
    }

    // the fruit grows on a branch like the bananas do: the same plank, set
    // at the same radius & angle as a tree segment's BranchWithFruit, with
    // the fruit hanging under its tip; tagged Branch, so it knocks the monkey
    // down like any branch. It stays while the fruit grows back
    private GameObject[] AddBranchStub(
        float angle,
        float fruitY,
        out Vector3 fruitSpot
    ) {
        Transform root = new GameObject("FruitBranch").transform;
        root.SetParent(transform, false);

        GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = "Branch";
        plank.tag = BRANCH_TAG;
        plank.transform.SetParent(root, false);
        plank.transform.localRotation = BRANCH_LOCAL_ROTATION;
        plank.transform.localScale = BRANCH_LOCAL_SCALE;
        Renderer plankRenderer = plank.GetComponent<Renderer>();
        if (body.PrimitiveMaterial != null) {
            plankRenderer.sharedMaterial = body.PrimitiveMaterial;
        }
        SetColor(plankRenderer, branchColor);

        GameObject stem = CreatePrimitive(
            PrimitiveType.Cylinder, "FruitStem",
            new Vector3(0.08f, 0.35f, 0.08f), stemColor
        );
        stem.transform.SetParent(root, false);
        stem.transform.localPosition = FRUIT_LOCAL_SPOT + Vector3.up * 0.55f;

        GameObject[] parts = { root.gameObject };
        fruitSpot = PlaceBranchStub(parts, angle, fruitY);
        return parts;
    }

    /// <returns>where the fruit hangs under the moved branch</returns>
    private Vector3 PlaceBranchStub(GameObject[] parts, float angle, float fruitY) {
        Transform root = parts[0].transform;
        // the root's +z points away from the trunk
        root.SetPositionAndRotation(
            PointOnTrunk(angle, fruitY - FRUIT_LOCAL_SPOT.y, branchRadiusU),
            Quaternion.Euler(0f, 90f - angle, 0f)
        );
        return root.TransformPoint(FRUIT_LOCAL_SPOT);
    }

    // dotted line from the first coconut to the jewel, until the 1st hit
    private void BuildDots() {
        dots = new Transform[dotCount];
        for (int i = 0; i < dotCount; i++) {
            dots[i] = CreatePrimitive(
                PrimitiveType.Sphere, "HintDot" + i,
                Vector3.one * dotRadiusU * 2f, Color.white
            ).transform;
        }
    }

    // gold armor worn while shielded: a back plate facing the camera, two
    // shoulder pads & a helmet, all plain primitives
    private void BuildShieldRing() {
        armor = new Transform[4];
        armor[0] = CreatePrimitive(
            PrimitiveType.Cylinder, "ArmorPlate",
            new Vector3(1.1f, 0.08f, 1.3f), goldenColor
        ).transform;
        for (int side = 0; side < 2; side++) {
            armor[1 + side] = CreatePrimitive(
                PrimitiveType.Sphere, side == 0 ? "ArmorPadL" : "ArmorPadR",
                new Vector3(0.6f, 0.4f, 0.6f), goldenColor
            ).transform;
        }
        armor[3] = CreatePrimitive(
            PrimitiveType.Sphere, "ArmorHelmet",
            new Vector3(0.85f, 0.5f, 0.85f), goldenColor
        ).transform;
        foreach (Transform piece in armor) {
            piece.gameObject.SetActive(false);
        }
    }

    // Fight Loop  -------------------------------------------------------------
    // rest (jewel lit) → a round of attacks (jewel dark) → rest ...
    private void UpdateFightLoop() {
        switch (loop) {
            case Loop.Intro:
                // waits for the first hit, which starts the first rest
                break;
            case Loop.Rest:
                loopTimeS -= Time.deltaTime;
                if (loopTimeS <= 0f) {
                    StartRound();
                }
                break;
            case Loop.Attack:
                UpdateAttack();
                break;
        }
    }

    private void StartRest() {
        loop = Loop.Rest;
        loopTimeS = Third >= 2 ? restFinalS : restS;
        body.SetWeakSpotLit(true);
    }

    // two bites a round, & each lost third of health adds an attack
    private void StartRound() {
        loop = Loop.Attack;
        attackQueue.Clear();
        if (Third >= 2) {
            attackQueue.Enqueue(Attack.Minions);
        }
        attackQueue.Enqueue(Attack.Bite);
        if (Third >= 1) {
            attackQueue.Enqueue(Attack.Sweep);
        }
        attackQueue.Enqueue(Attack.Bite);
        NextAttack();
    }

    private void NextAttack() {
        ClearMarkers();
        if (attackQueue.Count == 0) {
            StartRest();
            return;
        }
        attack = attackQueue.Dequeue();
        attackTimeS = 0f;
        lockAngle = AngleOf(monkey.position);
        lockY = monkey.position.y;
        switch (attack) {
            case Attack.Bite:
                StartBite();
                break;
            case Attack.Sweep:
                StartSweep();
                break;
            case Attack.Minions:
                StartMinions();
                break;
        }
    }

    private void UpdateAttack() {
        attackTimeS += Time.deltaTime;
        switch (attack) {
            case Attack.Bite:
                UpdateBite();
                break;
            case Attack.Sweep:
                UpdateSweep();
                break;
            case Attack.Minions:
                UpdateMinions();
                break;
        }
    }

    private float WarnScale => Third >= 2 ? finalWarnScale : 1f;

    // Bite: a red disc marks the spot, the head dives at it
    private void StartBite() {
        biteSpot = PointOnTrunk(lockAngle, lockY, MonkeyRadius());
        AddTrunkZone(lockAngle, lockY, biteRadiusU * 2f, "BITE!", true);
        biteStarted = false;
    }

    // the strike starts once the head is free, its wind-up filling the rest
    // of the warning; the attack ends once the head is back at rest
    private void UpdateBite() {
        UpdateMarkers(attackTimeS / Mathf.Max(0.01f, biteWarnS * WarnScale), false);
        if (!biteStarted) {
            if (body.IsStriking) {
                return;
            }
            biteStarted = true;
            float windUp = Mathf.Max(
                0.3f,
                biteWarnS * WarnScale - attackTimeS
            );
            Vector3 spot = biteSpot;
            body.StartStrike(spot, windUp, () => {
                if ((monkey.position - spot).magnitude < biteRadiusU) {
                    LethalHit();
                }
            });
            return;
        }
        if (!body.IsStriking) {
            biteStarted = false;
            NextAttack();
        }
    }

    // Sweep: a red band fills round the trunk at the height, then the tail
    // circles it
    private void StartSweep() {
        GameObject band = CreatePrimitive(
            PrimitiveType.Cylinder, "SweepBand",
            new Vector3(
                (trunkRadiusU + 0.08f) * 2f,
                0.01f,
                (trunkRadiusU + 0.08f) * 2f
            ),
            warningColor
        );
        band.transform.position = new Vector3(
            trunkCenter.x, lockY, trunkCenter.z
        );
        int warning = hud.AddWarning(SweepSignPoint(), "TAIL!");
        markers.Add(new Marker {
            fill = band.transform,
            warning = warning,
            isBand = true
        });
        tailHit = false;
    }

    // the band's sign follows the monkey round the trunk, so it stays in view
    private Vector3 SweepSignPoint() {
        return PointOnTrunk(
            AngleOf(monkey.position), lockY + sweepBandHalfU, MonkeyRadius()
        );
    }

    private void UpdateSweep() {
        float warn = sweepWarnS * WarnScale;
        if (attackTimeS < warn) {
            UpdateMarkers(attackTimeS / warn, true);
            return;
        }
        ClearMarkers();
        if (tail == null) {
            tail = new Transform[tailBeadCount];
            for (int i = 0; i < tailBeadCount; i++) {
                tail[i] = CreatePrimitive(
                    PrimitiveType.Sphere, "Tail" + i,
                    Vector3.one * Mathf.Lerp(1.6f, 0.8f,
                        i / Mathf.Max(1f, tailBeadCount - 1f)),
                    tailColor
                ).transform;
            }
        }

        // starts across the trunk & loops round once
        float t = (attackTimeS - warn) / sweepTimeS;
        float headAngle = lockAngle + 180f + t * 360f;
        for (int i = 0; i < tail.Length; i++) {
            tail[i].position = PointOnTrunk(
                headAngle - i * 9f, lockY, trunkRadiusU + 1.1f
            );
        }

        float gap = Mathf.Abs(Mathf.DeltaAngle(
            headAngle, AngleOf(monkey.position)
        ));
        if (!tailHit && gap < 14f
            && Mathf.Abs(monkey.position.y - lockY) < sweepBandHalfU) {
            tailHit = true;
            LethalHit();
        }

        if (t >= 1f) {
            foreach (Transform bead in tail) {
                Destroy(bead.gameObject);
            }
            tail = null;
            NextAttack();
        }
    }

    // Minions: drop spots marked round the monkey, then small snakes fall
    private void StartMinions() {
        minions.Clear();
        for (int i = 0; i < minionCount; i++) {
            float angle = lockAngle
                + (i - (minionCount - 1) * 0.5f) * minionSpreadDeg;
            AddTrunkZone(
                angle, lockY, minionHitRadiusU * 2f,
                i == (minionCount - 1) / 2 ? "LOOK UP!" : null, false
            );
            minions.Add(new Minion { angle = angle, root = null });
        }
    }

    private void UpdateMinions() {
        float warn = minionWarnS * WarnScale;
        if (attackTimeS < warn) {
            UpdateMarkers(attackTimeS / warn, false);
            return;
        }
        bool isAnyLeft = false;
        for (int i = 0; i < minions.Count; i++) {
            Minion minion = minions[i];
            if (minion.isDone) {
                continue;
            }
            if (minion.root == null) {
                minion.root = CreateMinion();
                minion.root.position = PointOnTrunk(
                    minion.angle, coilBottomY, MonkeyRadius()
                );
            }
            minion.root.position += Vector3.down
                * minionFallSpeedU * Time.deltaTime;
            bool hasHit = (minion.root.position - monkey.position).magnitude
                < minionHitRadiusU;
            if (hasHit) {
                KnockDown(body.HeadCollider);
            }
            if (hasHit || minion.root.position.y < entryY - guardDepthU) {
                Destroy(minion.root.gameObject);
                minion.root = null;
                minion.isDone = true;
            } else {
                isAnyLeft = true;
            }
            minions[i] = minion;
        }
        if (!isAnyLeft) {
            NextAttack();
        }
    }

    // a small snake: three green beads in a column
    private Transform CreateMinion() {
        Transform root = new GameObject("Minion").transform;
        root.SetParent(transform, false);
        for (int i = 0; i < 3; i++) {
            Transform bead = CreatePrimitive(
                PrimitiveType.Sphere, "Bead" + i,
                Vector3.one * (0.7f - i * 0.12f), minionColor
            ).transform;
            bead.SetParent(root, false);
            bead.localPosition = Vector3.up * i * 0.55f;
        }
        return root;
    }

    // Hits On The Monkey  -----------------------------------------------------
    // a bite or a sweep: fatal, unless the gold shield takes it
    private void LethalHit() {
        if (hasShield) {
            hasShield = false;
            hud.PopText("Saved!", monkey.position, goldenColor);
            KnockDown(body.HeadCollider);
            return;
        }
        if (GameController.I != null) {
            GameController.I.LoseGame();
        }
    }

    // the usual stun & drop w/ the heavier shake, never fatal
    private static void KnockDown(Collider source) {
        HitBranchSensor sensor = HitBranchSensor.I;
        if (sensor != null && source != null) {
            sensor.ReportHit(
                source,
                HitBranchSensor.HitKind.FallingCobraController
            );
        }
    }

    // the climb is capped below the king, & a monkey at the cap is butted
    // down; too low, the king taunts
    private void UpdateWarnings() {
        float ceilingY = body.LowestY - ceilingGapU;
        if (monkeyController != null) {
            monkeyController.SetHeightCeiling(ceilingY);
        }
        float y = monkey.position.y;
        if (y < entryY - retreatLimitU) {
            Say(retreatLine);
        } else if (y > ceilingY - pushRangeU) {
            StartPush();
        }
    }

    private void OnKingTouched() {
        if (phase == Phase.Fighting) {
            StartPush();
        }
    }

    // no butt during an attack round, so it never delays an attack
    private void StartPush() {
        if (loop == Loop.Attack || body.IsStriking
            || Time.time < nextPushTime) {
            return;
        }
        nextPushTime = Time.time + pushCooldownS;
        // every butt says it, never held back by the speech cooldown
        hud.Say(approachLine);
        body.StartStrike(monkey.position, pushWindUpS, () => {
            if (monkey.position.y > body.LowestY - pushRangeU - 1.5f) {
                KnockDown(body.HeadCollider);
            }
        });
    }

    // Golden Banana  ----------------------------------------------------------
    private void TakeGoldenBanana() {
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStamina(stamina.MaxStamina);
        }
        hasShield = true;
        hud.PopText("Shield!", monkey.position, goldenColor);
    }

    // the armor follows the monkey; its back faces out from the trunk
    private void UpdateShieldRing() {
        if (armor == null) {
            return;
        }
        Vector3 outward = monkey.position - trunkCenter;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, outward);
        float shine = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
        Color gold = Color.Lerp(goldenColor, Color.white, shine * 0.35f);

        Vector3[] positions = {
            monkey.position + outward * armorOutU + Vector3.up * 0.15f,
            monkey.position + outward * (armorOutU * 0.6f) - side * 0.55f
                + Vector3.up * 0.6f,
            monkey.position + outward * (armorOutU * 0.6f) + side * 0.55f
                + Vector3.up * 0.6f,
            monkey.position + Vector3.up * armorHelmetU,
        };
        for (int i = 0; i < armor.Length; i++) {
            armor[i].gameObject.SetActive(hasShield);
            if (!hasShield) {
                continue;
            }
            armor[i].position = positions[i];
            armor[i].rotation = i == 0
                ? Quaternion.FromToRotation(Vector3.up, outward)
                : Quaternion.LookRotation(outward, Vector3.up);
            SetColor(armor[i].GetComponent<Renderer>(), gold);
        }
    }

    // Coconuts On The King  ---------------------------------------------------
    private void UpdateProjectiles() {
        Vector3 target = body.WeakSpotPosition;
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Transform projectile = projectiles[i];
            projectile.position = Vector3.MoveTowards(
                projectile.position,
                target,
                throwSpeedU * Time.deltaTime
            );
            projectile.Rotate(Vector3.right, 720f * Time.deltaTime);
            if ((projectile.position - target).sqrMagnitude > 0.3f) {
                continue;
            }
            Destroy(projectile.gameObject);
            projectiles.RemoveAt(i);
            OnJewelHit();
        }
    }

    // a coconut on the jewel always hurts
    private void OnJewelHit() {
        if (phase != Phase.Fighting) {
            return;
        }

        int lastThird = Third;
        int damage = balanceConfig.BossCoconutDamage;
        health = Mathf.Max(0, health - damage);
        body.FlashHit();
        hud.SetHealth(health / (float)balanceConfig.BossMaxHealth);
        hud.PopDamage("-" + damage, body.WeakSpotPosition);

        if (health <= 0) {
            Defeat();
            return;
        }
        if (!hasHit) {
            hasHit = true;
            Say(firstHitLine);
            StartRest();
        } else if (Third > lastThird) {
            // each lost third: more coconuts to throw & a few more webs
            AddCoconuts(coconutsPerThird);
            SpawnWebs(websPerThird);
            if (Third >= 2) {
                Say(finalThirdLine);
            }
        }
    }

    /// <returns>how many thirds of its health the king has lost: 0, 1, 2
    /// </returns>
    private int Third {
        get {
            float lost = 1f - health / (float)balanceConfig.BossMaxHealth;
            return Mathf.Clamp(Mathf.FloorToInt(lost * 3f), 0, 2);
        }
    }

    // Victory  ----------------------------------------------------------------
    private void Defeat() {
        phase = Phase.Defeated;
        ClearMarkers();
        ClearAttackObjects();
        if (monkeyController != null) {
            monkeyController.ClearHeightCeiling();
        }
        hud.SetPrompt(false, Vector3.zero);
        hud.SetHealth(0f);
        Say(defeatLine);

        crown = body.Shatter();
        if (crown != null) {
            crownStart = crown.position;
            crownStartScale = crown.localScale;
        }
        victoryTimeS = 0f;
        if (Debug.isDebugBuild) {
            Debug.Log("BossFightController:\tCobra King defeated", this);
        }
    }

    // the crown flies in an arc onto the monkey's head, then the run is won
    private void UpdateVictory() {
        victoryTimeS += Time.deltaTime;
        if (crown != null) {
            float t = Mathf.Clamp01(victoryTimeS / crownFlyS);
            Vector3 end = monkey.position + Vector3.up * crownAboveMonkeyU;
            Vector3 position = Vector3.Lerp(crownStart, end, t)
                + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.5f;
            crown.position = position;
            crown.localScale = crownStartScale
                * Mathf.Lerp(1.4f, crownScaleOnMonkey, t);
            crown.rotation = Quaternion.Euler(0f, t * 720f, 0f);
            if (t >= 1f) {
                crown.SetParent(monkey, true);
            }
        }
        if (!hasWon && victoryTimeS >= crownFlyS + victoryDelayS) {
            hasWon = true;
            if (GameController.I != null) {
                GameController.I.WinGame();
            }
            ScoreManager score = ScoreManager.I;
            hud.ShowVictory(score != null ? score.TotalScore : 0);
        }
    }

    // Hint, Fruits, Camera  ---------------------------------------------------
    private void UpdateFirstThrowHint() {
        ArenaFruit first = fruits.Count > 0 ? fruits[0] : default;
        bool isShown = !hasHit && first.root != null && first.root.activeSelf;
        Vector3 from = first.root != null
            ? first.root.transform.position
            : Vector3.zero;
        hud.SetPrompt(isShown, from);
        if (dots == null) {
            return;
        }

        Vector3 to = body.WeakSpotPosition;
        Vector3 outward = from - trunkCenter;
        outward.y = 0f;
        Vector3 control = (from + to) * 0.5f + outward.normalized * 3f;
        float wave = Time.time * 1.5f;
        for (int i = 0; i < dots.Length; i++) {
            dots[i].gameObject.SetActive(isShown);
            if (!isShown) {
                continue;
            }
            float t = (i + 1f) / (dots.Length + 1f);
            float u = 1f - t;
            dots[i].position = u * u * from + 2f * u * t * control + t * t * to;
            // a pulse runs along the line toward the king
            float pulse = Mathf.Repeat(t - wave, 1f);
            dots[i].localScale = Vector3.one * dotRadiusU * 2f
                * Mathf.Lerp(1.6f, 0.8f, pulse);
        }
    }

    // taken fruit grows back; the one the monkey would reach turns lighter
    private void UpdateFruits() {
        Collider target = UpwardFruitSensor.I != null
            ? UpwardFruitSensor.I.ReachTarget
            : null;
        for (int i = 0; i < fruits.Count; i++) {
            ArenaFruit fruit = fruits[i];
            if (!fruit.root.activeSelf) {
                if (!fruit.isGolden && phase == Phase.Fighting
                    && fruit.respawnTime >= 0f
                    && Time.time >= fruit.respawnTime) {
                    fruit.root.SetActive(true);
                    fruit.respawnTime = -1f;
                    fruits[i] = fruit;
                }
                continue;
            }
            Color baseColor = fruit.isGolden ? goldenColor : coconutColor;
            SetColor(
                fruit.renderer,
                fruit.trigger == target
                    ? Color.Lerp(baseColor, fruitTargetedColor, 0.6f)
                    : baseColor
            );
        }
    }

    // the camera aims up between the monkey & the king, so both stay in view
    private void UpdateCameraFocus() {
        if (cameraLookUp == null) {
            return;
        }
        bool isFighting = phase == Phase.Fighting;
        float focus = isFighting
            ? Mathf.Clamp(
                (body.HeadPosition.y - monkey.position.y) * cameraFocusShare,
                0f,
                maxCameraFocusU
            )
            : 0f;
        cameraLookUp.ExtraAimOffsetU = focus;
        // pulled back through the fight & the crown's flight
        cameraLookUp.ViewDistanceScale = phase != Phase.Waiting
            ? cameraDistanceScale
            : 1f;
    }

    // Markers & Helpers  ------------------------------------------------------
    // a danger zone flat on the trunk, facing out: a dark red rim whose
    // bright red center fills it as the hit nears, w/ a hud sign if worded
    private void AddTrunkZone(
        float angle,
        float y,
        float size,
        string word,
        bool hasRimSign
    ) {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 outward = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        Quaternion facing = Quaternion.FromToRotation(Vector3.up, outward);

        GameObject rim = CreatePrimitive(
            PrimitiveType.Cylinder, "ZoneRim",
            new Vector3(size, 0.02f, size), warningDimColor
        );
        rim.transform.position = PointOnTrunk(angle, y, trunkRadiusU + 0.04f);
        rim.transform.rotation = facing;

        GameObject fill = CreatePrimitive(
            PrimitiveType.Cylinder, "ZoneFill",
            new Vector3(0.01f, 0.03f, 0.01f), warningColor
        );
        fill.transform.position = PointOnTrunk(angle, y, trunkRadiusU + 0.07f);
        fill.transform.rotation = facing;

        int warning = word != null
            ? hud.AddWarning(rim.transform.position, word)
            : -1;
        markers.Add(new Marker {
            rim = rim.transform,
            fill = fill.transform,
            size = size,
            warning = warning,
            isBand = false
        });
    }

    // fills every zone to progress 0~1; the band grows from its height
    private void UpdateMarkers(float progress, bool isSweep) {
        progress = Mathf.Clamp01(progress);
        foreach (Marker marker in markers) {
            if (marker.fill == null) {
                continue;
            }
            if (marker.isBand) {
                marker.fill.localScale = new Vector3(
                    (trunkRadiusU + 0.08f) * 2f,
                    Mathf.Max(0.01f, progress * sweepBandHalfU),
                    (trunkRadiusU + 0.08f) * 2f
                );
            } else {
                float size = marker.size * Mathf.Max(0.02f, progress);
                marker.fill.localScale = new Vector3(size, 0.03f, size);
            }
            if (marker.warning >= 0) {
                Vector3 signAt = isSweep && marker.isBand
                    ? SweepSignPoint()
                    : marker.rim != null
                        ? marker.rim.position
                        : marker.fill.position;
                hud.UpdateWarning(marker.warning, signAt, progress);
            }
        }
    }

    private void ClearMarkers() {
        foreach (Marker marker in markers) {
            if (marker.rim != null) {
                Destroy(marker.rim.gameObject);
            }
            if (marker.fill != null) {
                Destroy(marker.fill.gameObject);
            }
        }
        markers.Clear();
        hud.ClearWarnings();
    }

    private void ClearAttackObjects() {
        if (tail != null) {
            foreach (Transform bead in tail) {
                Destroy(bead.gameObject);
            }
            tail = null;
        }
        foreach (Minion minion in minions) {
            if (minion.root != null) {
                Destroy(minion.root.gameObject);
            }
        }
        minions.Clear();
        foreach (Transform projectile in projectiles) {
            Destroy(projectile.gameObject);
        }
        projectiles.Clear();
    }

    private void Say(string line) {
        if (line == lastLine && Time.time < nextSameLineTime) {
            return;
        }
        lastLine = line;
        nextSameLineTime = Time.time + speechCooldownS;
        hud.Say(line);
    }

    // horizontal distance from the trunk axis to the monkey
    private float MonkeyRadius() {
        Vector3 offset = monkey.position - trunkCenter;
        offset.y = 0f;
        return offset.magnitude;
    }

    private float AngleOf(Vector3 position) {
        Vector3 offset = position - trunkCenter;
        return Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    private Vector3 PointOnTrunk(float angleDeg, float y, float radius) {
        float radians = angleDeg * Mathf.Deg2Rad;
        return new Vector3(
            trunkCenter.x + Mathf.Cos(radians) * radius,
            y,
            trunkCenter.z + Mathf.Sin(radians) * radius
        );
    }

    // a decor primitive in the king's lit material, no collider
    private GameObject CreatePrimitive(
        PrimitiveType type,
        string objectName,
        Vector3 scale,
        Color color
    ) {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = objectName;
        part.transform.SetParent(transform, true);
        part.transform.localScale = scale;
        DestroyImmediate(part.GetComponent<Collider>());
        Renderer partRenderer = part.GetComponent<Renderer>();
        if (body.PrimitiveMaterial != null) {
            partRenderer.sharedMaterial = body.PrimitiveMaterial;
        }
        SetColor(partRenderer, color);
        return part;
    }

    private void SetColor(Renderer target, Color color) {
        if (target == null) {
            return;
        }
        target.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BASE_COLOR, color);
        colorBlock.SetColor(COLOR, color);
        target.SetPropertyBlock(colorBlock);
    }

    // private members  ########################################################
    private const string FRUIT_TAG = "FruitCollider";
    private const string BRANCH_TAG = "Branch";
    // the BranchWithFruit prefab's plank & banana spot, under its root
    private static readonly Quaternion BRANCH_LOCAL_ROTATION =
        new Quaternion(-0.122787856f, 0.6963643f, -0.122787856f, 0.6963643f);
    private static readonly Vector3 BRANCH_LOCAL_SCALE =
        new Vector3(5f, 0.7f, 0.5f);
    private static readonly Vector3 FRUIT_LOCAL_SPOT =
        new Vector3(0f, -1f, 2.149f);
    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR = Shader.PropertyToID("_Color");

    private enum Phase {
        Waiting, // climbing to the trigger height
        Fighting,
        Defeated,
    }

    private enum Loop {
        Intro, // before the first hit: no attacks
        Rest, // a pause between attack rounds
        Attack, // a round of attacks
    }

    private enum Attack {
        Bite,
        Sweep,
        Minions,
    }

    private struct ArenaFruit {
        public GameObject root;
        public Collider trigger;
        public Renderer renderer;
        public bool isGolden;
        public GameObject[] stub; // its branch stub & stem
        public float respawnTime; // Time.time it grows back, -1 if hanging
    }

    private struct Marker {
        public Transform rim; // full-size dark disc, null for the band
        public Transform fill; // grows with the warning's progress
        public float size; // disc diameter; u
        public int warning; // hud warning id, -1 if none
        public bool isBand; // the sweep's band round the trunk
    }

    private struct Minion {
        public float angle; // round the trunk; deg
        public Transform root; // null until it falls
        public bool isDone;
    }

    private Phase phase = Phase.Waiting;
    private Loop loop;
    private Attack attack;
    private readonly Queue<Attack> attackQueue = new Queue<Attack>();
    private float loopTimeS; // rest time left
    private float attackTimeS; // time into the running attack
    private float lockAngle; // monkey angle when the attack began; deg
    private float lockY; // monkey y when the attack began
    private bool biteStarted;
    private Vector3 biteSpot; // world point the running bite dives at
    private bool tailHit;
    private int health;
    private bool hasHit; // the monkey has landed a first hit
    private bool hasShield; // the golden banana's shield is up
    private bool hasWon;
    private readonly List<SpiderWebTrap> webs = new List<SpiderWebTrap>();
    private int goldenIndex = -1; // the golden banana in the fruit list
    private float nextGoldenTime; // Time.time the next visit starts
    private float goldenHideTime; // Time.time the current visit ends
    private float victoryTimeS;
    private float entryY; // monkey's y when the fight began
    private float coilBottomY; // world y of the king's coil
    private Vector3 trunkCenter;
    private string lastLine;
    private float nextSameLineTime;
    private float nextPushTime; // Time.time b4 which no new butt starts
    private Vector3 crownStart;
    private Vector3 crownStartScale;
    private readonly List<ArenaFruit> fruits = new List<ArenaFruit>();
    private readonly List<Transform> projectiles = new List<Transform>();
    private readonly List<Marker> markers = new List<Marker>();
    private readonly List<Minion> minions = new List<Minion>();
    private Transform[] tail;
    private Transform[] dots;
    private Transform[] armor;

    // cached references  ------------------------------------------------------
    private BossKingBody body;
    private BossFightHud hud;
    private Transform monkey;
    private MonkeyController monkeyController;
    private Transform crown;
    private CobraMotor greatCobra;
    private CameraLookUp cameraLookUp;
    private MaterialPropertyBlock colorBlock;
}
