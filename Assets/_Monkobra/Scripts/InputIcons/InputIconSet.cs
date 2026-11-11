using System;
using UnityEngine;

/// <summary>
/// The one icon table for input hints: each <see cref="InputHint"/> has a
/// keyboard and a touch look, a still icon, a sprite-sheet clip, or both. A
/// hint with no art returns null, so its owner falls back to the old text and
/// nothing breaks before the art lands.
/// </summary>
[CreateAssetMenu(
    fileName = "InputIconSet",
    menuName = "Scriptable Objects/InputIconSet"
)]
public class InputIconSet: ScriptableObject {
    // Public Types  ###########################################################
    [Serializable]
    public class Entry {
        public InputHint hint;

        [Tooltip("still icon while a keyboard is in use")]
        public Sprite keyboardIcon;

        [Tooltip("still icon while touch is in use")]
        public Sprite touchIcon;

        [Tooltip("clip frames in order while a keyboard is in use; wins over "
            + "the still icon")]
        public Sprite[] keyboardClip;

        [Tooltip("clip frames in order while touch is in use; wins over the "
            + "still icon")]
        public Sprite[] touchClip;

        [Tooltip("clip speed; frames per s")]
        public float clipFps = 8f;
    }

    // Public Methods  #########################################################
    /// <returns>the still icon for the style, null if none is set</returns>
    public Sprite GetIcon(InputHint hint, ControlStyle style) {
        Entry entry = Find(hint);
        if (entry == null) {
            return null;
        }
        return style == ControlStyle.Touch
            ? entry.touchIcon
            : entry.keyboardIcon;
    }

    /// <returns>the clip frames for the style, null if fewer than 2 are set
    /// </returns>
    public Sprite[] GetClip(InputHint hint, ControlStyle style) {
        Entry entry = Find(hint);
        if (entry == null) {
            return null;
        }
        Sprite[] frames = style == ControlStyle.Touch
            ? entry.touchClip
            : entry.keyboardClip;
        return frames != null && frames.Length >= 2 ? frames : null;
    }

    /// <returns>the clip speed of the hint; frames per s, default if none
    /// </returns>
    public float GetClipFps(InputHint hint) {
        Entry entry = Find(hint);
        return entry != null ? entry.clipFps : DEFAULT_CLIP_FPS;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    private Entry[] entries = new Entry[0];

    // Private Members  ########################################################
    private const float DEFAULT_CLIP_FPS = 8f;

    // Private Methods  ########################################################
    private Entry Find(InputHint hint) {
        if (entries == null) {
            return null;
        }
        for (int i = 0; i < entries.Length; i++) {
            if (entries[i] != null && entries[i].hint == hint) {
                return entries[i];
            }
        }
        return null;
    }
}
