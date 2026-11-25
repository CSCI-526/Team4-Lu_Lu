using UnityEngine;

/// <summary>
/// The Spider King's web intro, a scripted story event that teaches the web
/// escape: a player may reach the web zone and never meet a web, so once per
/// Normal Play run, at a fixed climbed height set in
/// <see cref="SpiderWebIntroConfig"/>, the king appears on the trunk above
/// the monkey and the camera cuts to it. Never in Endless mode. A fresh scene
/// load makes a fresh event, so every run gets its own.
/// <para>
/// Phases: <see cref="Phase.Waiting"/> for the trigger, then
/// <see cref="Phase.Appear"/>: the great cobra is held still
/// (<see cref="CobraMotor.SetStoryHold"/>), the spider intro shot is asked of
/// <see cref="CameraRigManager"/> (which locks the monkey's input), the king
/// is built above the monkey and its eyes glow up. Then
/// <see cref="Phase.Throw"/>: the king spits a web, the real
/// <see cref="SpiderWebTrap"/> prefab, unarmed so it passes harmlessly, that
/// arcs to the monkey's spot on the bark and grows to full size on the way.
/// Then <see cref="Phase.Stick"/>: it lands flat on the bark facing out,
/// armed, and traps the monkey, whose escape the existing
/// <see cref="SpiderWebStruggle"/> and its first-trap hint teach.
/// </para>
/// Finds the monkey, the cobra, the camera rig and the framing by singleton or
/// type at use, as they live in other prefabs.
/// </summary>
[RequireComponent(typeof(SpiderKingBody))]
public class SpiderWebIntroEvent: MonoBehaviour {
    // Public Members  #########################################################
    /// <summary>
    /// The stages of the event, in order
    /// </summary>
    public enum Phase {
        Waiting,
        Appear,
        Throw,
        Stick,
    }

