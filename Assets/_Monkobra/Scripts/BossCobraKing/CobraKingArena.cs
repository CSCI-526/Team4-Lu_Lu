using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static CobraKingRig;

/// <summary>Builds and clears the arena: branches, fruit, webs, rough bark, and
/// the trunk markers.</summary>
public sealed class CobraKingArena {
    // Private Members  ########################################################
    private readonly CobraKingRig rig;
    private readonly CobraKingTuning tuning;

    public CobraKingArena(CobraKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // the boss zone grows no tree branches, but the fight was tuned w/ the
    // climb's own around it: the same count, spread & fruit, from the guard
    // up to the nest, grown here once & kept through a restart
    public void SpawnArenaBranches() {
        if (tuning.BranchEmptyPrefab == null
            || tuning.BananaBranchPrefab == null) {
            return;
        }
        float bottomY = rig.entryY - tuning.GuardDepthU;
        float topY =
            rig.coilBottomY - rig.body.HangDepthU - tuning.NestClearanceU;
        List<Vector3> placed = new List<Vector3>();
        foreach (CobraKingArenaFruit fruit in rig.fruits) {
            if (fruit.branch != null) {
                placed.Add(fruit.branch.transform.position);
            }
        }
        for (float sliceY = bottomY; sliceY < topY; sliceY += 10f) {
            float sliceTopY = Mathf.Min(topY, sliceY + 10f);
            int attempts = Mathf.RoundToInt(
                tuning.ArenaBranchAttemptsPer10U * (sliceTopY - sliceY) / 10f
            );
            for (int i = 0; i < attempts; i++) {
                if (Random.value >= tuning.ArenaBranchChance) {
                    continue;
                }
                TryAddArenaBranch(sliceY, sliceTopY, placed);
            }
        }
    }

    public void TryAddArenaBranch(
        float minY, float maxY, List<Vector3> placed
    ) {
        for (int attempt = 0; attempt < 30; attempt++) {
            float angle = Random.Range(0f, 360f);
            Vector3 position = rig.PointOnTrunk(
                angle, Random.Range(minY, maxY), tuning.BranchRadiusU
            );
            if (Vector3.Distance(position,
                rig.entryPosition) < tuning.ArenaBranchStartClearU) {
                continue;
            }
            bool isClear = true;
            foreach (Vector3 other in placed) {
                if (Vector3.Distance(position,
                    other) < tuning.ArenaBranchSeparationU) {
                    isClear = false;
                    break;
                }
            }
            if (!isClear) {
                continue;
            }
            bool hasFruit = Random.value < tuning.ArenaBranchFruitChance;
            GameObject branch = UnityEngine.Object.Instantiate(
                hasFruit ? tuning.BananaBranchPrefab
                    : tuning.BranchEmptyPrefab, rig.host
            );
            branch.transform.SetPositionAndRotation(
                position, Quaternion.Euler(0f, 90f - angle, 0f)
            );
            BranchController controller =
                branch.GetComponent<BranchController>();
            if (controller != null) {
                controller.Setup();
            }
            if (hasFruit) {
                ScorePickup pickup =
                    branch.GetComponentInChildren<ScorePickup>(true);
                if (pickup != null) {
                    ApplyWatermelon(pickup.gameObject);
                    rig.arenaBranchFruits.Add(pickup.gameObject);
                }
            }
            placed.Add(position);
            rig.arenaBranchSpots.Add(rig.PointOnTrunk(angle, position.y,
                tuning.TrunkRadiusU));
            return;
        }
    }

    // the arena, guard cobra down to the king's nest, belongs inside the boss
    // zone, where nothing else is generated
    public void WarnIfArenaLeavesZone() {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        // the endless run's rush keeps its own bare range
        if (ramp == null || rig.IsRush) {
            return;
        }
        float zoneStartY = ramp.PlayerGameStartYPosition
            + tuning.WorldBalanceConfig.BossZoneStartU;
        if (rig.entryY - tuning.GuardDepthU < zoneStartY) {
            Debug.LogWarning(
                "CobraKingFightController:\tthe guard cobra hangs below the "
                    + "boss "
                + "zone start, ordinary hazards may reach the arena; move "
                + "the trigger up or shorten guardDepthU",
                rig.owner
            );
        }
    }

    // Endless Boss Rush  -------------------------------------------------------
    // the world y range a boss of the endless run's rush keeps bare, from
    // under its guard to over its coil
    public Vector2 RushRange(float entryY) {
        return new Vector2(entryY - tuning.GuardDepthU - 4f,
            entryY + tuning.NestHeightU + 10f);
    }

    // the coming fight's range is kept bare as the tree grows toward it
    public void ReserveRushRange() {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp == null || !rig.IsRush) {
            return;
        }
        Vector2 range = RushRange(
            EndlessBossRush.TriggerWorldY(rig.rushEncounter));
        ramp.SetExtraBossZone(rig.owner, range.x, range.y);
    }

