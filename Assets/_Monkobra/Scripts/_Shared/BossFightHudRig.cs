using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct BossFightHudPop {
    public TextMeshProUGUI label;
    public Vector3 worldPosition;
    public float ageS;
}

/// <summary>What a flying -1 stands for.</summary>
public enum BossFightHudLoss {
    Heart,
    Armor
}

public struct BossFightHudLossFlight {
    public TextMeshProUGUI label;
    public BossFightHudLoss kind;
    public Vector3 worldPosition;
    // the heart that dims on arrival
    public int heartIndex;
    public float ageS;
}

public struct BossFightHudDamageFlight {
    public TextMeshProUGUI label;
    public Vector3 worldPosition;
    // the boss health the bar drops to as the number lands
    public int health;
    public int maxHealth;
    public float ageS;
}

public struct BossFightHudWarning {
    public int id;
    public RectTransform sign;
    // the diamond & "!" together, the only part that shakes
    public RectTransform mark;
    public Vector3 worldPosition;
    public float progress;
    // 0 when shown, 1 at the hit
}

/// <summary>
/// The state every Cobra King role shares, plus the geometry queries
/// more than one role asks of it.
/// </summary>
public sealed class BossFightHudRig {
    // constants  #############################################################
    /// <summary>Real s a call out takes from its head to its dock.</summary>
    public const float CALL_OUT_S = CALL_AT_HEAD_S + CALL_GLIDE_S;
    // a call out's beats, real s
    public const float CALL_AT_HEAD_S = 0.9f;
    public const float CALL_GLIDE_S = 0.6f;
    public const float POP_S = 0.9f;
    // real s the win panel is up b4 it can be closed, so a Space still
    // mashed from the fight never skips it
    public const float REWARD_READY_S = 0.5f;
    public const float ARMOR_BADGE_POP_S = 0.45f;

    // Shared State  ##########################################################
    public readonly MonoBehaviour owner;
    public readonly Transform host;
    public readonly BossFightHudConfig config;
    public BossFightHudBuilder Builder;
    public BossFightHudMotion Motion;
    public RectTransform callOutRect;
    public TextMeshProUGUI callOutLabel;
    public Vector3 callOutWorld;
    public float callOutAgeS;
    public readonly List<BossFightHudPop> pops = new List<BossFightHudPop>();
    public readonly List<BossFightHudWarning> warnings =
        new List<BossFightHudWarning>();
    public int nextWarningId;
    public float flashLeftS;
    public Color flashColor = Color.white;
    public float speechLeftS;
    public Vector3 speechWorldPosition;
    // cached references  ------------------------------------------------------
    public RectTransform canvasRect;
    public Camera canvasCamera;
    // null for an overlay canvas
    public RectTransform rootRect;
    public RectTransform healthFill;
    // the heart left of the health bar & its count of the health left
    public RectTransform heart;
    public TextMeshProUGUI heartLabel;
    public int heartHealth = -1;
    public float heartPopLeftS;
    public TextMeshProUGUI nameLabel;
    // real s since the name's intro began; negative once it has docked
    public float nameIntroAgeS = -1f;
    public RectTransform speechRect;
    public TextMeshProUGUI speechLabel;
    public KeyPromptWidget keyPrompt;
    public TextMeshProUGUI bannerLabel;
    public GameObject revivePanel;
    public TextMeshProUGUI reviveCountLabel;
    public GameObject adPanel;
    public RectTransform adTitle;
    public readonly List<RectTransform> adShapes = new List<RectTransform>();
    public GameObject adSkipButton;
    public RectTransform armorBadge;
    // the monkey's heart row in the stamina bar's place, & its armor mark
    public RectTransform monkeyHeartRow;
    public readonly List<Image> monkeyHeartImages = new List<Image>();
    public RectTransform armorMark;
    public bool isArmorShown;
    public int monkeyHeartsShown = -1;
    // a golden grab's armor sprite still flying to the mark: the armor counts
    // as worn, its mark shows & pops on landing
    public bool isArmorGainPending;
    public float armorGainDeadlineS;
    public const float ARMOR_GAIN_MAX_S = 2.5f;
    // -1 numbers flying to the heart row or the armor mark
    public readonly List<BossFightHudLossFlight> lossFlights =
        new List<BossFightHudLossFlight>();
    public System.Action<BossFightHudLossFlight> LossLanded;
    // -n numbers flying to the boss health bar
    public readonly List<BossFightHudDamageFlight> damageFlights =
        new List<BossFightHudDamageFlight>();
    public System.Action<BossFightHudDamageFlight> DamageLanded;
    // real s a heart loss waits for its flight before it dims anyway
    public float heartGraceLeftS;
    public const float HEART_GRACE_S = 0.15f;
    public int monkeyHeartLostIndex = -1;
    public float monkeyHeartPopLeftS;
    public GameObject rewardPanel;
    public float rewardAgeS;
    public TextMeshProUGUI rewardTitle;
    public TextMeshProUGUI rewardLine;
    public TextMeshProUGUI rewardDetail;
    public RectTransform rewardButton;
    public float armorBadgePopS;
    // pop-in time left; unscaled s
    public readonly Vector2 bannerCenterPosition = new Vector2(0f, 120f);
    public readonly Vector2 bannerLowPosition = new Vector2(0f, -150f);
    public float bannerHideTime = -1f;
    // unscaled time a timed banner goes
    public Image flashImage;

    public BossFightHudRig(MonoBehaviour owner, BossFightHudConfig config) {
        this.owner = owner;
        host = owner.transform;
        this.config = config;
    }

    // Public Methods  #########################################################
}
