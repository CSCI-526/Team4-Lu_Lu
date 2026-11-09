using UnityEngine;

/// <summary>
/// Teaches the first successful grab of each fruit kind in a run, using the
/// same keycap as the boss fight. Lives on the shared UI Canvas, so generated
/// fruit needs no extra component or scene wiring.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class FruitGrabPrompt: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("prompt offset over the reachable fruit, in canvas units")]
    private Vector2 promptOffset = new Vector2(0f, 80f);

    [SerializeField]
    private string keyText = "space";

    [SerializeField]
    private string instructionText = "hold, then release";

    // MonoBehaviour Lifecycle  ################################################
    private void Start() {
        game = GameController.I;
        sensor = UpwardFruitSensor.I;
        if (game == null || sensor == null) {
            Debug.LogError(
                "FruitGrabPrompt:\tno GameController or UpwardFruitSensor",
                this
            );
            enabled = false;
            return;
        }
        monkey = sensor.GetComponentInParent<MonkeyController>();

        RectTransform canvasRect = (RectTransform)transform;
        prompt = KeyPromptUI.Create(canvasRect, "FruitGrabPrompt");
        prompt.SetText(keyText, instructionText);
        // The pause, tutorial and lose screens must cover the prompt.
        foreach (Transform child in transform) {
            if (child.name.Contains("Screens")) {
                prompt.transform.SetSiblingIndex(child.GetSiblingIndex());
                break;
            }
        }
    }

    private void OnEnable() {
        ArmController.FruitGrabbed += OnFruitGrabbed;
    }

    private void OnDisable() {
        ArmController.FruitGrabbed -= OnFruitGrabbed;
        HidePrompt();
    }

    private void OnDestroy() {
        if (prompt != null) {
            Destroy(prompt.gameObject);
        }
    }

    private void Update() {
        if (prompt == null) {
            return;
        }
        if (!CanShowPrompt()) {
            HidePrompt();
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
            HidePrompt();
            return;
        }

        prompt.SetPrompt(true, fruit.bounds.center, promptOffset);
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
        HidePrompt();
    }

    // private members  ########################################################
    private bool hasGrabbedBanana;
    private bool hasGrabbedPineapple;
    private bool hasGrabbedCoconut;
    private GameController game;
    private UpwardFruitSensor sensor;
    private MonkeyController monkey;
    private KeyPromptUI prompt;

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

    private void HidePrompt() {
        if (prompt != null) {
            prompt.SetPrompt(false, Vector3.zero, promptOffset);
        }
    }
}