    public void ReleaseRushRange() {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp != null) {
            ramp.ClearExtraBossZone(rig.owner);
        }
    }

    // what the endless tree grew in the arena b4 it was reserved goes:
    // branches w/ their fruit, webs, falling cobras, rough bark
    public void ClearRushArena() {
        Vector2 range = RushRange(rig.entryY);
        ReserveRushRangeAt(range);
        foreach (BranchController branch in
            UnityEngine.Object.FindObjectsByType<BranchController>(
                FindObjectsSortMode.None)) {
            RemoveIfIn(branch.gameObject, range);
        }
        foreach (SpiderWebTrap web in
            UnityEngine.Object.FindObjectsByType<SpiderWebTrap>(
                FindObjectsSortMode.None)) {
            RemoveIfIn(web.gameObject, range);
        }
        foreach (FallingCobraController cobra in
            UnityEngine.Object.FindObjectsByType<FallingCobraController>(
                FindObjectsSortMode.None)) {
            RemoveIfIn(cobra.gameObject, range);
        }
        foreach (RoughBarkZone bark in
            UnityEngine.Object.FindObjectsByType<RoughBarkZone>(
                FindObjectsSortMode.None)) {
            RemoveIfIn(bark.gameObject, range);
        }
    }

    private void ReserveRushRangeAt(Vector2 range) {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp != null) {
            ramp.SetExtraBossZone(rig.owner, range.x, range.y);
        }
    }

    private static void RemoveIfIn(GameObject thing, Vector2 range) {
        float y = thing.transform.position.y;
        if (y < range.x || y > range.y) {
            return;
        }
        ForgetFruits(thing);
        // inactive first, Destroy only lands at the end of the frame
        thing.SetActive(false);
        UnityEngine.Object.Destroy(thing);
    }

    // a segment recycled over the top during the fight rolls new branches
    public void OnBranchesGenerated(TreeSegmentController segment) {
        BranchController[] branches =
            segment.GetComponentsInChildren<BranchController>();
        ClearBranchesNearNest(branches);
    }

    public void ClearBranchesNearNest(BranchController[] branches) {
        float minY =
            rig.coilBottomY - rig.body.HangDepthU - tuning.NestClearanceU;
        // the endless run's rush: only up to its arena's top, the climb
        // above keeps its branches
        float maxY = rig.IsRush ? RushRange(rig.entryY).y
            : float.PositiveInfinity;
        foreach (BranchController branch in branches) {
            float branchY = branch.transform.position.y;
            if (branchY > minY && branchY <= maxY) {
                ForgetFruits(branch.gameObject);
                branch.gameObject.SetActive(false);
            }
        }
        // a falling cobra never outlives its branch: one perched up here would
        // hang in mid air, whichever listener ran first
        foreach (FallingCobraController cobra in
            UnityEngine.Object.FindObjectsByType<FallingCobraController>(
                FindObjectsSortMode.None)) {
            float cobraY = cobra.transform.position.y;
            if (cobraY > minY && cobraY <= maxY) {
                // inactive first, Destroy only lands at the end of the frame
                cobra.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(cobra.gameObject);
            }
        }
    }

    // two banana branches beside the monkey, plain & golden: the first throw
    // picks easy or normal mode
    public void SpawnChoices() {
        float angle = rig.AngleOf(rig.entryPosition);
        float y = rig.entryPosition.y + tuning.ChoiceAboveU;
        AddFruit(angle - tuning.ChoiceSideDeg, y, false);
        AddFruit(angle + tuning.ChoiceSideDeg, y, true);
        rig.choices.Add(rig.fruits[rig.fruits.Count - 2]);
        rig.choices.Add(rig.fruits[rig.fruits.Count - 1]);
        rig.fruits.RemoveRange(rig.fruits.Count - 2, 2);
        rig.isChoosing = true;
    }

    public void ChooseMode(bool isEasyPick) {
        rig.isChoosing = false;
        rig.isEasy = isEasyPick;
        rig.maxHealth = rig.isEasy
            ? Mathf.Max(
                1,
                Mathf.RoundToInt(rig.BaseMaxHealth * tuning.EasyHealthScale)
            )
            : rig.BaseMaxHealth;
        rig.health = rig.maxHealth;
        rig.hud.SetHealth(1f);
        foreach (CobraKingArenaFruit choice in rig.choices) {
            RemoveBranch(choice.branch);
        }
        rig.choices.Clear();
        rig.hud.SetChoiceTags(false, Vector3.zero, Vector3.zero);
        SpawnFruits();
        rig.hud.PopText(
            rig.isEasy ? "Easy mode" : "Normal mode",
            rig.monkey.position,
            rig.isEasy ? new Color(0.55f, 0.95f, 0.55f) : tuning.GoldenColor
        );
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CobraKingFightController:\t" + (rig.isEasy ? "easy" : "normal")
                    + " mode, king health " + rig.maxHealth,
                rig.owner
            );
        }
    }

    // the monkey's fruit sensor never sees a trigger exit for a fruit that
    // is switched off or destroyed, & a destroyed one would break its reach
    // & grab every physics step, so it is told to forget it first
    public static void ForgetFruits(GameObject branch) {
        UpwardFruitSensor sensor = UpwardFruitSensor.I;
        if (sensor == null || branch == null) {
            return;
        }
        foreach (Collider part in
            branch.GetComponentsInChildren<Collider>(true)) {
            sensor.RemoveFruit(part);
        }
    }

    public static void RemoveBranch(GameObject branch) {
        if (branch == null) {
            return;
        }
        ForgetFruits(branch);
        UnityEngine.Object.Destroy(branch);
    }

    // every arena banana branch goes, eg for a restart
    public void ClearFruits() {
        foreach (CobraKingArenaFruit fruit in rig.fruits) {
            RemoveBranch(fruit.branch);
        }
        rig.fruits.Clear();
        foreach (CobraKingArenaFruit choice in rig.choices) {
            RemoveBranch(choice.branch);
        }
        rig.choices.Clear();
        rig.goldenIndex = -1;
        rig.isChoosing = false;
        rig.hud.SetChoiceTags(false, Vector3.zero, Vector3.zero);
    }

    public void SpawnFruits() {
        float startAngle = rig.AngleOf(rig.monkey.position);
        for (int i = 0; i < tuning.BananaCount; i++) {
            // spread round the trunk, the first one right above the monkey
            float t = tuning.BananaCount > 1 ? i / (tuning.BananaCount - 1f)
                : 0f;
            AddFruit(
                startAngle + i * 360f / tuning.BananaCount,
                rig.entryY + Mathf.Lerp(
                    tuning.FruitHeightRangeU.x, tuning.FruitHeightRangeU.y, t
                ),
                false
            );
        }
        // the golden banana starts hidden & shows up once the fight is on
        rig.goldenIndex = AddFruit(startAngle, rig.entryY, true);
        rig.Armor.HideGolden();
    }

    // extra banana branches at random spots round the trunk
    public void AddBananas(int count) {
        for (int i = 0; i < count; i++) {
            AddFruit(
                Random.Range(0f, 360f),
                rig.entryY
                    + Random.Range(tuning.FruitHeightRangeU.x,
                        tuning.FruitHeightRangeU.y),
                false
            );
        }
    }

    public bool IsNearSpot(float angle, float y, Vector3 spot) {
        return Mathf.Abs(Mathf.DeltaAngle(angle, rig.AngleOf(spot))) < 30f
            && Mathf.Abs(y - spot.y) < 3f;
    }

    // webs on the trunk in the arena, never on a fruit's branch line
    public void SpawnWebs(int count) {
        if (tuning.WebPrefab == null) {
            return;
        }
        float topY = rig.body.LowestY - tuning.CeilingGapU - 1.5f;
        for (int i = 0; i < count; i++) {
            TryAddWeb(rig.entryY - tuning.WebBelowEntryU,
                Mathf.Max(rig.entryY + 1f, topY));
        }
    }

    // the climb's own webs below the arena floor, as it had there b4 the
    // boss zone: a few rolls per 10 u from the guard up to just under the
    // entry
    public void SpawnLowWebs() {
        if (tuning.WebPrefab == null) {
            return;
        }
        float bottomY = rig.entryY - tuning.GuardDepthU;
        float topY = rig.entryY - 3f;
        for (float sliceY = bottomY; sliceY < topY; sliceY += 10f) {
            float sliceTopY = Mathf.Min(topY, sliceY + 10f);
            int attempts = Mathf.RoundToInt(
                tuning.LowWebAttemptsPer10U * (sliceTopY - sliceY) / 10f
            );
            for (int i = 0; i < attempts; i++) {
                if (Random.value < tuning.LowWebChance) {
                    TryAddWeb(sliceY, sliceTopY);
                }
            }
        }
    }

    // a web at a free spot b/t minY & maxY: off the fruit's branch lines, the
    // monkey & its start, & never overlapping another web or a branch
    public void TryAddWeb(float minY, float maxY) {
        for (int attempt = 0; attempt < 16; attempt++) {
            float angle = Random.Range(0f, 360f);
            if (IsNearFruit(angle)) {
                continue;
            }
            float y = Random.Range(minY, maxY);
            Vector3 spot =
                rig.PointOnTrunk(angle, y, tuning.TrunkRadiusU + 0.02f);
            if (IsWebCrowded(spot)) {
                continue;
            }
            // never right on the monkey, nor on its start spot, where
            // a restart brings it back
            bool isOnMonkey = IsNearSpot(angle, y, rig.monkey.position)
                || IsNearSpot(angle, y, rig.entryPosition);
            if (isOnMonkey) {
                continue;
            }
            // as the segments' spawner: the web's face looks away from
            // the trunk
            SpiderWebTrap web = UnityEngine.Object.Instantiate(
                tuning.WebPrefab,
                rig.PointOnTrunk(angle, y, tuning.TrunkRadiusU + 0.02f),
                Quaternion.Euler(0f, -angle, 0f),
                rig.host
            );
            rig.webs.Add(web);
            return;
        }
    }

    public bool IsWebCrowded(Vector3 spot) {
        foreach (SpiderWebTrap other in rig.webs) {
            if (other != null
                && Vector3.Distance(other.transform.position, spot)
                    < tuning.WebSeparationU) {
                return true;
            }
        }
        foreach (Vector3 branch in rig.arenaBranchSpots) {
            if (Vector3.Distance(branch, spot) < tuning.WebBranchGapU) {
                return true;
            }
        }
        return false;
    }

    // the teammates' rough bark, a few patches in the arena that slow the
    // monkey while it touches them; kept off its start spot & the banana
    // branches, & kept through a restart
    public void SpawnRoughBark() {
        if (tuning.RoughBarkMaterial == null
            || tuning.RoughBarkAttemptsPer10U <= 0) {
            return;
        }
        // as the climb's spawner: per 10 u a few rolls, each patch kept
        // clear of the others' heights, of branches & webs, & here of the
        // monkey's start spot
        float bottomY = rig.entryY - tuning.GuardDepthU;
        float topY =
            rig.coilBottomY - rig.body.HangDepthU - tuning.NestClearanceU;
        List<Vector3> patches = new List<Vector3>();
        // x angle, y y, z extent
        Physics.SyncTransforms();
        for (float sliceY = bottomY; sliceY < topY; sliceY += 10f) {
            float sliceTopY = Mathf.Min(topY, sliceY + 10f);
            for (int i = 0; i < tuning.RoughBarkAttemptsPer10U; i++) {
                if (Random.value >= tuning.RoughBarkChance) {
                    continue;
                }
                TryAddRoughBark(sliceY, sliceTopY, patches);
            }
        }
        // the rolls can come up empty where the monkey fights; one patch
        // there is sure
        bool hasPlayPatch = false;
        foreach (Vector3 patch in patches) {
            if (patch.y + patch.z > rig.entryY - 2f
                && patch.y - patch.z < topY) {
                hasPlayPatch = true;
            }
        }
        if (!hasPlayPatch) {
            TryAddRoughBark(rig.entryY - 2f, topY, patches);
        }
    }

    public void TryAddRoughBark(
        float minY, float maxY, List<Vector3> patches
    ) {
        for (int attempt = 0; attempt < 16; attempt++) {
            float height = Random.Range(
                tuning.RoughBarkHeightRangeU.x, tuning.RoughBarkHeightRangeU.y
            );
            float arc = Random.Range(
                tuning.RoughBarkArcRangeDeg.x, tuning.RoughBarkArcRangeDeg.y
            );
            float extent = RoughBarkZone.GetVerticalExtent(height);
            float y = Random.Range(minY, maxY);
            float angle = Random.Range(0f, 360f);
            bool isClear = true;
            foreach (Vector3 other in patches) {
                if (Mathf.Abs(other.y - y) < extent + other.z + 1f) {
                    isClear = false;
                    break;
                }
            }
            bool isOnStart = Mathf.Abs(
                Mathf.DeltaAngle(angle, rig.AngleOf(rig.entryPosition))
            ) < arc * 0.5f + 15f && Mathf.Abs(y - rig.entryY) < extent + 1f;
            if (!isClear || isOnStart || IsArcBlocked(angle, y, extent, arc)) {
                continue;
            }
            AddRoughBark(angle, y, height, arc);
            patches.Add(new Vector3(angle, y, extent));
            return;
        }
    }

    public bool IsFruitBranch(Transform part) {
        foreach (CobraKingArenaFruit fruit in rig.fruits) {
            if (fruit.branch != null
                && part.IsChildOf(fruit.branch.transform)) {
                return true;
            }
        }
        return false;
    }

    // the climb's check: no fruit or web on the patch's arc
    public bool IsArcBlocked(float angle, float y, float extent, float arc) {
        float radius = tuning.TrunkRadiusU + 0.05f;
        int steps = Mathf.Max(
            1, Mathf.CeilToInt(radius * arc * Mathf.Deg2Rad / 0.7f)
        );
        for (int i = 0; i <= steps; i++) {
            float sample = angle + (i / (float)steps - 0.5f) * arc;
            Vector3 point = rig.PointOnTrunk(sample, y, radius);
            foreach (Collider hit in Physics.OverlapCapsule(
                point - Vector3.up * extent, point + Vector3.up * extent, 0.7f,
                Physics.AllLayers, QueryTriggerInteraction.Collide
            )) {
                // an obstacle branch may stick out over a patch; only a
                // fruit's branch & a web keep it off
                if (hit.GetComponentInParent<SpiderWebTrap>() != null
                    || IsFruitBranch(hit.transform)) {
                    return true;
                }
            }
        }
        return false;
    }

    // built on the tree segment at its height, so its mesh hugs the
    // faceted trunk, then kept under the fight so no recycle moves it
    public void AddRoughBark(float angle, float y, float height, float arc) {
        GameObject patch = new GameObject("BossRoughBarkPatch");
        Transform segment = NearestSegment(y);
        patch.transform.SetParent(segment != null ? segment : rig.host, false);
        patch.transform.position =
            new Vector3(rig.trunkCenter.x, y, rig.trunkCenter.z);
        RoughBarkZone zone = patch.AddComponent<RoughBarkZone>();
        // the bark measures angles from +z toward +x, this fight from +x
        zone.Initialize(
            tuning.TrunkRadiusU + 0.05f, height, 90f - angle, arc,
            tuning.RoughBarkMaterial, true, Random.Range(0, 100000)
        );
        patch.transform.SetParent(rig.host, true);
    }

    public static Transform NearestSegment(float y) {
        Transform nearest = null;
        float best = float.MaxValue;
        foreach (TreeSegmentController segment in
            UnityEngine.Object.FindObjectsByType<TreeSegmentController>(
                FindObjectsSortMode.None)) {
            float distance = Mathf.Abs(segment.transform.position.y - y);
            if (distance < best) {
                best = distance;
                nearest = segment.transform;
            }
        }
        return nearest;
    }

    public bool IsNearFruit(float angle) {
        foreach (CobraKingArenaFruit fruit in rig.fruits) {
            if (fruit.isGolden) {
                continue;
            }
            float fruitAngle = rig.AngleOf(fruit.root.transform.position);
            if (Mathf.Abs(Mathf.DeltaAngle(angle,
                fruitAngle)) < tuning.WebFruitGapDeg) {
                return true;
            }
        }
        return false;
    }

    /// <returns>the new fruit's index in the fruit list</returns>
    // a real banana branch, as the tree segments grow, always bearing; the
    // golden one is the same banana tinted gold w/ a thick gold outline
    public int AddFruit(float angle, float y, bool isGolden) {
        GameObject branch =
            UnityEngine.Object.Instantiate(tuning.BananaBranchPrefab, rig.host);
        PlaceBranch(branch.transform, angle, y);
        BranchController controller = branch.GetComponent<BranchController>();
        if (controller != null) {
            controller.Setup();
            // the banana branch prefab bears a banana
        }
        ScorePickup pickup = branch.GetComponentInChildren<ScorePickup>(true);
        GameObject banana = pickup != null ? pickup.gameObject : branch;
        ApplyWatermelon(banana);
        if (isGolden) {
            rig.Armor.MakeGolden(banana);
        }

        rig.fruits.Add(new CobraKingArenaFruit {
            root = banana,
            branch = branch,
            trigger = banana.GetComponent<Collider>(),
            isGolden = isGolden,
            respawnTime = -1f
        });
        return rig.fruits.Count - 1;
    }

    // the branch root sits on the segments' branch ring, its +z away from the
    // trunk, high enough that the banana hangs about the given height
    public void PlaceBranch(Transform branch, float angle, float fruitY) {
        branch.SetPositionAndRotation(
            rig.PointOnTrunk(angle, fruitY + BANANA_HANG_U,
                tuning.BranchRadiusU),
            Quaternion.Euler(0f, 90f - angle, 0f)
        );
    }

    // the arena's fruit looks like a watermelon: the banana model's mesh is
    // swapped, its collider & scripts stay, so grab & throw work as before
    public void ApplyWatermelon(GameObject fruit) {
        if (tuning.WatermelonMesh == null || fruit == null) {
            return;
        }
        foreach (MeshFilter filter in
            fruit.GetComponentsInChildren<MeshFilter>(true)) {
            filter.sharedMesh = tuning.WatermelonMesh;
            filter.transform.localRotation = Quaternion.identity;
            filter.transform.localScale = tuning.WatermelonScale;
            filter.transform.localPosition = tuning.WatermelonOffset;
        }
    }

    // EASY & NORMAL tags over the two choice bananas
    public void UpdateChoiceTags() {
        if (!rig.isChoosing || rig.choices.Count < 2) {
            return;
        }
        rig.hud.SetChoiceTags(
            true,
            rig.choices[0].root.transform.position,
            rig.choices[1].root.transform.position
        );
    }

    // a taken banana grows back on its branch
    // the arena's watermelons belong to the fight: once it is won they go,
    // their branches left bare, so none is eaten like a banana after
    public void RemoveArenaFruit() {
        rig.isArenaFruitGone = true;
        foreach (CobraKingArenaFruit fruit in rig.fruits) {
            if (fruit.root != null) {
                ForgetFruits(fruit.root);
                fruit.root.SetActive(false);
            }
        }
        foreach (GameObject melon in rig.arenaBranchFruits) {
            if (melon != null) {
                ForgetFruits(melon);
                melon.SetActive(false);
            }
        }
    }

    public void UpdateFruits() {
        if (rig.isArenaFruitGone) {
            return;
        }
        for (int i = 0; i < rig.fruits.Count; i++) {
            CobraKingArenaFruit fruit = rig.fruits[i];
            if (fruit.isGolden || fruit.root.activeSelf
                || fruit.respawnTime < 0f || Time.time < fruit.respawnTime) {
                continue;
            }
            fruit.root.SetActive(true);
            fruit.respawnTime = -1f;
            rig.fruits[i] = fruit;
        }
    }

    // Markers & Helpers  ------------------------------------------------------
    // a danger zone flat on the trunk, facing out: a dark red rim whose
    // bright red center fills it as the hit nears, w/ a hud sign if worded
    public void AddTrunkZone(
        float angle,
        float y,
        float size,
        string word,
        bool hasRimSign
    ) {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 outward =
            new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        Quaternion facing = Quaternion.FromToRotation(Vector3.up, outward);

        GameObject rim = rig.Feedback.CreatePrimitive(
            PrimitiveType.Cylinder, "ZoneRim",
            new Vector3(size, 0.02f, size), tuning.WarningDimColor
        );
        rim.transform.position =
            rig.PointOnTrunk(angle, y, tuning.TrunkRadiusU + 0.04f);
        rim.transform.rotation = facing;

        GameObject fill = rig.Feedback.CreatePrimitive(
            PrimitiveType.Cylinder, "ZoneFill",
            new Vector3(0.01f, 0.03f, 0.01f), tuning.WarningColor
        );
        fill.transform.position =
            rig.PointOnTrunk(angle, y, tuning.TrunkRadiusU + 0.07f);
        fill.transform.rotation = facing;

        int warning = word != null
            ? rig.hud.AddWarning(rim.transform.position, word)
            : -1;
        rig.markers.Add(new CobraKingMarker {
            rim = rim.transform,
            fill = fill.transform,
            size = size,
            warning = warning,
            isBand = false
        });
    }

    // a minion's fall line: a dark red strip down the trunk from the king's
    // coil, filling top-down in bright red as the drop nears
    public void AddTrunkStrip(
        float angle,
        float topY,
        float bottomY,
        float width,
        string word
    ) {
        float length = Mathf.Max(0.5f, topY - bottomY);
        float radians = angle * Mathf.Deg2Rad;
        Vector3 outward =
            new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        Quaternion facing = Quaternion.LookRotation(outward, Vector3.up);

        GameObject rim = rig.Feedback.CreatePrimitive(
            PrimitiveType.Cube, "StripRim",
            new Vector3(width, length, 0.04f), tuning.WarningDimColor
        );
        rim.transform.SetPositionAndRotation(
            rig.PointOnTrunk(angle, topY - length * 0.5f,
                tuning.TrunkRadiusU + 0.04f),
            facing
        );
        // the whole line is red from the start, it only beats faster
        GameObject fill = rig.Feedback.CreatePrimitive(
            PrimitiveType.Cube, "StripFill",
            new Vector3(width * 0.8f, length, 0.06f), tuning.WarningColor
        );
        fill.transform.SetPositionAndRotation(
            rig.PointOnTrunk(angle, topY - length * 0.5f,
                tuning.TrunkRadiusU + 0.07f),
            facing
        );

        int warning = word != null
            ? rig.hud.AddWarning(
                rig.PointOnTrunk(angle, rig.minionLockY + 2.5f,
                    tuning.TrunkRadiusU),
                word
            )
            : -1;
        rig.markers.Add(new CobraKingMarker {
            rim = rim.transform,
            fill = fill.transform,
            size = width,
            warning = warning,
            isBand = false,
            isStrip = true,
            topY = topY,
            length = length,
            angle = angle
        });
    }

}
