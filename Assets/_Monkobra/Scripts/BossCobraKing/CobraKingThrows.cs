using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>Thrown bananas in flight, their trail and sparks, the jewel hit,
/// and the first throw hint.</summary>
public sealed class CobraKingThrows {
    // private members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingConfig config;

    public CobraKingThrows(CobraKingRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    // Public Methods  #########################################################
    // thrown bananas follow an arc onto the crown, then burst on impact
    public void UpdateProjectiles() {
        Vector3 target = rig.body.WeakSpotPosition;
        for (int i = rig.projectiles.Count - 1; i >= 0; i--) {
            CobraKingProjectile thrown = rig.projectiles[i];
            if (thrown.transform == null) {
                rig.projectiles.RemoveAt(i);
                continue;
            }
            thrown.ageS += Time.deltaTime;
            float t = Mathf.Clamp01(thrown.ageS / config.ThrowTimeS);
            Vector3 position = Vector3.Lerp(thrown.start, target, t)
                + Vector3.up * Mathf.Sin(t * Mathf.PI) * config.ThrowArcU;
            thrown.transform.position = position;
            thrown.transform.Rotate(Vector3.forward, 900f * Time.deltaTime);

            // a dot every few hundredths of a second marks the path
            if (thrown.ageS >= thrown.nextTrailS) {
                thrown.nextTrailS = thrown.ageS + 0.035f;
                AddTrailDot(position);
            }
            rig.projectiles[i] = thrown;
            if (t < 1f) {
                continue;
            }
            UnityEngine.Object.Destroy(thrown.transform.gameObject);
            rig.projectiles.RemoveAt(i);
            BurstAt(target, config.TrailColor);
            OnJewelHit();
        }
        UpdateSparks();
    }

    public void AddTrailDot(Vector3 position) {
        Transform dot = rig.Feedback.CreatePrimitive(
            PrimitiveType.Sphere, "TrailDot", Vector3.one * 0.22f,
                config.TrailColor
        ).transform;
        dot.position = position;
        rig.sparks.Add(new CobraKingSpark {
            transform = dot,
            velocity = Vector3.zero,
            size = 0.22f,
            ageS = 0f,
            lifeS = 0.35f
        });
    }

    // cubes fly out, eg where a banana lands or the reward armor settles
    public void BurstAt(Vector3 position, Color color) {
        for (int i = 0; i < 10; i++) {
            Transform cube = rig.Feedback.CreatePrimitive(
                PrimitiveType.Cube, "Burst", Vector3.one * 0.25f, color
            ).transform;
            cube.position = position;
            cube.rotation = Random.rotation;
            rig.sparks.Add(new CobraKingSpark {
                transform = cube,
                velocity = Random.insideUnitSphere * 7f + Vector3.up * 2f,
                size = 0.25f,
                ageS = 0f,
                lifeS = 0.6f
            });
        }
    }

    public void UpdateSparks() {
        for (int i = rig.sparks.Count - 1; i >= 0; i--) {
            CobraKingSpark spark = rig.sparks[i];
            spark.ageS += Time.deltaTime;
            if (spark.transform == null || spark.ageS >= spark.lifeS) {
                if (spark.transform != null) {
                    UnityEngine.Object.Destroy(spark.transform.gameObject);
                }
                rig.sparks.RemoveAt(i);
                continue;
            }
            spark.velocity += Physics.gravity * 0.5f * Time.deltaTime;
            spark.transform.position += spark.velocity * Time.deltaTime;
            spark.transform.localScale = Vector3.one * spark.size
                * (1f - spark.ageS / spark.lifeS);
            rig.sparks[i] = spark;
        }
    }

    // a banana on the crown always hurts
    public void OnJewelHit() {
        if (rig.phase != CobraKingPhase.Fighting) {
            return;
        }

        int lastThird = rig.Third;
        int damage = rig.worldBalanceConfig.BossBananaDamage;
        rig.health = Mathf.Max(0, rig.health - damage);
        rig.body.FlashHit();
        rig.onJewelHit?.Invoke();
        rig.hud.PopDamage(damage, rig.health, rig.maxHealth,
            rig.body.WeakSpotPosition);

        if (rig.health <= 0) {
            rig.Ending.StartFinisher(true);
            return;
        }
        if (!rig.hasHit) {
            rig.hasHit = true;
            rig.Feedback.Say(config.FirstHitLine);
            rig.nextGoldenTime = Time.time + config.GoldenFirstDelayS;
            rig.Attacks.StartRest();
        } else if (rig.Third > lastThird) {
            // each lost third: more bananas to throw & more webs
            rig.Arena.AddBananas(config.BananasPerThird);
            rig.Arena.SpawnWebs(config.WebsPerThird);
        }
    }

    // Hint, Fruits, Camera  ---------------------------------------------------
    public void UpdateFirstThrowHint() {
        // until a mode is picked: the key prompt between the two choices,
        // the dotted line from there to the crown
        bool isShown;
        Vector3 from;
        if (rig.isChoosing && rig.choices.Count == 2) {
            isShown = rig.choices[0].root != null
                && rig.choices[1].root != null;
            from = isShown
                ? (rig.choices[0].root.transform.position
                    + rig.choices[1].root.transform.position) * 0.5f
                : Vector3.zero;
        } else {
            CobraKingArenaFruit first = rig.fruits.Count > 0 ? rig.fruits[0]
                : default;
            isShown = !rig.hasHit && first.root != null
                && first.root.activeSelf;
            from = first.root != null
                ? first.root.transform.position
                : Vector3.zero;
        }
        // a seen or dev-skipped lesson shows no prompt and no dotted line
        if (TutorialSkipManager.I != null
            && TutorialSkipManager.I.ShouldSkip(TutorialItem.CobraKingLesson)) {
            isShown = false;
        }
        rig.hud.SetPrompt(isShown, from);
        if (rig.dots == null) {
            return;
        }

        Vector3 to = rig.body.WeakSpotPosition;
        Vector3 outward = from - rig.trunkCenter;
        outward.y = 0f;
        Vector3 control = (from + to) * 0.5f + outward.normalized * 3f;
        float wave = Time.time * 1.5f;
        for (int i = 0; i < rig.dots.Length; i++) {
            rig.dots[i].gameObject.SetActive(isShown);
            if (!isShown) {
                continue;
            }
            float t = (i + 1f) / (rig.dots.Length + 1f);
            float u = 1f - t;
            rig.dots[i].position =
                u * u * from + 2f * u * t * control + t * t * to;
            // a pulse runs along the line toward the king
            float pulse = Mathf.Repeat(t - wave, 1f);
            rig.dots[i].localScale = Vector3.one * config.DotRadiusU * 2f
                * Mathf.Lerp(1.6f, 0.8f, pulse);
        }
    }

}
