using UnityEngine;

/// <summary>
/// Maps player height to a ramped difficulty value in 3 stages: 0~1 from the
/// starting zone exit to the Cobra King fight, 1~2 between the 2 boss
/// fights, above 2 after the Spider King fight.
/// </summary>
// run first so other LateUpdate readers see this frame's height
[DefaultExecutionOrder(-100)]
public class RampedDifficultyManager: MonoBehaviour {
    // Public Members  #########################################################
    public static RampedDifficultyManager I { get; private set; }
    // player y at game start; world u
    public float PlayerGameStartYPosition { get; private set; }
    // player y above start y, refreshed every frame; world u
    public float CurrentPlayerClimbedYDistance { get; private set; }
    // highest CurrentPlayerClimbedYDistance so far, never below 0; world u
    public float HighestPlayerClimbedYDistance { get; private set; }

    /// <summary>
    /// World y where the ramp sits at 0: the y of the starting zone exit
    /// trigger, falling back to <see cref="PlayerGameStartYPosition"/> while
    /// none is assigned. Climbed height for score and display is unaffected.
    /// </summary>
    public float RampBaseYPosition =>
        rampBase != null ? rampBase.position.y : PlayerGameStartYPosition;

    /// <returns>the player's transform, null if none was found</returns>
    public Transform PlayerTransform => player;

    // Public Methods  #########################################################
    /// <summary>
    /// Height of <paramref name="y"/> above the player's game start y, the
    /// one baseline every climb height is measured from. A future world
    /// origin shift only has to move <see cref="PlayerGameStartYPosition"/>
    /// along with the world.
    /// </summary>
    /// <param name="y">any y position; world u</param>
    /// <returns>climbed height, negative below the start; world u</returns>
    public float GetClimbedYDistance(float y) {
        return y - PlayerGameStartYPosition;
    }

    /// <param name="y">any y position; world u</param>
    /// <returns>if <paramref name="y"/> lies below the starting zone exit,
    /// ie in the starting zone</returns>
    public bool IsInStartingZoneY(float y) {
        return y < RampBaseYPosition;
    }

    /// <returns>the world balance tuning this reads, eg for the hardcore
    /// boss rush; null if unassigned</returns>
    public WorldBalanceConfig WorldBalanceConfig => worldBalanceConfig;

    /// <summary>
    /// Keeps a world height range bare like the boss zone till
    /// <see cref="ClearExtraBossZone"/>, eg a boss arena outside the zone;
    /// one range per <paramref name="owner"/>, so two bosses never undo each
    /// other's.
    /// </summary>
    public void SetExtraBossZone(Object owner, float bottomY, float topY) {
        extraBossZones[owner] = new Vector2(
            Mathf.Min(bottomY, topY), Mathf.Max(bottomY, topY));
    }

    /// <summary>Ends the range <paramref name="owner"/> set by
    /// <see cref="SetExtraBossZone"/>.</summary>
    public void ClearExtraBossZone(Object owner) {
        extraBossZones.Remove(owner);
    }

    /// <param name="y">any y position; world u</param>
    /// <returns>if <paramref name="y"/> lies in the boss zone, from its start
    /// to its end in climbed height, where only the boss fight generates
    /// anything</returns>
    public bool IsInBossZoneY(float y) {
        // a fight's own arena, eg the Spider King's test fight near the start
        // or a boss of the hardcore run's boss rush
        foreach (Vector2 zone in extraBossZones.Values) {
            if (y >= zone.x && y <= zone.y) {
                return true;
            }
        }
        // the hardcore tree has no boss zone, so no hazard-free gap
        if (worldBalanceConfig == null || IsHardcore) {
            return false;
        }
        float climbedY = GetClimbedYDistance(y);
        bool isInCobraZone = climbedY >= worldBalanceConfig.BossZoneStartU
            && climbedY <= worldBalanceConfig.BossZoneEndU;
        // the Spider King's zone higher up is kept just as clear
        bool isInSpiderZone = climbedY >= worldBalanceConfig.SpiderZoneStartU
            && climbedY <= worldBalanceConfig.SpiderZoneEndU;
        return isInCobraZone || isInSpiderZone;
    }

