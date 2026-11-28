using System;
using UnityEngine;

/// <summary>
/// The cut scene that opens a boss fight: the cinema bars come in, the
/// camera frames the monkey and the boss from below (the spider intro
/// camera's framing, <see cref="CameraSpiderIntroFraming"/>), the monkey's
/// input is locked, and a boss animation may play through
/// <see cref="Playing"/>; as the shot ends the bars draw out, the UI is back
/// and <c>onFinished</c> runs, which shows the boss name.
/// <para>
/// A plain helper owned by a fight controller, which calls
/// <see cref="Tick"/> every frame while <see cref="IsPlaying"/>. The scene
/// skips by <see cref="TutorialSkipManager"/> (the dev skip box, or seen
/// once this session): then <c>onFinished</c> runs at once, from
/// <see cref="Begin"/>, so the boss name always plays.
/// </para>
/// </summary>
public sealed class BossIntroCutScene {
    // Public Members  #########################################################
    /// <returns>if the intro shot is live: the fight waits, the monkey is
    /// locked & the bars show</returns>
    public bool IsPlaying {
        get; private set;
    }

    /// <summary>
    /// Raised each frame the scene plays, w/ the seconds since it began: the
    /// hook for the boss animation.
    /// </summary>
    public event Action<float> Playing;

    /// <returns>how long the scene lasts; s, 0 when it is not playing
    /// </returns>
    public float HoldS => IsPlaying ? holdS : 0f;

    // Public Methods  #########################################################
    /// <summary>
    /// Opens the scene, or, when it is skipped or the camera rig is missing,
    /// finishes it at once.
    /// </summary>
    /// <param name="bossPosition">where the boss is now, the point the
    /// camera frames beside the monkey</param>
    /// <param name="item">the tutorial item asked of the skip manager</param>
    /// <param name="onFinished">runs once the scene is over, or at once
    /// when it never plays</param>
    public void Begin(
        Func<Vector3> bossPosition, TutorialItem item, Action onFinished
    ) {
        position = bossPosition;
        finished = onFinished;
        TutorialSkipManager skip = TutorialSkipManager.I;
        rig = UnityEngine.Object.FindFirstObjectByType<CameraRigManager>();
        framing =
            UnityEngine.Object.FindFirstObjectByType<CameraSpiderIntroFraming>();
        bool isSkipped = skip != null && skip.ShouldSkip(item);
        if (isSkipped || rig == null || framing == null) {
            if (!isSkipped && Debug.isDebugBuild) {
                Debug.LogWarning(
                    "BossIntroCutScene:\tno CameraRigManager or "
                        + "CameraSpiderIntroFraming, intro not played"
                );
            }
            Finish();
            return;
        }

        subject = new GameObject("BossIntroSubject").transform;
        subject.position = position();
        framing.SetSubject(subject);
        holdS = rig.BossIntroHoldS;
        if (!rig.RequestShot(CameraRigManager.ShotKind.BossIntro, holdS)) {
            Finish();
            return;
        }
        if (skip != null) {
            skip.MarkSeen(item);
        }
        elapsedS = 0f;
        IsPlaying = true;
    }

    /// <summary>
    /// Advances the scene one frame; the shot ends on its own at its hold,
    /// and the scene finishes as it does.
    /// </summary>
    public void Tick() {
        if (!IsPlaying) {
            return;
        }
        elapsedS += Time.deltaTime;
        subject.position = position();
        Playing?.Invoke(elapsedS);
        if (!rig.IsShotLive(CameraRigManager.ShotKind.BossIntro)) {
            Finish();
        }
    }

    // private members  ########################################################
    private Func<Vector3> position;
    private Action finished;
    private CameraRigManager rig;
    private CameraSpiderIntroFraming framing;
    private Transform subject;
    private float holdS;
    private float elapsedS;

    // private methods  ########################################################
    // frees the camera & runs the callback once
    private void Finish() {
        IsPlaying = false;
        if (framing != null) {
            framing.SetSubject(null);
        }
        if (subject != null) {
            UnityEngine.Object.Destroy(subject.gameObject);
            subject = null;
        }
        Action callback = finished;
        finished = null;
        callback?.Invoke();
    }
}