    /// <returns>the stage the event is in</returns>
    public Phase CurrentPhase => phase;

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("trigger height, king offset and every timing of the event")]
    private SpiderWebIntroConfig config;

    [SerializeField]
    [Tooltip("the climb's spider web prefab, the very web the king throws")]
    private SpiderWebTrap webPrefab;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "SpiderWebIntroEvent:\tmust assign Inspector Field: config",
                this
            );
        }
        if (webPrefab == null) {
            Debug.LogWarning(
                "SpiderWebIntroEvent:\tmust assign Inspector Field: webPrefab",
                this
            );
        }
        if (config == null || webPrefab == null) {
            enabled = false;
            return;
        }

        king = GetComponent<SpiderKingBody>();
        subject = new GameObject("SpiderIntroSubject").transform;
        subject.SetParent(transform, false);
    }

    private void Start() {
        // a climb in the endless run has its own boss rush, no intro
        if (GameController.I != null
            && GameController.I.Mode != RunMode.Full) {
            enabled = false;
            return;
        }
        if (GameController.I == null) {
            Debug.LogWarning(
                "SpiderWebIntroEvent:\tfail to get singleton: "
                    + "GameController, no mode check",
                this
            );
        }
    }

    private void Update() {
        switch (phase) {
            case Phase.Waiting:
                if (IsTriggered()) {
                    BeginAppear();
                }
                break;
            case Phase.Appear:
                UpdateAppear();
                break;
            case Phase.Throw:
                UpdateThrow();
                break;
            case Phase.Stick:
                UpdateStick();
                break;
        }
        FollowKingWithSubject();
    }

    // private members  ########################################################
    private Phase phase = Phase.Waiting;
    private bool isDone; // the event ran once, never again this run
    private bool hasWarnedNoShot; // a refused shot is logged once
    private bool hasForcedTrap; // the trap fallback ran, it runs once
    private float phaseStartTime; // Time.time the phase began
    private float kingAngleDeg; // angle round the trunk the king sits at
    private float kingY; // world height of the king's center
    private Vector3 trunkCenter; // trunk axis point at the start
    private Vector3 webFullScale; // the prefab's own scale, reached at landing
    private SpiderWebTrap web; // the web in flight, then stuck on the bark

    // cached references  ------------------------------------------------------
    private SpiderKingBody king;
    private Transform subject; // the point the shot fits with the monkey
    private MonkeyController monkey;
    private CobraMotor cobra;
    private CameraRigManager cameraRig;
    private CameraSpiderIntroFraming framing;
    private SpiderWebStruggle struggle;

    // constants  ##############################################################
    // how long the web may take to catch by itself before the trap is forced; s
    private const float CATCH_GRACE_S = 0.3f;

    // private methods  ########################################################
    // iff the run plays, the monkey climbed to the trigger height in the open
    // and nothing else holds or hides it: not leaping, not trapped, not in a
    // boss zone
    private bool IsTriggered() {
        if (isDone || !ResolveReferences()) {
            return false;
        }
        GameController game = GameController.I;
        if (game != null
            && (game.State != GameState.Playing || game.IsPaused
                || game.IsLeaping)) {
            return false;
        }
        if (monkey.IsHeld || monkey.IsInputLocked) {
            return false;
        }
        if (struggle != null && struggle.IsTrapped) {
            return false;
        }

        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null) {
            return false;
        }
        float monkeyY = monkey.transform.position.y;
        return difficulty.GetClimbedYDistance(monkeyY)
                >= config.TriggerClimbedHeightU
            && !difficulty.IsInBossZoneY(monkeyY)
            && !difficulty.IsInBossZoneY(monkeyY + config.KingAboveU);
    }

    // looks up what lives in other prefabs; false while the monkey is not in
    // the scene yet
    private bool ResolveReferences() {
        if (monkey == null) {
            monkey = MonkeyController.I;
        }
        if (cobra == null) {
            cobra = FindFirstObjectByType<CobraMotor>();
        }
        if (cameraRig == null) {
            cameraRig = FindFirstObjectByType<CameraRigManager>();
        }
        if (framing == null) {
            framing = FindFirstObjectByType<CameraSpiderIntroFraming>();
        }
        if (monkey != null && struggle == null) {
            struggle = monkey.GetComponent<SpiderWebStruggle>();
        }
        return monkey != null && monkey.transform.parent != null;
    }

    // the king shows above the monkey and the camera cuts to it; stays
    // waiting if the shot is refused, so the player is never left free
    // beside a story they cannot see
    private void BeginAppear() {
        if (cameraRig == null) {
            WarnNoShot();
            return;
        }

        trunkCenter = monkey.transform.parent.position;
        Vector3 outward = monkey.transform.position - trunkCenter;
        outward.y = 0f;
        if (outward.sqrMagnitude <= Mathf.Epsilon) {
            return;
        }
        kingAngleDeg = Mathf.Atan2(outward.z, outward.x) * Mathf.Rad2Deg;
        kingY = monkey.transform.position.y + config.KingAboveU;

        king.Build(trunkCenter, kingAngleDeg, kingY);
        FollowKingWithSubject();
        if (framing != null) {
            framing.SetSubject(subject);
        }
        if (!cameraRig.RequestShot(CameraRigManager.ShotKind.SpiderIntro)) {
            king.Clear();
            if (framing != null) {
                framing.SetSubject(null);
            }
            WarnNoShot();
            return;
        }

        if (cobra != null) {
            cobra.SetStoryHold(true);
        }
        isDone = true;
        king.SetEyeGlow(0f);
        EnterPhase(Phase.Appear);
        if (Debug.isDebugBuild) {
            Debug.Log(
                $"SpiderWebIntroEvent:\tking appears at y {kingY}",
                this
            );
        }
    }

    // the eyes glow up as the king takes its aim
    private void UpdateAppear() {
        king.Place(kingAngleDeg, kingY, 0f);
        float windUpS = Mathf.Max(0.01f, config.WindUpS);
        float glow = Mathf.Clamp01((Time.time - phaseStartTime) / windUpS);
        king.SetEyeGlow(glow);
        if (glow >= 1f) {
            BeginThrow();
        }
    }

    // the king spits the real web, unarmed, from between its eyes
    private void BeginThrow() {
        web = Instantiate(
            webPrefab,
            king.HeadPosition,
            GetStickRotation(GetOutward())
        );
        web.IsArmed = false;
        webFullScale = webPrefab.transform.localScale;
        web.transform.localScale = webFullScale * config.StartScale;
        EnterPhase(Phase.Throw);
    }

    // the web arcs out from the trunk to the monkey's spot on the bark,
    // growing as it flies; the spot is read live, the monkey may still glide
    private void UpdateThrow() {
        king.Place(kingAngleDeg, kingY, 0f);
        if (web == null) {
            // the web vanished mid flight: nothing left to throw
            Debug.LogWarning(
                "SpiderWebIntroEvent:\tthe flying web is gone", this);
            EnterPhase(Phase.Stick);
            return;
        }

        float t = Mathf.Clamp01(
            (Time.time - phaseStartTime) / Mathf.Max(0.01f, config.ThrowTimeS)
        );
        Vector3 outward = GetOutward();
        Vector3 spot = GetStickSpot(outward);
        Vector3 flight = Vector3.Lerp(king.HeadPosition, spot, t)
            + outward * (config.ArcHeightU * 4f * t * (1f - t));
        web.transform.SetPositionAndRotation(
            flight, GetStickRotation(outward));
        web.transform.localScale =
            webFullScale * Mathf.Lerp(config.StartScale, 1f, t);
        if (t >= 1f) {
            BeginStick(outward);
        }
    }

    // lands flat on the bark facing out and arms: the monkey is in its
    // trigger, so it traps by itself; UpdateStick forces it if it does not
    private void BeginStick(Vector3 outward) {
        web.transform.SetPositionAndRotation(
            GetStickSpot(outward), GetStickRotation(outward));
        web.transform.localScale = webFullScale;
        web.IsArmed = true;
        EnterPhase(Phase.Stick);
    }

    private void UpdateStick() {
        king.Place(kingAngleDeg, kingY, 0f);
        if (struggle == null || struggle.IsTrapped || hasForcedTrap) {
            return;
        }
        if (Time.time - phaseStartTime < CATCH_GRACE_S) {
            return;
        }
        // the trigger had its chance, trap the monkey in the web outright, once
        hasForcedTrap = true;
        if (web == null || !struggle.TryTrap(web)) {
            Debug.LogWarning(
                "SpiderWebIntroEvent:\tthe web did not trap the monkey",
                this
            );
        }
    }

    // direction from the trunk axis out to the monkey, flat; the web faces it
    private Vector3 GetOutward() {
        Vector3 outward = monkey.transform.position - trunkCenter;
        outward.y = 0f;
        return outward.sqrMagnitude > Mathf.Epsilon
            ? outward.normalized
            : new Vector3(
                Mathf.Cos(kingAngleDeg * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(kingAngleDeg * Mathf.Deg2Rad)
            );
    }

    // the monkey's height on the bark, as SpiderWebSpawner lays a web
    private Vector3 GetStickSpot(Vector3 outward) {
        return new Vector3(trunkCenter.x, monkey.transform.position.y,
            trunkCenter.z) + outward * config.TrunkRadiusU;
    }

    // -90 yaw turns the strands' local +X onto forward, so the face looks out
    private static Quaternion GetStickRotation(Vector3 outward) {
        return Quaternion.LookRotation(outward)
            * Quaternion.Euler(0f, -90f, 0f);
    }

    private void EnterPhase(Phase next) {
        phase = next;
        phaseStartTime = Time.time;
    }

    // keeps the framed point on the king, so the shot fits it with the monkey
    private void FollowKingWithSubject() {
        if (king != null && king.IsBuilt) {
            subject.position = king.Position;
        }
    }

    private void WarnNoShot() {
        if (hasWarnedNoShot) {
            return;
        }
        hasWarnedNoShot = true;
        Debug.LogWarning(
            "SpiderWebIntroEvent:\tthe spider intro shot was refused, the "
                + "intro waits; assign spiderIntroCamera on CameraRigManager",
            this
        );
    }
}
