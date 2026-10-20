using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Every dev/hack key in one place: the key, the action it triggers, and a
/// one-line description for the on-screen help. Consumers look a key up by
/// <see cref="HackAction"/> and gate on <see cref="IsActive"/>, so the keys
/// only work in a development build.
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
    /// <returns>if hack keys may run, ie a development build or the Editor
    /// </returns>
    public static bool IsActive => Debug.isDebugBuild;

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

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("one entry per hack action, each key used once")]
    private List<Entry> entries = new List<Entry>();

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
