using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the popup system of one scene: shows one <see cref="MessagePopUp"/>
/// at a time, by <see cref="PopUpId"/>. A higher priority interrupts the
/// showing popup, which returns to the queue front with its time left; an
/// equal or lower one queues. It also gates run-only popups on the run being
/// live, remembers the once-per-scene ones, and drops everything on a loss.
/// <para>
/// Put it on the always-active popup root: every variant under it is saved
/// inactive and found by this script, its only owner.
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
    /// <param name="id">the popup variant</param>
    /// <param name="args">values for the <c>{0}</c> slots of its text</param>
    public void Show(PopUpId id, params object[] args) {
        if (!lookup.TryGetValue(id, out MessagePopUp popUp)) {
            Debug.LogWarning("PopUp:\tno popup mapped to id " + id, this);
            return;
        }
        if (popUp.IsRunOnly && !IsRunLive) {
            return;
        }
        if (popUp.IsOncePerScene && !shownOnce.Add(id)) {
            return;
        }

        // the same popup again: refresh it in place, never twice on screen
        if (current == popUp) {
            popUp.Show(args);
            return;
        }
        RemoveQueued(id);

        if (current == null) {
            Present(new Entry(popUp, args, -1f));
        } else if (popUp.Priority > current.Priority) {
            queue.Insert(
                0, new Entry(current, currentArgs, current.RemainingS));
            current.Stop();
            Present(new Entry(popUp, args, -1f));
        } else {
            Enqueue(new Entry(popUp, args, -1f));
        }
    }

    /// <summary>
    /// Dismisses a popup: a showing one fades out, a queued one is dropped.
    /// </summary>
    /// <param name="id">the popup variant</param>
    public void Hide(PopUpId id) {
        if (current != null && current.Id == id) {
            current.Hide();
            return;
        }
        RemoveQueued(id);
    }

    /// <summary>
    /// Finds a popup variant, so its owner can reach the variant's other
    /// components, e.g. <see cref="PopUpWorldLink"/>.
    /// </summary>
    /// <param name="id">the popup variant</param>
    /// <returns>the variant, or null when no variant has the id</returns>
    public MessagePopUp GetPopUp(PopUpId id) {
        return lookup.TryGetValue(id, out MessagePopUp popUp) ? popUp : null;
    }
    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (I != null && I != this) {
            Debug.LogWarning(
                "PopUp:\tduplicate PopUpManager, this one is off", this);
            enabled = false;
            return;
        }
        I = this;

        // inactive variants included: they are saved off until first shown
        MessagePopUp[] popUps = GetComponentsInChildren<MessagePopUp>(true);
        if (popUps.Length == 0) {
            Debug.LogWarning("PopUp:	no MessagePopUp under this root", this);
        }
        foreach (MessagePopUp popUp in popUps) {
            if (!lookup.TryAdd(popUp.Id, popUp)) {
                Debug.LogWarning(
                    "PopUp:	id used by two variants: " + popUp.Id, this);
                continue;
            }
            popUp.Hidden += OnPopUpHidden;
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
        foreach (MessagePopUp popUp in lookup.Values) {
            if (popUp != null) {
                popUp.Hidden -= OnPopUpHidden;
            }
        }
        if (I == this) {
            I = null;
        }
    }

    // Event Handlers  #########################################################
    private void OnPopUpHidden(MessagePopUp popUp) {
        if (popUp != current) {
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
            current.Stop();
            current = null;
        }

        if (Debug.isDebugBuild) {
            Debug.Log("PopUp:\tloss, all dropped", this);
        }
    }

    // private members  ########################################################
    private readonly Dictionary<PopUpId, MessagePopUp> lookup =
        new Dictionary<PopUpId, MessagePopUp>();
    private readonly HashSet<PopUpId> shownOnce = new HashSet<PopUpId>();
    private readonly List<Entry> queue = new List<Entry>();
    private MessagePopUp current;
    private object[] currentArgs;

    // cached references  ------------------------------------------------------
    private GameController gameController;

    // private properties  #####################################################
    // true w/o a GameController, so the scene works standalone
    private bool IsRunLive =>
        gameController == null || gameController.IsRunLive;

    // private methods  ########################################################
    private void Present(Entry entry) {
        current = entry.popUp;
        currentArgs = entry.args;
        current.Show(entry.args, entry.remainingS);
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
        public readonly MessagePopUp popUp;
        public readonly object[] args;
        public readonly float remainingS; // negative for a fresh show

        public Entry(MessagePopUp popUp, object[] args, float remainingS) {
            this.popUp = popUp;
            this.args = args;
            this.remainingS = remainingS;
        }
    }
}
