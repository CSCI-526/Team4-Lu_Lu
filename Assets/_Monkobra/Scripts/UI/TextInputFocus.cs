using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Text entry consumes run shortcuts, including its final key
/// frame.</summary>
public static class TextInputFocus {
    // Public Members  #########################################################
    public static bool BlocksShortcuts {
        get {
            if (Time.frameCount <= protectedThroughFrame) {
                return true;
            }
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;
            TMP_InputField input = selected != null
                ? selected.GetComponent<TMP_InputField>() : null;
            return input != null && input.isFocused;
        }
    }

    // Public Methods  #########################################################
    public static void ProtectThisFrame() {
        protectedThroughFrame = Time.frameCount + 1;
    }

    // Runtime Lifecycle  ######################################################
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void Reset() {
        protectedThroughFrame = -1;
    }

    // private members  ########################################################
    private static int protectedThroughFrame = -1;
}
