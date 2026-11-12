using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

// shows a short "+N" beside the score for each banana or coconut grab
// texts come from a pool cloned off the template child, so the font is
// whatever the prefab gives the template; code never sets one
public class ScoreBonusWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("hidden child text cloned for each popup; its position is where a popup starts")]
    private TMP_Text template;

    [SerializeField]
    [Tooltip("seconds to fade in")]
    private float fadeInSeconds = 0.12f;

    [SerializeField]
    [Tooltip("seconds fully visible")]
    private float holdSeconds = 0.5f;

    [SerializeField]
    [Tooltip("seconds to fade out")]
    private float fadeOutSeconds = 0.35f;

    [SerializeField]
    [Tooltip("UI units a popup rises over its whole life")]
    private float riseDistance = 60f;

    [SerializeField]
    [Tooltip("min UI units b/t 2 popups; an older popup is pushed up to keep it")]
    private float stackSpacing = 44f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (template == null) {
            Debug.LogError(
                "ScoreBonusWidget:	must assign Inspector Field: template",
                this);
            enabled = false;
            return;
        }
        template.gameObject.SetActive(false);
    }

    private void OnEnable() {
        scoreManager = ScoreManager.I;
        if (scoreManager == null) {
            return;
        }
        scoreManager.RewardAdded += OnRewardAdded;
    }

    private void OnDisable() {
        if (scoreManager != null) {
            scoreManager.RewardAdded -= OnRewardAdded;
            scoreManager = null;
        }
        ReleaseAll();
    }

    // scaled time: a pause freezes every popup mid fade
    private void Update() {
        // a lost run clears what is left
        if (scoreManager != null && scoreManager.IsRunEnded) {
            ReleaseAll();
            return;
        }
        float total = fadeInSeconds + holdSeconds + fadeOutSeconds;
        for (int i = active.Count - 1; i >= 0; i--) {
            Popup popup = active[i];
            popup.age += Time.deltaTime;
            if (popup.age >= total) {
                Release(i);
                continue;
            }
            Apply(popup, total);
        }
    }

    // private members  ########################################################
    private class Popup {
        public TMP_Text text;
        public float age;
        // extra rise from being pushed up by newer popups
        public float push;
    }

    private readonly List<Popup> active = new List<Popup>();
    private readonly Stack<Popup> pool = new Stack<Popup>();
    private ScoreManager scoreManager;

    // private methods  ########################################################
    private void OnRewardAdded(PickupKind kind, long points) {
        if (points <= 0) {
            return;
        }
        MakeRoomForNewest();
        Popup popup = pool.Count > 0 ? pool.Pop() : CreatePopup();
        popup.age = 0f;
        popup.push = 0f;
        popup.text.text =
            "+" + points.ToString("N0", CultureInfo.InvariantCulture);
        popup.text.gameObject.SetActive(true);
        active.Add(popup);
        Apply(popup, fadeInSeconds + holdSeconds + fadeOutSeconds);
    }

    // push older popups up so none sits closer than stackSpacing to the next
    private void MakeRoomForNewest() {
        float floor = stackSpacing;
        for (int i = active.Count - 1; i >= 0; i--) {
            Popup popup = active[i];
            float y = RiseAt(popup);
            if (y < floor) {
                popup.push += floor - y;
                y = floor;
            }
            floor = y + stackSpacing;
        }
    }

    private float RiseAt(Popup popup) {
        float total = fadeInSeconds + holdSeconds + fadeOutSeconds;
        float u = Mathf.Clamp01(popup.age / total);
        // ease out: fast start, settles near the top
        float eased = 1f - (1f - u) * (1f - u);
        return eased * riseDistance + popup.push;
    }

    private void Apply(Popup popup, float total) {
        RectTransform rect = (RectTransform)popup.text.transform;
        Vector2 start = ((RectTransform)template.transform).anchoredPosition;
        rect.anchoredPosition = start + Vector2.up * RiseAt(popup);

        float alpha;
        if (popup.age < fadeInSeconds) {
            alpha = popup.age / Mathf.Max(fadeInSeconds, 0.0001f);
        } else if (popup.age < fadeInSeconds + holdSeconds) {
            alpha = 1f;
        } else {
            float fadeOutAge = popup.age - fadeInSeconds - holdSeconds;
            alpha = 1f - fadeOutAge / Mathf.Max(fadeOutSeconds, 0.0001f);
        }
        popup.text.alpha = Mathf.Clamp01(alpha);
    }

    private Popup CreatePopup() {
        TMP_Text clone = Instantiate(template, template.transform.parent);
        clone.name = "ScoreBonusText";
        return new Popup { text = clone };
    }

    private void Release(int index) {
        Popup popup = active[index];
        active.RemoveAt(index);
        popup.text.gameObject.SetActive(false);
        pool.Push(popup);
    }

    private void ReleaseAll() {
        for (int i = active.Count - 1; i >= 0; i--) {
            Release(i);
        }
    }
}
