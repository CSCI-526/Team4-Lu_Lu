using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the popup system of one scene: shows one popup at a time, by
/// <see cref="PopUpId"/>, on the shared <see cref="MessagePopUp"/> entity of
/// its <see cref="PopUpKind"/>. What each popup says and how it looks comes
/// from the <see cref="PopUpCatalog"/>. A higher priority interrupts the
/// showing popup, which returns to the queue front with its time left; an
/// equal or lower one queues. It also gates run-only popups on the run being
/// live, remembers the once-per-scene ones, and drops everything on a loss.
/// <para>
/// Put it on the always-active popup root: the two entities under it are
/// saved inactive and shown only by this script, their only owner.
/// </para>
/// </summary>
public class PopUpManager: MonoBehaviour {
    // Public Members  #########################################################
    public static PopUpManager I {
        get; private set;
    }

    // Public Methods  #########################################################
    /// <summary>
    /// Asks for a popup. It shows now, waits in the queue, or is dropped by
    /// its rules: run-only while the run is not live, or once-per-scene
    /// already shown.
    /// </summary>
    /// <param name="id">the popup</param>
    /// <param name="args">values for the <c>{0}</c> slots of its text</param>
    public void Show(PopUpId id, params object[] args) {
        if (!lookup.TryGetValue(id, out PopUpDefinition popUp)) {
            Debug.LogWarning("PopUp:\tno popup defined for id " + id, this);
            return;
        }
        if (popUp.IsRunOnly && !IsRunLive) {
            return;
        }
        if (popUp.IsOncePerScene && !shownOnce.Add(id)) {
            return;
        }

        // the same popup again: refresh it in place, never twice on screen
        if (current != null && current.Id == id) {
            ViewOf(current).Show(current, args, -1f);
            currentArgs = args;
            return;
        }
        RemoveQueued(id);

        if (current == null) {
            Present(new Entry(popUp, args, -1f));
        } else if (popUp.Priority > current.Priority) {
            queue.Insert(
                0, new Entry(current, currentArgs, CurrentView.RemainingS));
            CurrentView.Stop();
            Present(new Entry(popUp, args, -1f));
        } else {
            Enqueue(new Entry(popUp, args, -1f));
        }
    }

    /// <summary>
    /// Dismisses a popup: a showing one fades out, a queued one is dropped.
    /// </summary>
    /// <param name="id">the popup</param>
    public void Hide(PopUpId id) {
        if (current != null && current.Id == id) {
            CurrentView.Hide();
            return;
        }
        RemoveQueued(id);
    }

    /// <summary>
    /// Finds the entity that draws a popup, so its owner can reach the
    /// entity's other components, e.g. <see cref="PopUpWorldLink"/>. Call it
    /// after <see cref="Show"/>: the entity is shared by every popup of its
    /// kind.
    /// </summary>
    /// <param name="id">the popup</param>
    /// <returns>the entity, or null when no popup has the id</returns>
    public MessagePopUp GetPopUp(PopUpId id) {
        return lookup.TryGetValue(id, out PopUpDefinition popUp)
            ? ViewOf(popUp)
            : null;
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("every popup as data")]
    private PopUpCatalog catalog;

    [SerializeField]
    [Tooltip("entity that draws a Message popup")]
    private MessagePopUp messageView;

    [SerializeField]
    [Tooltip("entity that draws a Fruit popup, with its line to the fruit")]
    private MessagePopUp fruitView;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (catalog == null) {
            Debug.LogWarning(
                "PopUp:\tmust assign Inspector Field: catalog", this);
        }
        if (messageView == null) {
            Debug.LogWarning(
                "PopUp:\tmust assign Inspector Field: messageView", this);
        }
        if (fruitView == null) {
            Debug.LogWarning(
                "PopUp:\tmust assign Inspector Field: fruitView", this);
        }

        if (I != null && I != this) {
            Debug.LogWarning(
                "PopUp:\tduplicate PopUpManager, this one is off", this);
            enabled = false;
            return;
        }
        I = this;

