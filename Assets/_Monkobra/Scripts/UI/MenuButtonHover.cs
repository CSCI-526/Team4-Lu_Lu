using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// on a menu <see cref="Selectable"/>: hovering selects it, so the mouse and
/// the keys share one highlight
/// </summary>
[RequireComponent(typeof(Selectable))]
public class MenuButtonHover: MonoBehaviour, IPointerEnterHandler {
    // Public Members  #########################################################
    public void OnPointerEnter(PointerEventData eventData) {
        if (selectable != null && selectable.IsInteractable()
            && EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        selectable = GetComponent<Selectable>();
    }

    // Private Members  ########################################################
    private Selectable selectable;
}
