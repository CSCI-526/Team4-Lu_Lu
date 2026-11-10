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
    // MonoBehaviour Lifecycle  ################################################
    private void Start() {
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

        // Follow the same target as the arm and outline, including its lock
        // while Space is held. An unrelated nearby fruit cannot steal it.
        Collider fruit = sensor.ReachTarget;
        FruitKind kind = GetFruitKind(fruit);
        if (
            fruit == null
            || !fruit.enabled
            || !fruit.gameObject.activeInHierarchy
            || !sensor.IsFruitInReach(fruit)
            || kind == FruitKind.None
            || IsLearned(kind)
        ) {
            HidePopUp();
            return;
        }

        ShowPopUp(kind);
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

    private enum FruitKind { None, Banana, Pineapple, Coconut }

    // private methods  ########################################################
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
