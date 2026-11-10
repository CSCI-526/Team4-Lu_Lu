using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Builds a fully mocked session and saves it as its own markdown file, so
/// the file layout can be checked w/o playing. The live session is untouched.
/// <para>
/// Every run is a small simulation of the real game: the monkey climbs on the
/// real speed curve, hazards and fruit are rolled per height from the real
/// difficulty ramp and chance curves, and the great cobra chases w/ its real
/// speed curves, surge and exit boost. A run ends where a real one would:
/// the cobra catches a monkey that lost too much time to branches, webs and
/// grabs. Skill, not a fixed script, sets how far each run gets, and the
/// monkey learns over the session like a real tester.
/// </para>
/// <para>
/// The tuning below is a mirror of the balance assets, so a retuned game
/// needs the mirror updated by hand; see the Analytics doc.
/// </para>
/// </summary>
public static class MockAnalyticsGenerator {
    // Public Methods  #########################################################
    /// <param name="folder">analytics folder to save into</param>
    /// <param name="now">moment the session is stamped w/ and the last run
    /// ends at</param>
    /// <param name="windowS">death cause window, as in
    /// <see cref="AnalyticsConfig.DeathCauseWindowS"/></param>
    /// <returns>path of the file written, empty if the write failed</returns>
    public static string SaveMockSession(
        string folder,
        DateTime now,
        float windowS
    ) {
        string stamp = now.ToString(
            "yyyy-MM-dd_HH_mm_ss",
            CultureInfo.InvariantCulture
        );
        string path = Path.Combine(folder, "0" + stamp + "-mock.md");
        try {
            Directory.CreateDirectory(folder);
        } catch (Exception e) {
            Debug.LogWarning("Analytics:\tfail to open mock folder: " + e);
            return string.Empty;
        }

        SessionData session = BuildMockSession(
            now,
            new System.Random(),
            windowS
        );
        bool isWritten = AnalyticsSessionFile.WriteFile(
            path,
            AnalyticsMarkdownWriter.BuildSession(session)
        );

        return isWritten ? path : string.Empty;
    }

    /// <returns>session of <see cref="MIN_RUNS"/> to
    /// <see cref="MAX_RUNS"/> runs, all lost but the last, which is cut short;
    /// the final run ends at <paramref name="now"/></returns>
    public static SessionData BuildMockSession(
        DateTime now,
        System.Random rng,
        float windowS
    ) {
        int runCount = rng.Next(MIN_RUNS, MAX_RUNS + 1);
        // the tester: where they start, where they peak, how fast they learn
        float baseSkill = Range(rng, 0.1f, 0.35f);
        float capSkill = Range(rng, 0.55f, 0.92f);
        float learnRuns = Range(rng, 3f, 5f);
        // a returning player already beat the king, so Endless is open
        bool isEndlessOpen = rng.NextDouble() < RETURNING_PLAYER_CHANCE;

        List<RunPlan> plans = new List<RunPlan>();
        float total = 0f;
        for (int i = 0; i < runCount; i++) {
            float skill = baseSkill
                + (capSkill - baseSkill) * (1f - Mathf.Exp(-i / learnRuns))
                + Range(rng, -0.08f, 0.08f);
            // a distracted run
            if (rng.NextDouble() < TILT_CHANCE) {
                skill -= 0.2f;
            }
            skill = Mathf.Clamp(skill, 0.05f, 1f);

            bool isEndless = isEndlessOpen && rng.NextDouble() < 0.5;
            RunPlan plan = new RunSim(skill, isEndless, rng).Run();
            // a few seconds on the lose screen and the tutorial b4 the next
            plan.GapS = Range(rng, 4f, 12f);
            if (plan.IsBossWon) {
                isEndlessOpen = true;
            }
            if (i == runCount - 1) {
                CutShort(plan, Range(rng, 0.3f, 0.8f));
            }
            plans.Add(plan);
            total += plan.DurationS + plan.GapS;
        }

        DateTime sessionStart = now.AddSeconds(-total);
        SessionData session = new SessionData {
            StartClock = sessionStart,
            Title = sessionStart.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            ) + " (mock)",
            AppVersion = Application.version
        };

        DateTime cursor = sessionStart;
        foreach (RunPlan plan in plans) {
            session.Runs.Add(BuildRun(plan, cursor, windowS, rng));
            cursor = cursor.AddSeconds(plan.DurationS + plan.GapS);
        }