        if (catalog != null) {
            foreach (PopUpDefinition popUp in catalog.Definitions) {
                if (!lookup.TryAdd(popUp.Id, popUp)) {
                    Debug.LogWarning(
                        "PopUp:\tid defined twice: " + popUp.Id, this);
                }
            }
        }
        if (lookup.Count == 0) {
            Debug.LogWarning("PopUp:\tno popup defined in the catalog", this);
        }
        if (messageView != null) {
            messageView.Hidden += OnPopUpHidden;
        }
        if (fruitView != null) {
            fruitView.Hidden += OnPopUpHidden;
        }
    }

    // the game controller is a singleton set in Awake, so it is read in Start
    private void Start() {
        if (!enabled) {
            return;
        }

        gameController = GameController.I;
        if (gameController == null) {
            Debug.LogWarning(
                "PopUp:\tfail to get singleton: GameController, no loss gate",
                this);
            return;
        }
        gameController.StateChanged += OnStateChanged;
    }

    private void OnDestroy() {
        if (gameController != null) {
            gameController.StateChanged -= OnStateChanged;
        }
        if (messageView != null) {
            messageView.Hidden -= OnPopUpHidden;
        }
        if (fruitView != null) {
            fruitView.Hidden -= OnPopUpHidden;
        }
        if (I == this) {
            I = null;
        }
    }

    // Event Handlers  #########################################################
    private void OnPopUpHidden(MessagePopUp view) {
        if (current == null || view != CurrentView) {
            return;
        }

        current = null;
        PresentNext();
    }

    // a loss freezes time: nothing may keep showing through the end panel
    private void OnStateChanged(GameState state) {
        if (state != GameState.Lost) {
            return;
        }

        queue.Clear();
        if (current != null) {
            CurrentView.Stop();
            current = null;
        }

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\tloss, all dropped", this);
        }
    }

    // private members  ########################################################
    private readonly Dictionary<PopUpId, PopUpDefinition> lookup =
        new Dictionary<PopUpId, PopUpDefinition>();
    private readonly HashSet<PopUpId> shownOnce = new HashSet<PopUpId>();
    private readonly List<Entry> queue = new List<Entry>();
    private PopUpDefinition current;
    private object[] currentArgs;

    // cached references  ------------------------------------------------------
    private GameController gameController;

    // private properties  #####################################################
    // true w/o a GameController, so the scene works standalone
    private bool IsRunLive =>
        gameController == null || gameController.IsRunLive;

    private MessagePopUp CurrentView => ViewOf(current);

    // private methods  ########################################################
    private MessagePopUp ViewOf(PopUpDefinition popUp) {
        return popUp.Kind == PopUpKind.Fruit ? fruitView : messageView;
    }

    private void Present(Entry entry) {
        current = entry.popUp;
        currentArgs = entry.args;
        ViewOf(current).Show(current, entry.args, entry.remainingS);
    }

    // starts the queue front that may still show, dropping the stale ones
    private void PresentNext() {
        while (queue.Count > 0) {
            Entry next = queue[0];
            queue.RemoveAt(0);
            if (next.popUp.IsRunOnly && !IsRunLive) {
                continue;
            }
            Present(next);
            return;
        }
    }

    // by priority, first come first served within one priority
    private void Enqueue(Entry entry) {
        int at = queue.Count;
        for (int i = 0; i < queue.Count; i++) {
            if (queue[i].popUp.Priority < entry.popUp.Priority) {
                at = i;
                break;
            }
        }
        queue.Insert(at, entry);

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\tqueued " + entry.popUp.Id, this);
        }
    }

    private void RemoveQueued(PopUpId id) {
        queue.RemoveAll(entry => entry.popUp.Id == id);
    }

    // one waiting popup w/ the args & the show time it still owes
    private readonly struct Entry {
        public readonly PopUpDefinition popUp;
        public readonly object[] args;
        public readonly float remainingS; // negative for a fresh show

        public Entry(PopUpDefinition popUp, object[] args, float remainingS) {
            this.popUp = popUp;
            this.args = args;
            this.remainingS = remainingS;
        }
    }
}
