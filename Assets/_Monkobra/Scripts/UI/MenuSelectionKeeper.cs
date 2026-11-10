using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// on a menu root: nothing is highlighted until the player first presses
/// up or down, which selects <see cref="firstSelected"/>; a mouse hover
/// selects its own button (see <see cref="MenuButtonHover"/>)
/// </summary>
public class MenuSelectionKeeper: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("the button the first up or down press selects")]
    private GameObject firstSelected;

    [SerializeField]
    [Tooltip("moves the highlight, the UI/Navigate action (W, S, arrows)")]
    private InputActionReference navigateAction;

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip("how far a vertical press must reach to count")]
    private float pressThreshold = 0.5f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (firstSelected == null) {
            Debug.LogError(
                "MenuSelectionKeeper:\tmust assign Inspector Field: "
                    + "First Selected",
                this
            );
        }

        if (navigateAction == null) {
            Debug.LogError(
                "MenuSelectionKeeper:\tmust assign Inspector Field: "
                    + "Navigate Action",
                this
            );
        }
    }

    // the event system's input module owns the action, so it is only
    // switched on here, never off
    private void OnEnable() {
        navigateAction?.action.Enable();
        wasPressed = false;
    }

    private void Update() {
        if (navigateAction == null || EventSystem.current == null) {
            return;
        }

        float vertical = navigateAction.action.ReadValue<Vector2>().y;
        bool isPressed = Mathf.Abs(vertical) >= pressThreshold;
        bool isNewPress = isPressed && !wasPressed;
        wasPressed = isPressed;
        if (!isNewPress) {
            return;
        }

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if ((current == null || !current.activeInHierarchy)
            && firstSelected != null) {
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    // Private Members  ########################################################
    private bool wasPressed;
}