        return session;
    }

    // constants  ##############################################################
    private const int MIN_RUNS = 6;
    private const int MAX_RUNS = 14;
    private const float RETURNING_PLAYER_CHANCE = 0.25f;
    private const float TILT_CHANCE = 0.12f;
    private const float STEP_S = 0.1f;
    private const float MAX_RUN_S = 600f;
    // mirrors the default of AnalyticsConfig.SampleIntervalS
    private const float SAMPLE_INTERVAL_S = 2f;

    // game tuning mirrored from the balance assets  ------------
    // PlayerBalanceConfig
    private const float MAX_STAMINA = 100f;
    private const float UPWARD_DRAIN_PER_U = 0.5f;
    private const float SIDEWAY_DRAIN_PER_U = 0.3f;
    private const float FRUIT_RESTORE = 35f;
    private const float ZONE_START_STAMINA_FRACTION = 0.3f;
    private const float ZONE_EXHAUSTED_MULTIPLIER = 0.5f;
    private const float ZONE_RECOVERY_MULTIPLIER = 0.25f;
    private const float HIT_DROP_U = 10f;
    private const float HIT_DROP_SPEED_U = 8f;
    private const float HIT_STUN_S = 0.75f;
    private const float UNITS_PER_METER = 0.1f;
    private const int POINTS_PER_METER = 1;
    private const int REWARD_POINTS = 100;
    private const int COCONUT_POINTS = 200;
    private const float COBRA_FREEZE_S = 10f;
    // MonkeyClimbConfig
    private const float EXHAUSTED_SPEED_MULTIPLIER = 0.2f;
    private const float IDLE_RECOVERY_PER_S = 2.5f;
    private const float TOP_CLIMB_SPEED_U = 6f;
    // PineappleLeapConfig
    private const float LEAP_DISTANCE_U = 20f;
    private const float LEAP_S = 1.35f;
    // WorldBalanceConfig
    private const float ROUGH_BARK_SPEED_MULTIPLIER = 0.35f;
    private const float BOSS_START_U = 500f;
    private const float BOSS_TRIGGER_U = 525f;
    private const float BOSS_END_U = 550f;
    private const float PINEAPPLE_GUARANTEED_U = 150f;
    private const float COCONUT_GUARANTEED_U = 250f;
    private const float POST_BOSS_RAMP_PER_U = 0.002f;
    private const int EMPTY_ATTEMPTS = 10;
    private const int BANANA_ATTEMPTS = 10;
    private const int COCONUT_ATTEMPTS = 5;
    private const int PINEAPPLE_ATTEMPTS = 5;
    private const int BARK_ATTEMPTS = 5;
    private const int WEB_ATTEMPTS = 3;
    private const float ZONE_BANANA_CHANCE = 0.4f;
    // CobraBalanceConfig
    private const int FALLING_COBRA_ATTEMPTS = 3;
    private const float SURGE_PER_MISTAKE_S = 4f;
    private const float MAX_SURGE_S = 10f;
    private const float SURGE_SPEED_PER_MISTAKE_U = 2.5f;
    private const float MAX_SURGE_SPEED_U = 9f;
    private const float SPEED_CHANGE_RATE_U = 6f;
    private const float EXIT_BOOST_SPEED_U = 3f;
    private const float EXIT_BOOST_S = 5f;
    private const float ARMOR_FREEZE_S = 15f;
    private const float ARMOR_KNOCK_BACK_U = 20f;
    private const float WIN_COBRA_DROP_U = 60f;

    // assumptions the assets do not state  -----------
    // y of the starting zone exit above the start
    private const float ZONE_EXIT_U = 20f;
    // cobra head below the monkey at the start
    private const float START_GAP_U = 30f;
    // height of one trunk segment, the unit of every per segment roll
    private const float SEGMENT_U = 20f;

    // curves mirrored from the balance assets, flat x, y pairs  ---
    private static readonly float[] CLIMB_CURVE = {
        0f, 1.5f, 0.5f, 4.99f, 1.5f, 6f
    };
    private static readonly float[] EMPTY_CURVE = {
        0f, 0.1f, 0.183f, 0.113f, 0.321f, 0.068f, 0.397f, 0.057f, 0.497f,
        0.058f, 0.597f, 0.058f, 0.631f, 0.0625f, 1f, 0.1f, 2f, 0.3f
    };
    private static readonly float[] BANANA_CURVE = {
        0f, 0.06f, 0.103f, 0.0747f, 0.183f, 0.0618f, 0.283f, 0.0778f, 0.383f,
        0.0858f, 0.483f, 0.0818f, 0.583f, 0.0763f, 1f, 0.074f, 1.256f,
        0.0851f, 1.52f, 0.0897f, 2f, 0.1f
    };
    private static readonly float[] COCONUT_CURVE = {
        0f, 0f, 0.495f, 0f, 0.5f, 0.03f, 0.528f, 0.04f, 0.6f, 0.0325f, 1f,
        0.03f, 2f, 0.036f
    };
    private static readonly float[] PINEAPPLE_CURVE = {
        0f, 0f, 0.293f, 0f, 0.303f, 0.0343f, 0.353f, 0.0503f, 0.423f,
        0.0483f, 0.603f, 0.0368f, 1f, 0.0359f, 2f, 0.0376f
    };
    private static readonly float[] BARK_CURVE = {
        0f, 0f, 0.192f, 0f, 0.23f, 0.15f, 0.3f, 0.19f, 0.4f, 0.19f, 0.5f,
        0.165f, 0.6f, 0.14f, 2f, 0.3f
    };
    private static readonly float[] WEB_CURVE = {
        0f, 0f, 0.596f, 0f, 0.8f, 0.4f, 1f, 0.6f, 2f, 0.8f
    };
    private static readonly float[] FALLING_COBRA_CURVE = {
        0f, 0f, 0.394f, 0f, 0.47f, 0.14f, 0.65f, 0.07f, 1f, 0.2f, 2f, 0.35f
    };
    private static readonly float[] COBRA_BASE_CURVE = {
        0f, 0.5667f, 0.5f, 0.65f, 1f, 1f, 2f, 1.5f
    };
    private static readonly float[] COBRA_DISTANCE_CURVE = {
        0f, 0.3f, 10f, 0.4f, 20f, 0.8f, 54.09f, 0.85f, 60f, 1f, 62.706f,
        1.4066f, 100f, 2f
    };
    // share of each stacked mistake on the surge
    private static readonly float[] SURGE_STACK_SHARE = {
        1f, 0.5f, 0.25f, 0.125f, 0.0625f
    };

    // nested types  ###########################################################
    // one moment of a simulated run, seconds since run start
    private readonly struct Moment {
        public readonly float T;
        public readonly float Height;
        public readonly float Stamina;

        public Moment(float t, float height, float stamina) {
            T = t;
            Height = height;
            Stamina = stamina;
        }
    }

    // a simulated run, kept in run time until it is placed on the clock
    private class RunPlan {
        public float DurationS;
        public float GapS;
        public float EndHeight;
        public float HighestHeight;
        public bool IsLost = true;
        public bool IsBossWon;
        public RunMode Mode = RunMode.Full;
        public bool HasZoneExit;
        public Moment ZoneExit;
        public readonly List<Moment> Track = new List<Moment>();
        public readonly Dictionary<AnalyticsMetric, List<Moment>> Events =
            new Dictionary<AnalyticsMetric, List<Moment>>();

        public List<Moment> Get(AnalyticsMetric metric) {
            if (!Events.TryGetValue(metric, out List<Moment> list)) {
                list = new List<Moment>();
                Events[metric] = list;
            }

            return list;
        }
    }

    private enum Busy {
        None,
        Stun,
        Web,
        Grab,
        Rest,
        Leap
    }

    // the simulation of one run, a step at a time
    private sealed class RunSim {
        public RunSim(float skill, bool isEndless, System.Random rng) {
            this.skill = isEndless ? Mathf.Max(skill, 0.65f) : skill;
            this.isEndless = isEndless;
            this.rng = rng;
            plan.Mode = isEndless ? RunMode.Endless : RunMode.Full;
            stamina = MAX_STAMINA * ZONE_START_STAMINA_FRACTION;
            cobraY = -START_GAP_U;
            // endless starts in the armor a king win leaves
            hasArmor = isEndless;
        }

        /// <returns>the run, played until the cobra got the monkey</returns>
        public RunPlan Run() {
            plan.Track.Add(new Moment(0f, 0f, stamina));
            while (t < MAX_RUN_S) {
                t += STEP_S;
                float ramp = GetRamp(height);
                bool isInBossZone = !isEndless
                    && height >= BOSS_START_U
                    && height <= BOSS_END_U;

                MoveCobra(ramp);
                float climbedU = MoveMonkey();
                plan.HighestHeight = Mathf.Max(plan.HighestHeight, height);
                plan.Track.Add(new Moment(t, height, stamina));
                if (busy == Busy.Leap && t >= busyUntil) {
                    busy = Busy.None;
                }

                if (isInZone && height >= ZONE_EXIT_U) {
                    isInZone = false;
                    boostLeftS = EXIT_BOOST_S;
                    plan.HasZoneExit = true;
                    plan.ZoneExit = new Moment(t, height, stamina);
                }
                if (climbedU > 0f) {
                    if (isInZone) {
                        RollZoneFruit(climbedU);
                    } else if (!isInBossZone) {
                        RollHazards(ramp, climbedU);
                    }
                }
                TryRest();
                TryUseCoconut();

                if (!isInZone && height - cobraY <= 0f) {
                    if (!hasArmor) {
                        break;
                    }
                    SpendArmor();
                }
                if (
                    !isEndless
                    && !isBossDone
                    && height >= BOSS_TRIGGER_U
                ) {
                    isBossDone = true;
                    if (!FightBoss()) {
                        break;
                    }
                }
            }

            plan.DurationS = t;
            plan.EndHeight = plan.Track[plan.Track.Count - 1].Height;

            return plan;
        }

        // private members  ####################################################
        private readonly RunPlan plan = new RunPlan();
        private readonly float skill;
        private readonly bool isEndless;
        private readonly System.Random rng;

        private float t;
        private float height;
        private float stamina;
        private float cobraY;
        private float cobraSpeed;
        private float surgeLeftS;
        private int surgeStacks;
        private float boostLeftS;
        private float frozenLeftS;
        private bool isInZone = true;
        private bool hasArmor;
        private bool isBossDone;
        private Busy busy = Busy.None;
        private float busyUntil;
        private float dropLeftU;
        private float pausedUntil;
        private float holdS;
        private float barkUntil;
        private float webImmuneUntil;
        private int coconutsHeld;
        private bool hasCoconutGuarantee;
        private bool hasPineappleGuarantee;

        // private methods  ####################################################
        private float GetGap() {
            return height - cobraY;
        }

        // the ramped difficulty at a climbed height, as the real manager
        private float GetRamp(float y) {
            if (y < ZONE_EXIT_U) {
                return 0f;
            }
            if (isEndless) {
                return 1f + POST_BOSS_RAMP_PER_U * (y - ZONE_EXIT_U);
            }
            if (y < BOSS_START_U) {
                return (y - ZONE_EXIT_U) / (BOSS_START_U - ZONE_EXIT_U);
            }
            if (y <= BOSS_END_U) {
                return 1f;
            }

            return 1f + POST_BOSS_RAMP_PER_U * (y - BOSS_END_U);
        }

        private float GetRecovery() {
            return IDLE_RECOVERY_PER_S
                * (isInZone ? ZONE_RECOVERY_MULTIPLIER : 1f) * STEP_S;
        }

        // the chasing cobra: base speed by difficulty, scaled by the gap,
        // plus the surge and the exit boost, eased
        private void MoveCobra(float ramp) {
            if (isInZone) {
                return;
            }
            float target;
            if (frozenLeftS > 0f) {
                frozenLeftS -= STEP_S;
                target = 0f;
            } else {
                target = TOP_CLIMB_SPEED_U
                    * Curve(COBRA_BASE_CURVE, ramp)
                    * Curve(COBRA_DISTANCE_CURVE, Mathf.Max(GetGap(), 0f));
                if (surgeLeftS > 0f) {
                    surgeLeftS -= STEP_S;
                    float surge = 0f;
                    for (int i = 0; i < surgeStacks; i++) {
                        surge += SURGE_SPEED_PER_MISTAKE_U
                            * SURGE_STACK_SHARE[
                                Mathf.Min(i, SURGE_STACK_SHARE.Length - 1)
                            ];
                    }
                    target = Mathf.Min(
                        target + surge,
                        Mathf.Max(target, MAX_SURGE_SPEED_U)
                    );
                    if (surgeLeftS <= 0f) {
                        surgeStacks = 0;
                    }
                } else if (boostLeftS > 0f) {
                    target += EXIT_BOOST_SPEED_U;
                }
                if (boostLeftS > 0f) {
                    boostLeftS -= STEP_S;
                }
            }
            cobraSpeed = Mathf.MoveTowards(
                cobraSpeed,
                target,
                SPEED_CHANGE_RATE_U * STEP_S
            );
            cobraY += cobraSpeed * STEP_S;
        }

        // one step of the monkey; returns the height climbed under its own
        // power, 0 while stunned, held, grabbing, or resting
        private float MoveMonkey() {
            if (t < busyUntil) {
                if (busy == Busy.Stun) {
                    float drop = Mathf.Min(
                        dropLeftU,
                        HIT_DROP_SPEED_U * STEP_S
                    );
                    height = Mathf.Max(0f, height - drop);
                    dropLeftU -= drop;
                } else if (busy == Busy.Leap) {
                    height += LEAP_DISTANCE_U / LEAP_S * STEP_S;
                } else if (busy == Busy.Web || busy == Busy.Rest) {
                    stamina = Mathf.Min(MAX_STAMINA, stamina + GetRecovery());
                }

                return 0f;
            }
            if (t < pausedUntil) {
                holdS = 0f;
                stamina = Mathf.Min(MAX_STAMINA, stamina + GetRecovery());

                return 0f;
            }

            // a tester lets go of the key now and then, more so a novice
            if (rng.NextDouble() < (0.48f - 0.26f * skill) * STEP_S) {
                pausedUntil = t + Range(rng, 0.3f, 1.1f);
                holdS = 0f;
            }
            holdS += STEP_S;
            float speed = Curve(CLIMB_CURVE, holdS);
            if (stamina <= 0f) {
                speed *= isInZone
                    ? ZONE_EXHAUSTED_MULTIPLIER
                    : EXHAUSTED_SPEED_MULTIPLIER;
            }
            if (t < barkUntil) {
                speed *= ROUGH_BARK_SPEED_MULTIPLIER;
            }
            float climbedU = speed * STEP_S;
            height += climbedU;
            // a quarter of the climb is sideways dodging
            stamina = Mathf.Max(
                0f,
                stamina - climbedU
                    * (UPWARD_DRAIN_PER_U + SIDEWAY_DRAIN_PER_U * 0.25f)
            );

            return climbedU;
        }

        // the starting zone grows bananas only, and holds no hazard
        private void RollZoneFruit(float climbedU) {
            float perU = BANANA_ATTEMPTS * ZONE_BANANA_CHANCE / SEGMENT_U;
            if (
                rng.NextDouble() < perU * climbedU * 0.5
                && rng.NextDouble() < 0.6f + 0.3f * skill
            ) {
                TryGrabBanana();
            }
        }

        private void RollHazards(float ramp, float climbedU) {
            float perU = 1f / SEGMENT_U;
            float branchPerU = perU * (
                EMPTY_ATTEMPTS * Curve(EMPTY_CURVE, ramp)
                + BANANA_ATTEMPTS * Curve(BANANA_CURVE, ramp)
                + COCONUT_ATTEMPTS * Curve(COCONUT_CURVE, ramp)
                + PINEAPPLE_ATTEMPTS * Curve(PINEAPPLE_CURVE, ramp)
            );
            // a roll can place nothing, so fewer land than attempted
            float webPerU = perU * WEB_ATTEMPTS * Curve(WEB_CURVE, ramp) * 0.7f;
            float fallPerU = perU * FALLING_COBRA_ATTEMPTS
                * Curve(FALLING_COBRA_CURVE, ramp) * 0.8f;
            float barkPerU = perU * BARK_ATTEMPTS * Curve(BARK_CURVE, ramp)
                * 0.6f;
            float bananaPerU = perU * BANANA_ATTEMPTS
                * Curve(BANANA_CURVE, ramp);
            float pineapplePerU = perU * PINEAPPLE_ATTEMPTS
                * Curve(PINEAPPLE_CURVE, ramp);
            float coconutPerU = perU * COCONUT_ATTEMPTS
                * Curve(COCONUT_CURVE, ramp);

            // a novice walks into far more of them than an expert
            float branchHit = 0.27f - 0.21f * skill;
            float fallHit = 0.45f - 0.36f * skill;
            float webHit = 0.42f - 0.32f * skill;
            if (rng.NextDouble() < branchPerU * climbedU * branchHit) {
                HitByBranch();
            } else if (rng.NextDouble() < fallPerU * climbedU * fallHit) {
                HitByBranch();
            } else if (
                t >= webImmuneUntil
                && rng.NextDouble() < webPerU * climbedU * webHit
            ) {
                CatchInWeb();
            }

            if (rng.NextDouble() < barkPerU * climbedU * 0.5) {
                plan.Get(AnalyticsMetric.RoughBark)
                    .Add(new Moment(t, height, stamina));
                barkUntil = t + Range(rng, 1f, 2f);
            }
            if (rng.NextDouble() < bananaPerU * climbedU * 0.5) {
                float need = 1f - stamina / MAX_STAMINA;
                float chance = 0.35f + 0.45f * need + 0.15f * skill;
                // nobody reaches w/ the cobra on their heels
                if (GetGap() < 15f) {
                    chance *= 0.4f;
                }
                if (rng.NextDouble() < chance) {
                    TryGrabBanana();
                }
            }

            // the pineapple and the coconut each have a guaranteed spot
            bool isPineappleDue = !hasPineappleGuarantee
                && height >= PINEAPPLE_GUARANTEED_U;
            if (rng.NextDouble() < pineapplePerU * climbedU * 0.5
                || isPineappleDue) {
                hasPineappleGuarantee |= isPineappleDue;
                TryGrabPineapple();
            }
            bool isCoconutDue = !hasCoconutGuarantee
                && height >= COCONUT_GUARANTEED_U;
            if (rng.NextDouble() < coconutPerU * climbedU * 0.5
                || isCoconutDue) {
                hasCoconutGuarantee |= isCoconutDue;
                TryGrabCoconut();
            }
        }

        private void HitByBranch() {
            plan.Get(AnalyticsMetric.Branch)
                .Add(new Moment(t, height, stamina));
            busy = Busy.Stun;
            busyUntil = t + HIT_DROP_U / HIT_DROP_SPEED_U;
            dropLeftU = HIT_DROP_U;
            AddMistake();
        }

        private void CatchInWeb() {
            plan.Get(AnalyticsMetric.Web).Add(new Moment(t, height, stamina));
            // a novice mashes the escape key slower
            float escapeS = Range(rng, 1.5f, 3.5f)
                + (1f - skill) * Range(rng, 0f, 2f);
            busy = Busy.Web;
            busyUntil = t + escapeS;
            webImmuneUntil = t + escapeS + 1.5f;
            AddMistake();
        }

        // a mistake makes the cobra surge, in the zone it adds nothing
        private void AddMistake() {
            if (isInZone) {
                return;
            }
            surgeStacks++;
            surgeLeftS = Mathf.Min(
                MAX_SURGE_S,
                surgeLeftS + SURGE_PER_MISTAKE_S
            );
        }

        private void TryGrabBanana() {
            busy = Busy.Grab;
            busyUntil = t + Range(rng, 0.7f, 1.1f);
            if (rng.NextDouble() < 0.5f + 0.4f * skill) {
                stamina = Mathf.Min(MAX_STAMINA, stamina + FRUIT_RESTORE);
                plan.Get(AnalyticsMetric.Banana)
                    .Add(new Moment(t, height, stamina));
            }
        }

        // a pineapple pulls the monkey up past every hazard and tops up
        // stamina like a banana
        private void TryGrabPineapple() {
            if (rng.NextDouble() > 0.8 || busy == Busy.Leap
                || t < busyUntil) {
                return;
            }
            if (rng.NextDouble() < 0.55f + 0.4f * skill) {
                plan.Get(AnalyticsMetric.PineappleLeap)
                    .Add(new Moment(t, height, stamina));
                stamina = Mathf.Min(MAX_STAMINA, stamina + FRUIT_RESTORE);
                busy = Busy.Leap;
                busyUntil = t + LEAP_S;
            }
        }

        private void TryGrabCoconut() {
            if (rng.NextDouble() > 0.75 || t < busyUntil) {
                return;
            }
            if (rng.NextDouble() < 0.5f + 0.4f * skill) {
                plan.Get(AnalyticsMetric.Coconut)
                    .Add(new Moment(t, height, stamina));
                coconutsHeld++;
                busy = Busy.Grab;
                busyUntil = t + 0.9f;
            }
        }

        // a skilled tester rests on a safe stretch when the bar runs dry
        private void TryRest() {
            if (
                t >= busyUntil
                && stamina < 12f
                && GetGap() > 40f
                && skill > 0.35f
                && rng.NextDouble() < 0.3
            ) {
                busy = Busy.Rest;
                busyUntil = t + 2f;
            }
        }

        // a thrown coconut freezes the cobra, used when it gets close
        private void TryUseCoconut() {
            if (
                coconutsHeld <= 0
                || frozenLeftS > 0f
                || isInZone
                || GetGap() >= 22f
                || rng.NextDouble() >= (0.3f + 0.5f * skill) * STEP_S * 2f
            ) {
                return;
            }
            coconutsHeld--;
            plan.Get(AnalyticsMetric.CoconutUsed)
                .Add(new Moment(t, height, stamina));
            // the freeze lands as the coconut does
            plan.Get(AnalyticsMetric.CobraFreeze)
                .Add(new Moment(t + 0.8f, height, stamina));
            Freeze(COBRA_FREEZE_S);
        }

        private void Freeze(float seconds) {
            frozenLeftS = seconds;
            surgeStacks = 0;
            surgeLeftS = 0f;
            cobraSpeed = 0f;
        }

        // the gold armor takes the catch, throws the head back, freezes it
        private void SpendArmor() {
            hasArmor = false;
            plan.Get(AnalyticsMetric.BossArmorSaved)
                .Add(new Moment(t, height, stamina));
            plan.Get(AnalyticsMetric.CobraFreeze)
                .Add(new Moment(t, height, stamina));
            cobraY = height - ARMOR_KNOCK_BACK_U;
            Freeze(ARMOR_FREEZE_S);
        }

        // the boss fight is not climbed, so it is a few timed attempts,
        // each lost or won by skill; a win lets the run go on above the zone
        // return: if the monkey won the fight and the run goes on
        private bool FightBoss() {
            plan.Get(AnalyticsMetric.BossFight)
                .Add(new Moment(t, height, stamina));
            int revives = 0;
            while (true) {
                bool isLong = rng.NextDouble()
                    < 0.2f + 0.6f * Mathf.Max(0f, skill - 0.4f);
                float attemptS = isLong
                    ? Range(rng, 60f, 110f)
                    : Range(rng, 25f, 70f);
                float startT = t;
                AddFightTrack(startT, startT + attemptS);

                float winChance = 0.03f
                    + 0.45f * Mathf.Max(0f, skill - 0.5f);
                if (rng.NextDouble() < winChance) {
                    if (rng.NextDouble() < 0.5) {
                        AddFightEvent(
                            AnalyticsMetric.BossArmorSaved,
                            startT + attemptS * Range(rng, 0.3f, 0.8f)
                        );
                    }
                    plan.IsBossWon = true;
                    height = BOSS_END_U + 1f;
                    stamina = MAX_STAMINA;
                    cobraY = height - WIN_COBRA_DROP_U;
                    hasArmor = true;
                    busy = Busy.None;
                    Freeze(0f);
                    plan.Track.Add(new Moment(t, height, stamina));
                    plan.HighestHeight = Mathf.Max(
                        plan.HighestHeight,
                        height
                    );

                    return true;
                }

                // the first fatal hit is the starting armor's
                AddFightEvent(
                    AnalyticsMetric.BossArmorSaved,
                    startT + attemptS * Range(rng, 0.4f, 0.9f)
                );
                if (revives < 2 && rng.NextDouble() < 0.55) {
                    t += 8f;
                    AddFightEvent(AnalyticsMetric.BossRevive, t);
                    t += 14f;
                    plan.Track.Add(new Moment(t, height, stamina));
                    revives++;
                } else {
                    return false;
                }
            }
        }

        private void AddFightEvent(AnalyticsMetric metric, float at) {
            plan.Get(metric).Add(new Moment(at, BOSS_TRIGGER_U, stamina));
        }

        // the monkey hangs just under the king's reach, bananas keep it fed
        private void AddFightTrack(float from, float to) {
            float at = from;
            while (at + SAMPLE_INTERVAL_S < to) {
                at += SAMPLE_INTERVAL_S;
                height = BOSS_TRIGGER_U + Range(rng, -12f, 3f);
                stamina = Mathf.Clamp(
                    stamina + Range(rng, -14f, 12f),
                    20f,
                    MAX_STAMINA
                );
                plan.Track.Add(new Moment(at, height, stamina));
                plan.HighestHeight = Mathf.Max(plan.HighestHeight, height);
            }
            t = to;
            plan.Track.Add(new Moment(t, height, stamina));
        }
    }

    // private methods  ########################################################
    // ends the run at a fraction of its length, as if the game was quit
    private static void CutShort(RunPlan plan, float fraction) {
        float cutoff = plan.DurationS * fraction;
        plan.IsLost = false;
        plan.DurationS = cutoff;
        foreach (List<Moment> list in plan.Events.Values) {
            list.RemoveAll(m => m.T > cutoff);
        }
        plan.Track.RemoveAll(m => m.T > cutoff);
        if (plan.HasZoneExit && plan.ZoneExit.T > cutoff) {
            plan.HasZoneExit = false;
        }
        Moment last = plan.Track[plan.Track.Count - 1];
        plan.EndHeight = last.Height;
        plan.HighestHeight = 0f;
        foreach (Moment m in plan.Track) {
            plan.HighestHeight = Mathf.Max(plan.HighestHeight, m.Height);
        }
    }

    // places a simulated run on the clock & fills the data a real run holds
    private static RunData BuildRun(
        RunPlan plan,
        DateTime start,
        float windowS,
        System.Random rng
    ) {
        AddPauses(plan, rng);
        RunData run = new RunData {
            Status = plan.IsLost ? RunStatus.Lost : RunStatus.Interrupted,
            Mode = plan.Mode,
            MaxStamina = MAX_STAMINA,
            StartClock = start,
            StartedInZone = true,
            DurationS = plan.DurationS,
            HighestHeight = plan.HighestHeight
        };
        run.End = new EventStamp(
            start.AddSeconds(plan.DurationS),
            plan.EndHeight
        );
        if (plan.HasZoneExit) {
            run.HasZoneExit = true;
            run.ZoneExit = ToStamp(plan.ZoneExit, start, plan.DurationS);
        }

        foreach (KeyValuePair<AnalyticsMetric, List<Moment>> pair
            in plan.Events) {
            List<Moment> sorted = new List<Moment>(pair.Value);
            sorted.Sort((a, b) => a.T.CompareTo(b.T));
            foreach (Moment m in sorted) {
                run.GetEvents(pair.Key)
                    .Add(ToStamp(m, start, plan.DurationS));
            }
        }

        AddSamples(run, plan);

        if (plan.IsLost) {
            // the cause comes from the same rule as a real run
            run.Cause = DeathCauseResolver.Resolve(
                run,
                run.End.Clock,
                windowS
            );
        }

        int bananas = run.CountOf(AnalyticsMetric.Banana);
        int coconuts = run.CountOf(AnalyticsMetric.Coconut);
        run.HeightScore =
            (long)Mathf.Floor(plan.HighestHeight / UNITS_PER_METER)
            * POINTS_PER_METER;
        run.CoconutRewardScore = (long)coconuts * COCONUT_POINTS;
        run.RewardScore =
            (long)bananas * REWARD_POINTS + run.CoconutRewardScore;
        run.TotalScore = run.HeightScore + run.RewardScore;

        return run;
    }

    // the odd pause of the game, about one in 4 min of play
    private static void AddPauses(RunPlan plan, System.Random rng) {
        for (float from = 0f; from + 30f < plan.DurationS; from += 30f) {
            if (rng.NextDouble() >= 0.12) {
                continue;
            }
            float at = from + Range(rng, 0f, 30f);
            Moment atTrack = plan.Track[0];
            foreach (Moment m in plan.Track) {
                atTrack = m;
                if (m.T >= at) {
                    break;
                }
            }
            plan.Get(AnalyticsMetric.Pause)
                .Add(new Moment(at, atTrack.Height, atTrack.Stamina));
        }
    }

    // a sample at the start, then every interval & at the end, as the real
    // sampler does; a banana refill shows at the next tick
    private static void AddSamples(RunData run, RunPlan plan) {
        float nextT = 0f;
        foreach (Moment m in plan.Track) {
            if (m.T >= nextT) {
                run.Samples.Add(new RunSample(m.T, m.Height, m.Stamina));
                nextT = m.T + SAMPLE_INTERVAL_S;
            }
        }

        Moment last = plan.Track[plan.Track.Count - 1];
        if (run.Samples[run.Samples.Count - 1].Elapsed < last.T) {
            run.Samples.Add(new RunSample(last.T, last.Height, last.Stamina));
        }
    }

    // an event never lands after the run's end
    private static EventStamp ToStamp(Moment m, DateTime start, float endS) {
        return new EventStamp(
            start.AddSeconds(Mathf.Min(m.T, endS)),
            m.Height
        );
    }

    // piecewise linear read of a flat x, y curve, held past its ends
    private static float Curve(float[] keys, float x) {
        if (x <= keys[0]) {
            return keys[1];
        }
        for (int i = 2; i < keys.Length; i += 2) {
            if (x <= keys[i]) {
                float u = (x - keys[i - 2]) / (keys[i] - keys[i - 2]);

                return Mathf.Lerp(keys[i - 1], keys[i + 1], u);
            }
        }

        return keys[keys.Length - 1];
    }

    private static float Range(System.Random rng, float min, float max) {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
