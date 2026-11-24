using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// the intro comic's panels, written by <c>Monkobra/Build Intro Comic</c> from
/// <c>tools/comic/panels/panels.json</c>: each panel's caption, its layers back
/// to front, the view its camera eases between, and the cue of each layer
/// that moves on its own. Positions are 1280x720 canvas units from the
/// panel's middle, y up; the timings below are the screen's own.
/// </summary>
[CreateAssetMenu(
    fileName = "IntroComicConfig",
    menuName = "Scriptable Objects/IntroComicConfig"
)]
public class IntroComicConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <summary>what a layer does on its own, see <see cref="Cue"/></summary>
    public enum CueKind {
        None,
        Fall, // drops from start to end over period, swaying amount, fades
        Pulse, // scale grows by amount and back, every period
        Pop, // hidden until delay, then pops in over period
        Walk, // start to end, startScale to endScale, bobs amount, fades
        Drift, // start to end over period, bobbing amount
        Blink, // on and off, period each
        Breathe, // scale grows by amount and back, smoothly, every period
        Twinkle, // alpha falls to amount and back, every period
        Reveal, // hidden until delay, blinks on twice, period a blink, stays
    }

    [Serializable]
    public class Cue {
        public CueKind kind;
        [Min(0f)] public float delayS;
        [Min(0.01f)] public float periodS = 1f;
        public float amount;
        [Tooltip("offset from the layer's place, canvas units, y up")]
        public Vector2 start;
        [Tooltip("offset from the layer's place, canvas units, y up")]
        public Vector2 end;
        public float startScale = 1f;
        public float endScale = 1f;
    }

    [Serializable]
    public class Layer {
        public string name;
        public Sprite sprite;
        [Tooltip("middle of the layer from the panel's middle, y up")]
        public Vector2 center;
        public Vector2 size;
        public Cue cue = new Cue();
    }

    [Serializable]
    public class View {
        [Tooltip("panel point at the middle of the screen, from the panel's "
            + "middle, y up")]
        public Vector2 center;
        [Min(1f)] public float zoom = 1f;
    }

    [Serializable]
    public class Panel {
        [TextArea(2, 3)]
        [Tooltip("the caption; a newline is where its line breaks")]
        public string caption;
        [Min(0.1f)]
        [Tooltip("seconds the camera takes from start to end")]
        public float motionS = 6f;
        public View start = new View();
        public View end = new View();
        public Layer[] layers = new Layer[0];
    }

    public IReadOnlyList<Panel> Panels => panels;
    public float FadeS => fadeS;
    public float HintDelayS => hintDelayS;
    public float InputDelayS => inputDelayS;

#if UNITY_EDITOR
    /// <summary>the builder's way in; keeps the timings</summary>
    public void SetPanels(Panel[] value) {
        panels = value ?? new Panel[0];
    }
#endif

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("written by Monkobra/Build Intro Comic; edit panels.json and "
        + "build again rather than here")]
    private Panel[] panels = new Panel[0];

    [Header("Timing")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("seconds one panel fades into the next")]
    private float fadeS = 0.45f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("seconds into a panel before the next-page hint shows")]
    private float hintDelayS = 1.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("unscaled seconds a press is ignored after load, so the press "
        + "that left the title does not turn the first page")]
    private float inputDelayS = 0.4f;
}
