using UnityEngine;

/// <summary>
/// The one owner that shows a group of objects for each control style: the
/// keyboard group while a keyboard is in use, the touch group while touch is,
/// following <see cref="ControlStyleManager"/> live. The groups' own scripts
/// never switch themselves.
/// </summary>
public class ControlStyleSwitch: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("objects shown only while a keyboard is in use")]
    private GameObject[] keyboardObjects = new GameObject[0];

    [SerializeField]
    [Tooltip("objects shown only while touch is in use")]
    private GameObject[] touchObjects = new GameObject[0];

    // MonoBehaviour Lifecycle  ################################################
    private void OnEnable() {
        ControlStyleManager.StyleChanged += Apply;
        Apply(ControlStyleManager.Current);
    }

    private void OnDisable() {
        ControlStyleManager.StyleChanged -= Apply;
    }

    // Private Methods  ########################################################
    private void Apply(ControlStyle style) {
        bool isTouch = style == ControlStyle.Touch;
        SetAll(keyboardObjects, !isTouch);
        SetAll(touchObjects, isTouch);
    }

    private static void SetAll(GameObject[] objects, bool isOn) {
        foreach (GameObject obj in objects) {
            if (obj != null && obj.activeSelf != isOn) {
                obj.SetActive(isOn);
            }
        }
    }
}
