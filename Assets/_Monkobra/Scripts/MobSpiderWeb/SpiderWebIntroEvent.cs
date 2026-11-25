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
/// is built above the monkey and its eyes glow up.
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
    }

    /// <returns>the stage the event is in</returns>
    public Phase CurrentPhase => phase;

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("trigger height, king offset and every timing of the event")]
    private SpiderWebIntroConfig config;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "SpiderWebIntroEvent:\tmust assign Inspector Field: config",
                this
            );
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
        }
        FollowKingWithSubject();
    }

    // private members  ########################################################
    private Phase phase = Phase.Waiting;
    private bool isDone; // the event ran once, never again this run
    private bool hasWarnedNoShot; // a refused shot is logged once
    private float phaseStartTime; // Time.time the phase began
    private float kingAngleDeg; // angle round the trunk the king sits at
    private float kingY; // world height of the king's center
    private Vector3 trunkCenter; // trunk axis point at the start

    // cached references  ------------------------------------------------------
    private SpiderKingBody king;
    private Transform subject; // the point the shot fits with the monkey
    private MonkeyController monkey;
    private CobraMotor cobra;
    private CameraRigManager cameraRig;
    private CameraSpiderIntroFraming framing;

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
        SpiderWebStruggle struggle = monkey.GetComponent<SpiderWebStruggle>();
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
        // TODO the throw follows the wind up
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
