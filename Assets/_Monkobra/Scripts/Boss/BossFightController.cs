using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the boss fight against the Cobra King. Once the monkey has climbed
/// <see cref="GameBalanceConfig.BossTriggerHeightU"/> the screen flashes and
/// the run enters the boss arena: the king coils round the tree top above,
/// the great cobra stops chasing and guards below, and the spawners of webs,
/// falling cobras and wet bark stop. Banana branches grow on the trunk in
/// between; a grabbed banana still feeds the monkey and is also thrown in an
/// arc at the king's crown by itself, so the fight uses only the grab the
/// player already knows.
/// <para>
/// The fight loops: the king rests, then attacks; a banana hurts it at any
/// time. Before the first hit it only rests, and the first banana carries a
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

    /// <summary>
    /// Called by <see cref="ArmController"/> on every grab: in the arena a
    /// grabbed banana still feeds the monkey, & is thrown at the king too;
    /// the golden banana gives its shield instead.
    /// </summary>
    /// <returns>true if the grab was handled here, so the arm skips its own
    /// fly-to-the-bar effect</returns>
    public bool TryThrow(Collider fruit, Vector3 from) {
        if (phase != Phase.Fighting || fruit == null
            || !fruit.CompareTag(FRUIT_TAG)) {
            return false;
        }

        int index = fruits.FindIndex(f => f.trigger == fruit);
        if (index >= 0) {
            ArenaFruit picked = fruits[index];
            if (picked.isGolden) {
                HideGolden();
                nextGoldenTime = Time.time + goldenIntervalS;
                TakeGoldenBanana();
                return true;
            }
            picked.respawnTime = Time.time
                + bananaRespawnS * BananaRespawnScale;
            fruits[index] = picked;
        }

        // a banana on the tree's own branches flies too
        StaminaBarController stamina = StaminaBarController.I;
        if (stamina != null) {
            stamina.AddStaminaByFruit();
        }
        LaunchBanana(fruit.gameObject, from);
        return true;
    }

    // a copy of the grabbed banana arcs up at the king, spinning, with a
    // trail of fading dots, so the throw reads at a glance
    private void LaunchBanana(GameObject source, Vector3 from) {
        GameObject copy = Instantiate(source, transform, true);
        // a bare look-alike: no scripts or colliders wake up on it
        foreach (MonoBehaviour script in
            copy.GetComponentsInChildren<MonoBehaviour>(true)) {
            DestroyImmediate(script);
        }
        foreach (Collider part in copy.GetComponentsInChildren<Collider>(true)) {
            DestroyImmediate(part);
        }
        copy.transform.position = from;
        copy.transform.localScale = source.transform.lossyScale
            * thrownBananaScale;
        copy.SetActive(true);
        projectiles.Add(new Projectile {
            transform = copy.transform,
            start = from,
            ageS = 0f,
            nextTrailS = 0f
        });
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("balance numbers: trigger height, king health, banana damage")]
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
    private float cameraDistanceScale = 2f;

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
    [Tooltip("small snakes per wave, one of them left out as the gap")]
    private int minionCount = 5;

    [SerializeField]
    [Tooltip("angle between neighbouring minions round the trunk; deg")]
    private float minionSpreadDeg = 28f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("delay between neighbouring minions in a wave, so the wave "
        + "sweeps across; s")]
    private float minionStaggerS = 0.18f;


    [SerializeField]
    [Min(1)]
    [Tooltip("waves per minion attack, each re-aimed at the monkey w/ a new "
        + "gap")]
    private int minionWaves = 2;


    [SerializeField]
    [Tooltip("drop spots are marked this long before the minions fall; s")]
    private float minionWarnS = 0.9f;

    [SerializeField]
    [Tooltip("minion fall speed; u/s")]
    private float minionFallSpeedU = 9f;

    [SerializeField]
    [Tooltip("a minion this close knocks the monkey down; u")]
    private float minionHitRadiusU = 1.1f;

    [SerializeField]
    [Tooltip("a fall line runs this far below the monkey's height; u")]
    private float minionStripBelowU = 3f;

    [Header("Spider Webs")]
    [SerializeField]
    [Tooltip("the web prefab the tree segments use")]
    private SpiderWebTrap webPrefab;

    [SerializeField]
    [Min(0)]
    [Tooltip("webs on the trunk when the fight begins")]
    private int webCount = 4;

    [SerializeField]
    [Min(0)]
    [Tooltip("webs added each time the king loses another third of its "
        + "health")]
    private int websPerThird = 2;

    [SerializeField]
    [Tooltip("webs may hang this far below the entry, as low as a knocked-"
        + "down monkey; u")]
    private float webBelowEntryU = 4f;

    [SerializeField]
    [Tooltip("in the arena each escape press counts this many times the "
        + "normal, so a web is shaken off within the king's attack gaps")]
    private float webEscapeGainScale = 2f;

    [SerializeField]
    [Tooltip("webs keep this far round the trunk from any fruit; deg")]
    private float webFruitGapDeg = 25f;


    [Header("Bananas")]
    [SerializeField]
    [Tooltip("the tree segments' banana branch prefab")]
    private GameObject bananaBranchPrefab;

    [SerializeField]
    [Min(1)]
    [Tooltip("banana branches in the arena")]
    private int bananaCount = 3;

    [SerializeField]
    [Min(0)]
    [Tooltip("banana branches added each time the king loses another third "
        + "of its health")]
    private int bananasPerThird = 1;

    [SerializeField]
    [Tooltip("lowest & highest banana height above the entry; u")]
    private Vector2 fruitHeightRangeU = new Vector2(2f, 5.5f);

    [SerializeField]
    [Tooltip("radius of a fruit branch's root, as the tree segments' "
        + "BranchFruitPlacementConfig.branchRadius; u")]
    private float branchRadiusU = 6.6f;

    [SerializeField]
    [Tooltip("a taken banana grows back on its branch after this; s")]
    private float bananaRespawnS = 6f;

    [SerializeField]
    [Tooltip("banana grow-back time as a share of the normal, once 1 and 2 "
        + "thirds of the king's health are lost")]
    private Vector2 bananaRespawnScaleByThird = new Vector2(0.75f, 0.5f);

    [Header("Throw")]
    [SerializeField]
    [Tooltip("time a thrown banana takes to reach the crown; s")]
    private float throwTimeS = 0.7f;

    [SerializeField]
    [Tooltip("how high the throw arcs over the straight line; u")]
    private float throwArcU = 3f;

    [SerializeField]
    [Tooltip("thrown banana size as a share of the grabbed one")]
    private float thrownBananaScale = 1.6f;

    [SerializeField]
    private Color trailColor = new Color(1f, 0.85f, 0.2f);

    [Header("Golden Banana")]
    [SerializeField]
    [Tooltip("outline material copied for the golden banana's thick border, "
        + "eg the bananas' grabbable outline")]
    private Material goldenOutlineMaterial;

    [SerializeField]
    private Color goldenOutlineColor = new Color(1f, 0.75f, 0f);

    [SerializeField]
    [Tooltip("golden banana outline width")]
    private float goldenOutlineWidth = 0.25f;

    [SerializeField]
    [Tooltip("the golden banana first shows up this long after the first "
        + "hit on the king; s")]
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

    [Header("Finisher")]
    [SerializeField]
    [Tooltip("time scale of the last blow's slow motion, either way")]
    private float finisherTimeScale = 0.2f;

    [SerializeField]
    [Tooltip("real time the slow motion lasts; s")]
    private float finisherRealS = 1.4f;

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
    [Tooltip("dots from the first banana to the crown")]
    private int dotCount = 12;

    [SerializeField]
    [Tooltip("dot radius; u")]
    private float dotRadiusU = 0.12f;

    [Header("Colors")]


    [SerializeField]
    private Color goldenColor = new Color(1f, 0.82f, 0.15f);

    [SerializeField]
    private Color warningColor = new Color(1f, 0.15f, 0.15f);

    [SerializeField]
    private Color warningDimColor = new Color(0.45f, 0.05f, 0.05f);



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
    [Tooltip("said once a game, the first time the minions come")]
    private string minionCallLine = "Minions, help me!";


    [SerializeField]
    [Tooltip("the same line is not repeated within this; s")]
    private float speechCooldownS = 4f;

    [SerializeField]
    [Tooltip("a key line, eg the minion call, holds the bubble this long; "
        + "s")]
    private float keyLineHoldS = 2.2f;

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

        if (bananaBranchPrefab == null) {
            Debug.LogError(
                "BossFightController:\tmust assign Inspector Field: "
                    + "bananaBranchPrefab",
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
        if (phase == Phase.Finishing) {
            Time.timeScale = 1f;
        }
        if (struggle != null) {
            struggle.EscapeGainScale = 1f;
        }
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
        } else if (phase == Phase.Finishing) {
            UpdateFinisher();
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
        struggle = FindFirstObjectByType<SpiderWebStruggle>();
        if (struggle != null) {
            struggle.EscapeGainScale = webEscapeGainScale;
        }
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
        for (int i = 0; i < bananaCount; i++) {
            // spread round the trunk, the first one right above the monkey
            float t = bananaCount > 1 ? i / (bananaCount - 1f) : 0f;
            AddFruit(
                startAngle + i * 360f / bananaCount,
                entryY + Mathf.Lerp(
                    fruitHeightRangeU.x, fruitHeightRangeU.y, t
                ),
                false
            );
        }
        // the golden banana starts hidden & shows up once the fight is on
        goldenIndex = AddFruit(startAngle, entryY, true);
        HideGolden();
    }

    // extra banana branches at random spots round the trunk
    private void AddBananas(int count) {
        for (int i = 0; i < count; i++) {
            AddFruit(
                Random.Range(0f, 360f),
                entryY + Random.Range(fruitHeightRangeU.x, fruitHeightRangeU.y),
                false
            );
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
                float y = Random.Range(entryY - webBelowEntryU, Mathf.Max(entryY + 1f, topY));
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

    private float BananaRespawnScale => Third >= 2
        ? bananaRespawnScaleByThird.y
        : Third >= 1
            ? bananaRespawnScaleByThird.x
            : 1f;

    /// <returns>the new fruit's index in the fruit list</returns>
    // a real banana branch, as the tree segments grow, always bearing; the
    // golden one is the same banana tinted gold w/ a thick gold outline
    private int AddFruit(float angle, float y, bool isGolden) {
        GameObject branch = Instantiate(bananaBranchPrefab, transform);
        PlaceBranch(branch.transform, angle, y);
        BranchController controller = branch.GetComponent<BranchController>();
        if (controller != null) {
            controller.Initialize(1f);
        }
        ScorePickup pickup = branch.GetComponentInChildren<ScorePickup>(true);
        GameObject banana = pickup != null ? pickup.gameObject : branch;
        if (isGolden) {
            MakeGolden(banana);
        }

        fruits.Add(new ArenaFruit {
            root = banana,
            branch = branch,
            trigger = banana.GetComponent<Collider>(),
            isGolden = isGolden,
            respawnTime = -1f
        });
        return fruits.Count - 1;
    }

    // the branch root sits on the segments' branch ring, its +z away from the
    // trunk, high enough that the banana hangs about the given height
    private void PlaceBranch(Transform branch, float angle, float fruitY) {
        branch.SetPositionAndRotation(
            PointOnTrunk(angle, fruitY + BANANA_HANG_U, branchRadiusU),
            Quaternion.Euler(0f, 90f - angle, 0f)
        );
    }

    private void MakeGolden(GameObject banana) {
        // its own outline replaces the reach-target one
        foreach (FruitOutlineListener listener in
            banana.GetComponentsInChildren<FruitOutlineListener>(true)) {
            Destroy(listener);
        }
        Material outline = null;
        if (goldenOutlineMaterial != null) {
            outline = new Material(goldenOutlineMaterial);
            outline.SetColor(OUTLINE_COLOR, goldenOutlineColor);
            outline.SetFloat(OUTLINE_WIDTH, goldenOutlineWidth);
        }
        foreach (Renderer part in banana.GetComponentsInChildren<Renderer>(true)) {
            Material baseMaterial = part.sharedMaterial;
            if (outline != null) {
                part.sharedMaterials = new[] { baseMaterial, outline };
            }
            SetColor(part, goldenColor);
        }
    }

    // now & then the golden banana grows in sight, beside the monkey, for a
    // short while; only once the first banana has hit the king
    private void UpdateGolden() {
        if (goldenIndex < 0 || phase != Phase.Fighting || !hasHit) {
            return;
        }
        ArenaFruit golden = fruits[goldenIndex];
        if (golden.branch.activeSelf) {
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
            entryY + fruitHeightRangeU.x,
            body.LowestY - ceilingGapU - 0.5f
        );
        PlaceBranch(golden.branch.transform, angle, y);
        golden.branch.SetActive(true);
        golden.root.SetActive(true);
        goldenHideTime = Time.time + goldenShowS;
        hud.PopText(
            "Golden banana!", golden.root.transform.position, goldenColor
        );
    }

    private void HideGolden() {
        fruits[goldenIndex].branch.SetActive(false);
    }



    // dotted line from the first banana to the crown, until the 1st hit
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

    // Minions: waves of small snakes fall one after another across the
    // monkey's side of the trunk, each wave leaving one gap to dodge into
    private void StartMinions() {
        // the king calls its minions once a game, the first time they come
        if (!hasCalledMinions) {
            hasCalledMinions = true;
            SayNow(minionCallLine, true);
        }
        waveIndex = 0;
        StartWave();
    }


    private void StartWave() {
        ClearMarkers();
        minions.Clear();
        waveTimeS = 0f;
        lockAngle = AngleOf(monkey.position);
        lockY = monkey.position.y;

        // a gap never at either end, & the sweep runs either way
        int gap = Random.Range(1, Mathf.Max(2, minionCount - 1));
        float direction = Random.value < 0.5f ? -1f : 1f;
        float warn = minionWarnS * WarnScale;
        int signAt = minionCount / 2 == gap ? gap + 1 : minionCount / 2;
        int order = 0;
        for (int i = 0; i < minionCount; i++) {
            float angle = lockAngle
                + direction * (i - (minionCount - 1) * 0.5f) * minionSpreadDeg;
            bool isGap = i == gap;
            if (!isGap) {
                // the same filling danger zone as the first version; one
                // LOOK UP! sign for the whole wave, on a middle zone
                AddTrunkStrip(
                    angle,
                    coilBottomY,
                    lockY - minionStripBelowU,
                    minionHitRadiusU * 1.4f,
                    i == signAt ? "LOOK UP!" : null
                );
            }
            minions.Add(new Minion {
                angle = angle,
                root = null,
                isDone = isGap,
                dropDelayS = warn + order * minionStaggerS
            });
            if (!isGap) {
                order++;
            }
        }
    }



    private void UpdateMinions() {
        waveTimeS += Time.deltaTime;
        float warn = minionWarnS * WarnScale;
        if (waveTimeS < warn) {
            UpdateMarkers(waveTimeS / warn, false);
        }
        bool isAnyLeft = false;
        for (int i = 0; i < minions.Count; i++) {
            Minion minion = minions[i];
            if (minion.isDone) {
                continue;
            }
            isAnyLeft = true;
            if (waveTimeS < minion.dropDelayS) {
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
                // a minion is a snake too: its touch is fatal, unless the
                // gold shield takes it
                LethalHit();
            }
            if (hasHit || minion.root.position.y < entryY - guardDepthU) {
                Destroy(minion.root.gameObject);
                minion.root = null;
                minion.isDone = true;
            }
            minions[i] = minion;
        }
        if (isAnyLeft) {
            return;
        }
        waveIndex++;
        if (waveIndex < minionWaves) {
            StartWave();
        } else {
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

    // a bite, a sweep or a minion: fatal, unless the gold shield takes it
    private void LethalHit() {
        if (phase != Phase.Fighting) {
            return;
        }
        if (hasShield) {
            hasShield = false;
            hud.PopText("Saved!", monkey.position, goldenColor);
            KnockDown(body.HeadCollider);
            return;
        }
        StartFinisher(false);
    }

    // Finisher  ---------------------------------------------------------------
    // the last blow either way plays in slow motion: a win flashes the king
    // white before it shatters; a loss flashes red & spins the monkey off the
    // tree under a K.O. banner, then the run is lost
    private void StartFinisher(bool isWin) {
        phase = Phase.Finishing;
        isFinisherWin = isWin;
        finisherTimeS = 0f;
        nextFinisherFlashS = 0f;
        ClearMarkers();
        hud.SetPrompt(false, Vector3.zero);
        Time.timeScale = finisherTimeScale;

        if (isWin) {
            hud.Flash(Color.white);
            return;
        }
        hud.Flash(new Color(0.9f, 0.1f, 0.1f));
        hud.ShowBanner("K.O.", new Color(0.95f, 0.15f, 0.15f));
        if (monkeyController != null) {
            monkeyController.ClearHeightCeiling();
            Vector3 outward = monkey.position - trunkCenter;
            outward.y = 0f;
            monkeyController.IsHeld = true;
            monkeyController.PullTo(
                monkey.position + outward.normalized * 6f + Vector3.down * 3f
            );
        }
    }

    // runs on unscaled time, since the game itself crawls meanwhile
    private void UpdateFinisher() {
        finisherTimeS += Time.unscaledDeltaTime;
        if (isFinisherWin) {
            // the king keeps flashing as it reels
            if (finisherTimeS >= nextFinisherFlashS) {
                nextFinisherFlashS = finisherTimeS + 0.18f;
                body.FlashHit();
            }
        } else {
            monkey.Rotate(Vector3.up, 900f * Time.unscaledDeltaTime);
        }
        if (finisherTimeS < finisherRealS) {
            return;
        }

        Time.timeScale = 1f;
        if (isFinisherWin) {
            Defeat();
            return;
        }
        phase = Phase.Over;
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

    // any time the monkey gets too close the king shoves it off & says so:
    // a head butt when its head is free, else at once
    private void StartPush() {
        if (Time.time < nextPushTime) {
            return;
        }
        nextPushTime = Time.time + pushCooldownS;
        SayNow(approachLine, false);
        if (body.IsStriking) {
            KnockDown(body.HeadCollider);
            return;
        }
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

    // thrown bananas follow an arc onto the crown, then burst on impact
    private void UpdateProjectiles() {
        Vector3 target = body.WeakSpotPosition;
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile thrown = projectiles[i];
            if (thrown.transform == null) {
                projectiles.RemoveAt(i);
                continue;
            }
            thrown.ageS += Time.deltaTime;
            float t = Mathf.Clamp01(thrown.ageS / throwTimeS);
            Vector3 position = Vector3.Lerp(thrown.start, target, t)
                + Vector3.up * Mathf.Sin(t * Mathf.PI) * throwArcU;
            thrown.transform.position = position;
            thrown.transform.Rotate(Vector3.forward, 900f * Time.deltaTime);

            // a dot every few hundredths of a second marks the path
            if (thrown.ageS >= thrown.nextTrailS) {
                thrown.nextTrailS = thrown.ageS + 0.035f;
                AddTrailDot(position);
            }
            projectiles[i] = thrown;
            if (t < 1f) {
                continue;
            }
            Destroy(thrown.transform.gameObject);
            projectiles.RemoveAt(i);
            BurstAt(target);
            OnJewelHit();
        }
        UpdateSparks();
    }

    private void AddTrailDot(Vector3 position) {
        Transform dot = CreatePrimitive(
            PrimitiveType.Sphere, "TrailDot", Vector3.one * 0.22f, trailColor
        ).transform;
        dot.position = position;
        sparks.Add(new Spark {
            transform = dot,
            velocity = Vector3.zero,
            size = 0.22f,
            ageS = 0f,
            lifeS = 0.35f
        });
    }

    // yellow cubes fly out where a banana lands
    private void BurstAt(Vector3 position) {
        for (int i = 0; i < 10; i++) {
            Transform cube = CreatePrimitive(
                PrimitiveType.Cube, "Burst", Vector3.one * 0.25f, trailColor
            ).transform;
            cube.position = position;
            cube.rotation = Random.rotation;
            sparks.Add(new Spark {
                transform = cube,
                velocity = Random.insideUnitSphere * 7f + Vector3.up * 2f,
                size = 0.25f,
                ageS = 0f,
                lifeS = 0.6f
            });
        }
    }

    private void UpdateSparks() {
        for (int i = sparks.Count - 1; i >= 0; i--) {
            Spark spark = sparks[i];
            spark.ageS += Time.deltaTime;
            if (spark.transform == null || spark.ageS >= spark.lifeS) {
                if (spark.transform != null) {
                    Destroy(spark.transform.gameObject);
                }
                sparks.RemoveAt(i);
                continue;
            }
            spark.velocity += Physics.gravity * 0.5f * Time.deltaTime;
            spark.transform.position += spark.velocity * Time.deltaTime;
            spark.transform.localScale = Vector3.one * spark.size
                * (1f - spark.ageS / spark.lifeS);
            sparks[i] = spark;
        }
    }

    // a banana on the crown always hurts
    private void OnJewelHit() {
        if (phase != Phase.Fighting) {
            return;
        }

        int lastThird = Third;
        int damage = balanceConfig.BossBananaDamage;
        health = Mathf.Max(0, health - damage);
        body.FlashHit();
        hud.SetHealth(health / (float)balanceConfig.BossMaxHealth);
        hud.PopDamage("-" + damage, body.WeakSpotPosition);

        if (health <= 0) {
            StartFinisher(true);
            return;
        }
        if (!hasHit) {
            hasHit = true;
            Say(firstHitLine);
            nextGoldenTime = Time.time + goldenFirstDelayS;
            StartRest();
        } else if (Third > lastThird) {
            // each lost third: more bananas to throw & more webs
            AddBananas(bananasPerThird);
            SpawnWebs(websPerThird);
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

    // a taken banana grows back on its branch
    private void UpdateFruits() {
        for (int i = 0; i < fruits.Count; i++) {
            ArenaFruit fruit = fruits[i];
            if (fruit.isGolden || fruit.root.activeSelf
                || fruit.respawnTime < 0f || Time.time < fruit.respawnTime) {
                continue;
            }
            fruit.root.SetActive(true);
            fruit.respawnTime = -1f;
            fruits[i] = fruit;
        }
    }

    // the camera aims up between the monkey & the king, so both stay in view
    private void UpdateCameraFocus() {
        if (cameraLookUp == null) {
            return;
        }
        bool isFighting = phase == Phase.Fighting
            || phase == Phase.Finishing;
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

    // a minion's fall line: a dark red strip down the trunk from the king's
    // coil, filling top-down in bright red as the drop nears
    private void AddTrunkStrip(
        float angle,
        float topY,
        float bottomY,
        float width,
        string word
    ) {
        float length = Mathf.Max(0.5f, topY - bottomY);
        float radians = angle * Mathf.Deg2Rad;
        Vector3 outward = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        Quaternion facing = Quaternion.LookRotation(outward, Vector3.up);

        GameObject rim = CreatePrimitive(
            PrimitiveType.Cube, "StripRim",
            new Vector3(width, length, 0.04f), warningDimColor
        );
        rim.transform.SetPositionAndRotation(
            PointOnTrunk(angle, topY - length * 0.5f, trunkRadiusU + 0.04f),
            facing
        );
        GameObject fill = CreatePrimitive(
            PrimitiveType.Cube, "StripFill",
            new Vector3(width * 0.8f, 0.01f, 0.06f), warningColor
        );
        fill.transform.SetPositionAndRotation(
            PointOnTrunk(angle, topY, trunkRadiusU + 0.07f),
            facing
        );

        int warning = word != null
            ? hud.AddWarning(
                PointOnTrunk(angle, bottomY + length * 0.35f, trunkRadiusU),
                word
            )
            : -1;
        markers.Add(new Marker {
            rim = rim.transform,
            fill = fill.transform,
            size = width,
            warning = warning,
            isBand = false,
            isStrip = true,
            topY = topY,
            length = length,
            angle = angle
        });
    }

    // fills every zone to progress 0~1; the band grows from its height
    private void UpdateMarkers(float progress, bool isSweep) {
        progress = Mathf.Clamp01(progress);
        foreach (Marker marker in markers) {
            if (marker.fill == null) {
                continue;
            }
            if (marker.isStrip) {
                float filled = Mathf.Max(0.01f, marker.length * progress);
                marker.fill.localScale = new Vector3(
                    marker.size * 0.8f, filled, 0.06f
                );
                marker.fill.position = PointOnTrunk(
                    marker.angle,
                    marker.topY - filled * 0.5f,
                    trunkRadiusU + 0.07f
                );
                if (marker.warning >= 0) {
                    hud.UpdateWarning(
                        marker.warning,
                        PointOnTrunk(
                            marker.angle,
                            marker.topY - marker.length * 0.65f,
                            trunkRadiusU
                        ),
                        progress
                    );
                }
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
        foreach (Projectile thrown in projectiles) {
            if (thrown.transform != null) {
                Destroy(thrown.transform.gameObject);
            }
        }
        projectiles.Clear();
    }

    private void Say(string line) {
        if (line == lastLine && Time.time < nextSameLineTime) {
            return;
        }
        SayNow(line, false);
    }

    // a key line holds the bubble for a while, so no other line cuts it off
    private void SayNow(string line, bool isKeyLine) {
        if (Time.time < keyLineEndTime) {
            return;
        }
        lastLine = line;
        nextSameLineTime = Time.time + speechCooldownS;
        if (isKeyLine) {
            keyLineEndTime = Time.time + keyLineHoldS;
        }
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
    // the banana hangs about this far below its branch root; u
    private const float BANANA_HANG_U = 1f;
    private static readonly int OUTLINE_COLOR =
        Shader.PropertyToID("_Outline_Color");
    private static readonly int OUTLINE_WIDTH =
        Shader.PropertyToID("_Outline_Width");
    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR = Shader.PropertyToID("_Color");

    private enum Phase {
        Waiting, // climbing to the trigger height
        Fighting,
        Finishing, // the last blow, in slow motion
        Defeated, // the king beaten, the crown on its way
        Over, // the monkey beaten
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
        public GameObject branch; // the branch it grows on
        public float respawnTime; // Time.time it grows back, -1 if hanging
    }

    private struct Projectile {
        public Transform transform;
        public Vector3 start; // world point it was thrown from
        public float ageS;
        public float nextTrailS; // age the next trail dot drops at
    }

    private struct Spark {
        public Transform transform;
        public Vector3 velocity;
        public float size;
        public float ageS;
        public float lifeS;
    }

    private struct Marker {
        public Transform rim; // full-size dark disc, null for the band
        public Transform fill; // grows with the warning's progress
        public float size; // disc diameter; u
        public int warning; // hud warning id, -1 if none
        public bool isBand; // the sweep's band round the trunk
        public bool isStrip; // a minion's vertical fall line
        public float topY; // strip top, world y
        public float length; // strip length; u
        public float angle; // strip angle round the trunk; deg
    }

    private struct Minion {
        public float angle; // round the trunk; deg
        public Transform root; // null until it falls
        public bool isDone; // fallen, or the wave's gap
        public float dropDelayS; // wave time it starts to fall at
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
    private int waveIndex; // minion wave under way
    private bool hasCalledMinions; // the minion call was said this game
    private float waveTimeS; // time into the minion wave
    private Vector3 biteSpot; // world point the running bite dives at
    private bool tailHit;
    private int health;
    private bool hasHit; // the monkey has landed a first hit
    private bool hasShield; // the golden banana's shield is up
    private bool hasWon;
    private bool isFinisherWin;
    private float finisherTimeS; // real time into the finisher; s
    private float nextFinisherFlashS; // finisher time of the next flash
    private readonly List<SpiderWebTrap> webs = new List<SpiderWebTrap>();
    private int goldenIndex = -1; // the golden banana in the fruit list
    private float nextGoldenTime; // Time.time the next visit starts
    private float goldenHideTime; // Time.time the current visit ends
    private float victoryTimeS;
    private float entryY; // monkey's y when the fight began
    private float coilBottomY; // world y of the king's coil
    private Vector3 trunkCenter;
    private string lastLine;
    private float keyLineEndTime; // Time.time a key line stops holding
    private float nextSameLineTime;
    private float nextPushTime; // Time.time b4 which no new butt starts
    private Vector3 crownStart;
    private Vector3 crownStartScale;
    private readonly List<ArenaFruit> fruits = new List<ArenaFruit>();
    private readonly List<Projectile> projectiles = new List<Projectile>();
    private readonly List<Spark> sparks = new List<Spark>();
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
    private SpiderWebStruggle struggle;
    private Transform crown;
    private CobraMotor greatCobra;
    private CameraLookUp cameraLookUp;
    private MaterialPropertyBlock colorBlock;
}
