using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Every dev/hack key in one place: the key, the action it triggers, and a
/// one-line description for the on-screen help. Consumers look a key up by
/// <see cref="HackAction"/> and gate on <see cref="IsActive"/>, so the keys
/// work in every build; hints show only per <see cref="AreHintsShown"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "HackShortcutConfig",
    menuName = "Scriptable Objects/HackShortcutConfig"
)]
public class HackShortcutConfig: ScriptableObject {
    /// <summary>
    /// one hack key: what it triggers and how the help text words it
    /// </summary>
    [Serializable]
    public struct Entry {
        // Inspector Fields  ###################################################
        [SerializeField]
        [Tooltip("action the key triggers")]
        private HackAction action;

        [SerializeField]
        [Tooltip("key that triggers the action")]
        private Key key;

        [SerializeField]
        [Tooltip("one line for the on-screen help, after the key name")]
        private string description;

        // Public Members  #####################################################
        /// <returns>action the key triggers</returns>
        public HackAction Action => action;

        /// <returns>key that triggers the action</returns>
        public Key Key => key;

        /// <returns>one line for the on-screen help</returns>
        public string Description => description;
    }

    // Public Members  #########################################################
    /// <returns>if the hack layer is on, ie hack keys may run: always in a
    /// build w/o <see cref="AreHintsShown"/>, release included; in a
    /// development build or the Editor only once
    /// <see cref="HackAction.ToggleHints"/> has turned it on, so a tester
    /// never triggers a hack key by accident</returns>
    public static bool IsActive => !AreHintsShown || isLayerOn;

    /// <returns>if hack hints and dev-only UI may show, ie a development
    /// build or the Editor</returns>
    public static bool AreHintsShown => Debug.isDebugBuild;

    /// <returns>every hack key, in the order the help text lists them
    /// </returns>
    public IReadOnlyList<Entry> Entries => entries;

    // Public Methods  #########################################################
    /// <returns>key bound to the action, <see cref="Key.None"/> if the action
    /// has no entry</returns>
    public Key GetKey(HackAction action) {
        foreach (Entry entry in entries) {
            if (entry.Action == action) {
                return entry.Key;
            }
        }
        return Key.None;
    }

    /// <summary>
    /// Reads the dev key of an action, so no gameplay script touches the
    /// keyboard device itself. The toggle key is read w/ the hints, the rest
    /// w/ the hack layer.
    /// </summary>
    /// <returns>if the key of the action went down this frame; never while
    /// its gate is off (<see cref="IsActive"/>, or
    /// <see cref="AreHintsShown"/> for <see cref="HackAction.ToggleHints"/>),
    /// w/o a keyboard, or w/o a key</returns>
    public bool WasPressedThisFrame(HackAction action) {
        Keyboard keyboard = Keyboard.current;
        Key key = GetKey(action);
        bool isGateOpen = action == HackAction.ToggleHints
            ? AreHintsShown
            : IsActive;
        return !TextInputFocus.BlocksShortcuts && isGateOpen
            && keyboard != null
            && key != Key.None
            && keyboard[key].wasPressedThisFrame;
    }

    /// <summary>
    /// Flips the hack layer when the toggle key went down this frame, and so
    /// the on-screen list w/ it. Call from one script only, the panel.
    /// </summary>
    /// <returns>if the layer was flipped; never outside a development build
    /// </returns>
    public bool TryToggleLayer() {
        if (!WasPressedThisFrame(HackAction.ToggleHints)) {
            return false;
        }
        isLayerOn = !isLayerOn;
        if (Debug.isDebugBuild) {
            Debug.Log("HackShortcutConfig:\thack layer " + isLayerOn);
        }
        return true;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("one entry per hack action, each key used once")]
    private List<Entry> entries = new List<Entry>();

    // private members  ########################################################
    // the hack layer of a development build, off at every start; static, so
    // it holds across scene loads & every panel reads the same
    private static bool isLayerOn;

    // an Editor w/o a domain reload keeps statics, so a new play starts off
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLayer() {
        isLayerOn = false;
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        HashSet<Key> seen = new HashSet<Key>();
        foreach (Entry entry in entries) {
            if (entry.Key != Key.None && !seen.Add(entry.Key)) {
                Debug.LogWarning(
                    "HackShortcutConfig:\tkey bound twice: " + entry.Key,
                    this
                );
            }
        }
    }
}
