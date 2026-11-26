using UnityEngine;

/// <summary>
/// The hardcore run's boss rush: every
/// <see cref="WorldBalanceConfig.HardcoreBossIntervalU"/> of climbed height a
/// boss waits, the Cobra King first, then the Spider King, taking turns, each
/// harder than the last: more health, shorter wind ups, warnings & rests,
/// faster shots & dashes, the same moves. The monkey & the great cobra climb
/// <see cref="WorldBalanceConfig.HardcoreSpeedScale"/> times as fast all run
/// long, fights included, so every boss there is quicker & tougher to match.
/// Both kings ask it where & how hard their next fight is.
/// </summary>
public static class HardcoreBossRush {
    // Public Members  #########################################################
    /// <returns>if this run is the hardcore run, so the boss rush is on
    /// </returns>
    public static bool IsOn => GameController.I != null
        && GameController.I.Mode == RunMode.Hardcore;

    /// <returns>scale on the monkey's & the great cobra's climb now: the
    /// hardcore speed, in & out of a fight, else 1</returns>
    public static float ClimbSpeedScale {
        get {
            WorldBalanceConfig config = Config;
            if (!IsOn || config == null) {
                return 1f;
            }
            return config.HardcoreSpeedScale;
        }
    }

    /// <returns>if either king's fight is on now, from its start to its
    /// reward, not once won & the climb goes on; in a normal or a hardcore
    /// run alike</returns>
    public static bool IsFightOn {
        get {
            CobraKingFightController cobraKing = CobraKingFightController.I;
            SpiderKingFightController spiderKing = SpiderKingFightController.I;
            return (cobraKing != null && cobraKing.IsFighting)
                || (spiderKing != null && spiderKing.IsInArena);
        }
    }

    // Public Methods  #########################################################
    /// <param name="encounter">0 for the first boss of the run, 1 for the
    /// second, ...; even ones are the Cobra King's, odd ones the Spider
    /// King's</param>
    /// <returns>climbed height that boss's fight begins at; u</returns>
    public static float TriggerU(int encounter) {
        WorldBalanceConfig config = Config;
        float intervalU = config != null ? config.HardcoreBossIntervalU : 100f;
        return intervalU * (encounter + 1);
    }

    /// <param name="encounter">the boss's encounter</param>
    /// <returns>the Loop of that fight, counted per king: 1 for each king's
    /// first fight, which plays just as its Normal Play fight, 2 for its
    /// second, ...</returns>
    public static int LoopOf(int encounter) {
        return Mathf.Max(0, encounter) / 2 + 1;
    }

    /// <param name="encounter">the boss's encounter</param>
    /// <returns>that fight's arena: its trigger height, one boss interval
    /// per encounter, & the hardcore arena height, so the shared builder
    /// derives the rest</returns>
    public static BossArenaLayout Layout(int encounter) {
        WorldBalanceConfig config = Config;
        float heightU = config != null ? config.HardcoreArenaHeightU : 40f;
        return new BossArenaLayout(TriggerU(encounter), heightU);
    }

    /// <param name="encounter">the boss's encounter</param>
    /// <returns>world y of that fight's start; the run's start y plus its
    /// trigger</returns>
    public static float TriggerWorldY(int encounter) {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        return (ramp != null ? ramp.PlayerGameStartYPosition : 0f)
            + TriggerU(encounter);
    }

    // private members  ########################################################
    private static WorldBalanceConfig Config {
        get {
            RampedDifficultyManager ramp = RampedDifficultyManager.I;
            return ramp != null ? ramp.WorldBalanceConfig : null;
        }
    }
}
