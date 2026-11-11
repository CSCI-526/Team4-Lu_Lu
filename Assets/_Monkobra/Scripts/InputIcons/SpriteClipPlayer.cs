using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a sprite-sheet clip on the <see cref="Image"/> beside it, looping,
/// on unscaled time so a hint still moves under a paused or slowed game.
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteClipPlayer: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>if a clip is set and playing</returns>
    public bool IsPlaying => frames != null && frames.Length >= 2;

    // Public Methods  #########################################################
    /// <summary>
    /// Starts looping <paramref name="clip"/> from its first frame; fewer
    /// than 2 frames stops the clip instead.
    /// </summary>
    /// <param name="clip">frames in order</param>
    /// <param name="fps">speed; frames per s</param>
    public void Play(Sprite[] clip, float fps) {
        if (clip == null || clip.Length < 2 || fps <= 0f) {
            Stop();
            return;
        }
        frames = clip;
        frameSeconds = 1f / fps;
        elapsedS = 0f;
        Show(0);
    }

    /// <summary>Stops the clip and leaves the current frame showing.</summary>
    public void Stop() {
        frames = null;
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        image = GetComponent<Image>();
    }

    private void Update() {
        if (!IsPlaying) {
            return;
        }
        elapsedS += Time.unscaledDeltaTime;
        int frame = (int)(elapsedS / frameSeconds) % frames.Length;
        Show(frame);
    }

    // Private Methods  ########################################################
    private void Show(int frame) {
        if (frame != shownFrame || image.sprite != frames[frame]) {
            image.sprite = frames[frame];
            shownFrame = frame;
        }
    }

    // Private Members  ########################################################
    private Sprite[] frames;
    private float frameSeconds;
    private float elapsedS;
    private int shownFrame = -1;

    // cached references  ------------------------------------------------------
    private Image image;
}