    /// <summary>
    /// Maps a height to the ramped difficulty: 0 up to the starting zone exit,
    /// the pre boss curve from there to the Cobra King zone start (0~1), a
    /// flat 1 inside that zone, the mid boss curve from its end to the Spider
    /// King zone start (1~2), a flat 2 inside that zone, the post boss curve
    /// from its end (2 up). Zone heights are climbed heights, measured from
    /// the player's start y.
    /// </summary>
    /// <param name="y">any y position; world u</param>
    /// <returns>ramped difficulty, never below 0; 1~2 between the 2 boss
    /// zones, above 2 past the Spider King zone</returns>
    public float GetRampedValueFromYPosition(float y) {
        if (worldBalanceConfig == null) {
            return 0f;
        }
        float climbedY = GetClimbedYDistance(y);
        float exitClimbedY = GetClimbedYDistance(RampBaseYPosition);
        if (IsHardcore) {
            // hardcore opens at the post Spider King level at the exit and
            // keeps ramping from there
            return climbedY < exitClimbedY ? 0f : GetPostSpiderValue(
                climbedY - exitClimbedY + worldBalanceConfig.SpiderZoneEndU
            );
        }
        float zoneStartU = worldBalanceConfig.BossZoneStartU;
        if (climbedY < exitClimbedY || zoneStartU <= exitClimbedY) {
            // a zone start at or under the exit leaves no pre boss stretch
            return climbedY < zoneStartU ? 0f : GetBossStagesValue(climbedY);
        }
        if (climbedY < zoneStartU) {
            float progress =
                (climbedY - exitClimbedY) / (zoneStartU - exitClimbedY);
            return Mathf.Max(
                0f,
                worldBalanceConfig.PreBossRampCurve.Evaluate(progress)
            );
        }
        return GetBossStagesValue(climbedY);
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("world balance tuning; ramp curve")]
    private WorldBalanceConfig worldBalanceConfig;

    [SerializeField]
    [Tooltip("starting zone exit trigger; its y is where the ramp starts at 0")]
    private Transform rampBase;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (worldBalanceConfig == null) {
            Debug.LogError(
                "RampedDifficulty:\tmust assign Inspector Field: "
                + "worldBalanceConfig", this);
        }
        if (rampBase == null) {
            Debug.LogWarning(
                "RampedDifficulty:\tmust assign Inspector Field: rampBase, "
                + "ramp starts at player start y", this);
        }

        if (I != null && I != this) {
            if (Debug.isDebugBuild) {
                Debug.Log("RampedDifficulty:\tduplicate removed");
            }
            Destroy(this);
            return;
        }
        I = this;

        GameObject playerObj = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObj == null) {
            Debug.LogError(
                "RampedDifficulty:\tfail to find GameObject tagged: "
                + PLAYER_TAG, this);
        } else {
            player = playerObj.transform;
        }
        if (Debug.isDebugBuild) {
            Debug.Log("RampedDifficulty:\tready");
        }
    }

    private void Start() {
        if (player == null) {
            return;
        }
        PlayerGameStartYPosition = player.position.y;
        if (
            worldBalanceConfig != null
            && worldBalanceConfig.BossZoneStartU
                <= GetClimbedYDistance(RampBaseYPosition)
        ) {
            Debug.LogWarning(
                "RampedDifficulty:\tboss zone starts at or below the starting "
                + "zone exit, the pre boss ramp is skipped", this);
        }
        if (Debug.isDebugBuild) {
            Debug.Log("RampedDifficulty:\tplayer start y: "
                + PlayerGameStartYPosition);
        }
    }

    private void LateUpdate() {
        if (player == null) {
            return;
        }
        CurrentPlayerClimbedYDistance = GetClimbedYDistance(player.position.y);
        HighestPlayerClimbedYDistance = Mathf.Max(
            HighestPlayerClimbedYDistance,
            CurrentPlayerClimbedYDistance
        );
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    // constants  ##############################################################
    private const string PLAYER_TAG = "Player";

    // cached references  ######################################################
    private Transform player;

    // private methods  ########################################################
    // if this scene plays the hardcore run
    private bool IsHardcore =>
        GameController.I != null
            && GameController.I.Mode == RunMode.Hardcore;

    // from the Cobra King zone start on: 1 through its zone, the mid boss
    // curve to the Spider King zone start, then the post Spider King value
    private float GetBossStagesValue(float climbedY) {
        float cobraEndU = worldBalanceConfig.BossZoneEndU;
        float spiderStartU = worldBalanceConfig.SpiderZoneStartU;
        if (climbedY <= cobraEndU) {
            return 1f;
        }
        if (climbedY < spiderStartU) {
            float progress =
                (climbedY - cobraEndU) / (spiderStartU - cobraEndU);
            return Mathf.Max(
                1f,
                worldBalanceConfig.MidBossRampCurve.Evaluate(progress)
            );
        }
        return GetPostSpiderValue(climbedY);
    }

    // 2 through the Spider King zone, the post boss curve past its end
    private float GetPostSpiderValue(float climbedY) {
        float zoneEndU = worldBalanceConfig.SpiderZoneEndU;
        if (climbedY <= zoneEndU) {
            return 2f;
        }
        return Mathf.Max(
            2f,
            worldBalanceConfig.PostBossRampCurve.Evaluate(climbedY - zoneEndU)
        );
    }

    // the bare ranges set by boss fights, bottom & top world y, per fight
    private readonly System.Collections.Generic.Dictionary<Object, Vector2>
        extraBossZones = new System.Collections.Generic.Dictionary<Object,
            Vector2>();
}
