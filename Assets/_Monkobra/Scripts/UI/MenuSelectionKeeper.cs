using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// on a menu root: keeps one button selected at all times, so keys always
/// have a highlight, even after a click on empty space
/// </summary>
public class MenuSelectionKeeper: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("the button highlighted when the menu opens")]
    private GameObject firstSelected;

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
    }

    private void OnEnable() {
        lastSelected = firstSelected;
        Select(firstSelected);
    }

    private void Update() {
        if (EventSystem.current == null) {
            return;
        }

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null && current.activeInHierarchy) {
            lastSelected = current;
            return;
        }

        Select(lastSelected);
    }

    // Private Members  ########################################################
    private GameObject lastSelected;

    private static void Select(GameObject target) {
        if (target != null && EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
