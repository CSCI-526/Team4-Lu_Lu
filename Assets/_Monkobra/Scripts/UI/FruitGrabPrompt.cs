using UnityEngine;

/// <summary>
/// Teaches the grab of each fruit kind with a popup, until the player has
/// grabbed one of that kind in the run. While the monkey can reach an
/// unlearned fruit, its popup is held on screen
/// (<see cref="PopUpId.BananaFirst"/>, <see cref="PopUpId.PineappleFirst"/>,
/// <see cref="PopUpId.CoconutGrab"/>);
/// it hides when the fruit leaves reach and returns at the next one of that
/// kind, so a player who saw it but never grabbed is taught again. Lives on
/// the shared UI Canvas, so generated fruit needs no extra component or scene
/// wiring.
/// </summary>
[DisallowMultipleComponent]
public class FruitGrabPrompt: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("player balance tuning; the popup detect radius")]
    private PlayerBalanceConfig playerBalanceConfig;

    // MonoBehaviour Lifecycle  ################################################
    private void Start() {
        if (playerBalanceConfig == null) {
            Debug.LogError(
                "FruitGrabPrompt:	must assign Inspector Field: "
                    + "playerBalanceConfig",
                this
            );
            enabled = false;
            return;
        }
        game = GameController.I;
        sensor = UpwardFruitSensor.I;
        if (game == null || sensor == null) {
            Debug.LogError(
                "FruitGrabPrompt:	no GameController or UpwardFruitSensor",
                this
            );
            enabled = false;
            return;
        }
        monkey = sensor.GetComponentInParent<MonkeyController>();
    }

    private void OnEnable() {
        ArmController.FruitGrabbed += OnFruitGrabbed;
    }

    private void OnDisable() {
        ArmController.FruitGrabbed -= OnFruitGrabbed;
        HidePopUp();
    }

    private void Update() {
        if (!CanShowPrompt()) {
            HidePopUp();
            return;
        }

        // A fruit in arm reach wins, including its lock while Space is held,
        // so an unrelated fruit cannot steal it; else the nearest unlearned
        // fruit within the wide range, even on the far side of the tree.
        Collider fruit = sensor.ReachTarget;
        FruitKind kind = GetFruitKind(fruit);
        if (
            fruit == null
            || !IsPromptable(fruit)
            || !sensor.IsFruitInReach(fruit)
            || kind == FruitKind.None
            || IsLearned(kind)
        ) {
            fruit = FindNearestUnlearnedFruit();
            kind = GetFruitKind(fruit);
        }
        if (fruit == null) {
            HidePopUp();
            return;
        }

        ShowPopUp(kind);
        // the reach target may change while one kind stays shown
        if (shownLink != null) {
            shownLink.SetTarget(fruit.transform);
        }
    }

    // Event Handlers  #########################################################
    private void OnFruitGrabbed(Collider fruit) {
        // Only a successful release teaches a grab. A miss, a branch being
        // recycled, or a disabled target must never finish the tutorial.
        if (game == null || !game.IsRunLive || IsBossArena()) {
            return;
        }
        switch (GetFruitKind(fruit)) {
            case FruitKind.Banana:
                hasGrabbedBanana = true;
                break;
            case FruitKind.Pineapple:
                hasGrabbedPineapple = true;
                break;
            case FruitKind.Coconut:
                hasGrabbedCoconut = true;
                break;
        }
        HidePopUp();
    }

    // private members  ########################################################
    private bool hasGrabbedBanana;
    private bool hasGrabbedPineapple;
    private bool hasGrabbedCoconut;
    private GameController game;
    private UpwardFruitSensor sensor;
    private MonkeyController monkey;
    private FruitKind shownKind = FruitKind.None; // kind whose popup is held
    private PopUpWorldLink shownLink; // line from the shown popup to its fruit

    private enum FruitKind { None, Banana, Pineapple, Coconut }

    // constants  ##############################################################
    private const string FRUIT_TAG = "FruitCollider";
    private const int SCAN_BUFFER_SIZE = 64;

    private readonly Collider[] scanBuffer = new Collider[SCAN_BUFFER_SIZE];

    // private methods  ########################################################
    // if the fruit is still live in the scene
    private static bool IsPromptable(Collider fruit) {
        return fruit.enabled && fruit.gameObject.activeInHierarchy;
    }

    // nearest live fruit of an unlearned kind within the config radius of the
    // monkey, null for none; the scan ignores the arm zones on purpose
    private Collider FindNearestUnlearnedFruit() {
        Vector3 origin = sensor.transform.position;
        int count = Physics.OverlapSphereNonAlloc(
            origin,
            playerBalanceConfig.FruitPopUpDetectRadiusU,
            scanBuffer,
            ~0,
            QueryTriggerInteraction.Collide
        );

        Collider nearest = null;
        float nearestSqrDistance = float.MaxValue;
        int nearestRank = int.MaxValue;
        for (int i = 0; i < count; i++) {
            Collider candidate = scanBuffer[i];
            if (
                !candidate.CompareTag(FRUIT_TAG)
                || !IsPromptable(candidate)
                || IsLearned(GetFruitKind(candidate))
            ) {
                continue;
            }
            // the rarer kind outranks distance, so a far pineapple is not
            // hidden behind a near banana
            int rank = GetFruitKind(candidate) == FruitKind.Banana ? 1 : 0;
            float sqrDistance =
                (candidate.bounds.center - origin).sqrMagnitude;
            if (
                rank < nearestRank
                || rank == nearestRank && sqrDistance < nearestSqrDistance
            ) {
                nearestRank = rank;
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
        }
        return nearest;
    }

    private bool CanShowPrompt() {
        return game != null && sensor != null
            && game.IsRunLive && Time.timeScale > 0f
            && !game.IsLeaping
            && (monkey == null || !monkey.IsHeld)
            && !IsBossArena();
    }

    private static bool IsBossArena() {
        BossFightController boss = BossFightController.I;
        return boss != null && boss.isActiveAndEnabled && boss.IsInArena;
    }

    private bool IsLearned(FruitKind kind) {
        return kind switch {
            FruitKind.Banana => hasGrabbedBanana,
            FruitKind.Pineapple => hasGrabbedPineapple,
            FruitKind.Coconut => hasGrabbedCoconut,
            _ => true
        };
    }

    private static FruitKind GetFruitKind(Collider fruit) {
        if (fruit == null) {
            return FruitKind.None;
        }
        // The grab event arrives after the fruit root has been switched off.
        if (fruit.GetComponentInParent<LeapOnGrab>(true) != null) {
            return FruitKind.Pineapple;
        }
        ScorePickup pickup = fruit.GetComponentInParent<ScorePickup>(true);
        if (pickup == null) {
            return FruitKind.None;
        }
        return pickup.Kind == PickupKind.Coconut
            ? FruitKind.Coconut : FruitKind.Banana;
    }

    private static PopUpId GetPopUpId(FruitKind kind) {
        return kind switch {
            FruitKind.Pineapple => PopUpId.PineappleFirst,
            FruitKind.Coconut => PopUpId.CoconutGrab,
            _ => PopUpId.BananaFirst
        };
    }

    // held while an unlearned fruit is in reach, the popups have no show time
    private void ShowPopUp(FruitKind kind) {
        if (shownKind == kind || PopUpManager.I == null) {
            return;
        }
        HidePopUp();
        PopUpManager.I.Show(GetPopUpId(kind));
        shownKind = kind;
        MessagePopUp popUp = PopUpManager.I.GetPopUp(GetPopUpId(kind));
        shownLink = popUp != null
            ? popUp.GetComponent<PopUpWorldLink>() : null;
    }

    private void HidePopUp() {
        if (shownKind == FruitKind.None) {
            return;
        }
        FruitKind kind = shownKind;
        shownKind = FruitKind.None;
        if (PopUpManager.I != null) {
            PopUpManager.I.Hide(GetPopUpId(kind));
        }
    }
}
