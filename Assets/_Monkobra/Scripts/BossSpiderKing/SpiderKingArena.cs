using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static SpiderKingRig;

/// <summary>Builds and clears the arena: the sky web, bare branches, melons,
/// webs, and the mode choice.</summary>
public sealed class SpiderKingArena {
    // Private Members  ########################################################
    private readonly SpiderKingRig rig;
    private readonly SpiderKingTuning tuning;

    public SpiderKingArena(SpiderKingRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    // Sky Web  ----------------------------------------------------------------
    // the arena's top, the climb's cap, shown as a see-through sheet round
    // the trunk: faint far off, clearer as the monkey nears it, rippling
    // where the monkey bumps it
    public void BuildSkyWeb() {
        ClearSkyWeb();
        rig.skyDisc =
            rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder, "AirWall",
            Vector3.one, tuning.SilkColor).transform;
        Renderer discRenderer = rig.skyDisc.GetComponent<Renderer>();
        if (tuning.AirWallMaterial != null) {
            rig.skyMaterial = new Material(tuning.AirWallMaterial);
            // a plain sheet, no web picture on it
            if (rig.skyMaterial.HasProperty(BASE_MAP)) {
                rig.skyMaterial.SetTexture(BASE_MAP, null);
            }
            discRenderer.sharedMaterial = rig.skyMaterial;
        }
        discRenderer.shadowCastingMode = ShadowCastingMode.Off;
        discRenderer.receiveShadows = false;
        discRenderer.enabled = false;
        // a clearer patch just round the monkey, so the far sheet stays thin
        rig.skyPatch =
            rig.Feedback.CreatePrimitive(PrimitiveType.Cylinder, "AirWallPatch",
            Vector3.one, tuning.SilkColor).transform;
        Renderer patchRenderer = rig.skyPatch.GetComponent<Renderer>();
        if (rig.skyMaterial != null) {
            patchRenderer.sharedMaterial = rig.skyMaterial;
        }
        patchRenderer.shadowCastingMode = ShadowCastingMode.Off;
        patchRenderer.receiveShadows = false;
        patchRenderer.enabled = false;
    }

    public void ClearSkyWeb() {
        if (rig.skyDisc != null) {
            UnityEngine.Object.Destroy(rig.skyDisc.gameObject);
        }
        rig.skyDisc = null;
        if (rig.skyPatch != null) {
            UnityEngine.Object.Destroy(rig.skyPatch.gameObject);
        }
        rig.skyPatch = null;
        if (rig.skyMaterial != null) {
            UnityEngine.Object.Destroy(rig.skyMaterial);
        }
        rig.skyMaterial = null;
    }

    public void UpdateSkyWeb() {
        if (rig.skyDisc == null || rig.monkey == null) {
            return;
        }
        float ceilingY = rig.entryY + tuning.ArenaHeightU;
        float wallY = ceilingY + 0.7f;
        float gap = ceilingY - rig.monkey.position.y;
        float near = Mathf.Clamp01(1f - gap / SKY_SHOW_U);
        near = near * near * (3f - 2f * near);
        Renderer discRenderer = rig.skyDisc.GetComponent<Renderer>();
        Renderer patchRenderer = rig.skyPatch != null
            ? rig.skyPatch.GetComponent<Renderer>() : null;
        // unseen till the monkey nears it, & never once the camera is up at
        // it, where it would veil the whole view
        Camera view = Camera.main;
        bool isUnderView = view == null
            || view.transform.position.y < wallY - 0.5f;
        bool isShown = near > 0.01f && isUnderView;
        discRenderer.enabled = isShown;
        if (patchRenderer != null) {
            patchRenderer.enabled = isShown;
        }
        // a sheet out to the horizon, as if it never ends
        rig.skyDisc.SetPositionAndRotation(
            new Vector3(rig.trunkCenter.x, wallY, rig.trunkCenter.z),
            Quaternion.identity);
        rig.skyDisc.localScale = new Vector3(SKY_WIDE_U, 0.02f, SKY_WIDE_U);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
        // the far sheet barely there, the patch round the monkey clearer
        float shimmer = 0.85f + 0.15f * pulse;
        rig.Feedback.SetColor(discRenderer,
            new Color(0.75f, 0.9f, 1f, near * 0.05f * shimmer));
        if (rig.skyPatch != null) {
            float patchU =
                (rig.HorizontalDistance(rig.monkey.position) + 3f) * 2f;
            rig.skyPatch.SetPositionAndRotation(
                new Vector3(rig.trunkCenter.x, wallY + 0.02f,
                    rig.trunkCenter.z),
                Quaternion.identity);
            rig.skyPatch.localScale = new Vector3(patchU, 0.02f, patchU);
            rig.Feedback.SetColor(patchRenderer,
                new Color(0.75f, 0.9f, 1f, near * 0.12f * shimmer));
        }
        // a bump: a ripple where the monkey's head meets it
        bool isAtCap = gap <= 0.35f;
        if (isAtCap && Time.time >= rig.nextSkyRippleTime) {
            rig.nextSkyRippleTime = Time.time + 0.45f;
            Vector3 at =
                rig.PointOnTrunk(rig.AngleOf(rig.monkey.position), wallY,
                rig.HorizontalDistance(rig.monkey.position));
            rig.Parry.AddRing(at, Color.white, 0f);
        }
    }

    // Bare Arena  -------------------------------------------------------------
    // the arena is a space of its own, cut off from the climb: the climb's
    // generators skip it (also in the test fight near the start) & what
    // they grew there b4 goes: branches w/ their fruit, webs, falling
    // cobras, rough bark
    public void MakeArenaBare() {
        // down past the guard: the whole boss area grows only the fight's
        rig.bareBottomY = rig.entryY - tuning.GuardDepthU - 4f;
        rig.bareTopY = rig.entryY + tuning.ArenaHeightU + 6f;
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp != null) {
            ramp.SetExtraBossZone(rig.owner, rig.bareBottomY, rig.bareTopY);
        }
        foreach (BranchController branch in
            UnityEngine.Object.FindObjectsByType<BranchController>(
                FindObjectsSortMode.None)) {
            if (!branch.transform.IsChildOf(rig.host)) {
                RemoveIfInArena(branch.gameObject);
            }
        }
        foreach (SpiderWebTrap web in
            UnityEngine.Object.FindObjectsByType<SpiderWebTrap>(
                FindObjectsSortMode.None)) {
            if (!web.transform.IsChildOf(rig.host)) {
                RemoveIfInArena(web.gameObject);
            }
        }
        foreach (FallingCobraController cobra in
            UnityEngine.Object.FindObjectsByType<FallingCobraController>(
                FindObjectsSortMode.None)) {
            RemoveIfInArena(cobra.gameObject);
        }
        foreach (RoughBarkZone bark in
            UnityEngine.Object.FindObjectsByType<RoughBarkZone>(
                FindObjectsSortMode.None)) {
            RemoveIfInArena(bark.gameObject);
        }
        TreeSegmentController.BranchesGenerated += OnBranchesGenerated;
    }

    // the endless run's rush: the coming fight's range is kept bare as the
    // tree grows toward it, as the arena will be
    public void ReserveRushRange() {
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp == null || !rig.IsRush) {
            return;
        }
        float entryY = EndlessBossRush.TriggerWorldY(rig.rushEncounter);
        ramp.SetExtraBossZone(rig.owner, entryY - tuning.GuardDepthU - 4f,
            entryY + tuning.ArenaHeightU + 6f);
    }

    public void RemoveIfInArena(GameObject thing) {
        float y = thing.transform.position.y;
        if (y < rig.bareBottomY || y > rig.bareTopY) {
            return;
        }
        ForgetFruits(thing);
        // inactive first, Destroy only lands at the end of the frame
        thing.SetActive(false);
        UnityEngine.Object.Destroy(thing);
    }

    // a segment recycled into the arena grows nothing there
    public void OnBranchesGenerated(TreeSegmentController segment) {
        if (!rig.IsInArena || segment == null) {
            return;
        }
        foreach (BranchController branch in
            segment.GetComponentsInChildren<BranchController>(true)) {
            RemoveIfInArena(branch.gameObject);
        }
    }

    public void EndBareArena() {
        TreeSegmentController.BranchesGenerated -= OnBranchesGenerated;
        if (rig.monkeyController != null) {
            rig.monkeyController.ArenaSpeedScale = 1f;
        }
        RampedDifficultyManager ramp = RampedDifficultyManager.I;
        if (ramp != null) {
            ramp.ClearExtraBossZone(rig.owner);
        }
    }

    // Melons & Webs  ---------------------------------------------------------
    // melon branches spread round the arena at a few heights
    public void SpawnMelons() {
        ClearMelons();
        float baseAngle = rig.AngleOf(rig.monkey.position);
        for (int i = 0; i < tuning.MelonCount; i++) {
            float angle = baseAngle + 25f + i * 360f / tuning.MelonCount
                + Random.Range(-15f, 15f);
            float y = rig.entryY + Mathf.Lerp(1.5f, tuning.ArenaHeightU - 3f,
                tuning.MelonCount > 1
                    ? (i * 2 % tuning.MelonCount) / (tuning.MelonCount - 1f)
                        : 0.5f);
            SpiderKingMelon melon = AddMelon(angle, y);
            rig.melons.Add(melon);
            // no web right on a melon
            foreach (SpiderKingLaidWeb laid in rig.stickyWebs) {
                if (laid.web != null && !IsWebHoldingMonkey(laid.web)
                    && Vector3.Distance(laid.web.transform.position,
                        melon.root.transform.position) < 2.5f) {
                    UnityEngine.Object.Destroy(laid.web.gameObject);
                }
            }
        }
        rig.stickyWebs.RemoveAll(w => w.web == null);
    }

    // the climb's banana branch, its banana swapped for a melon model; the
    // collider & scripts stay, so the grab works as on any fruit
    public SpiderKingMelon AddMelon(float angle, float y) {
        GameObject branch =
            UnityEngine.Object.Instantiate(tuning.BananaBranchPrefab, rig.host);
        branch.transform.SetPositionAndRotation(
            rig.PointOnTrunk(angle, y + BANANA_HANG_U, tuning.BranchRadiusU),
            Quaternion.Euler(0f, 90f - angle, 0f)
        );
        BranchController controller = branch.GetComponent<BranchController>();
        if (controller != null) {
            controller.Setup();
            // the banana branch prefab bears a banana
        }
        ScorePickup pickup = branch.GetComponentInChildren<ScorePickup>(true);
        GameObject fruit = pickup != null ? pickup.gameObject : branch;
        if (tuning.WatermelonMesh != null) {
            foreach (MeshFilter filter in
                fruit.GetComponentsInChildren<MeshFilter>(true)) {
                filter.sharedMesh = tuning.WatermelonMesh;
                filter.transform.localRotation = Quaternion.identity;
                filter.transform.localScale = tuning.WatermelonScale;
                filter.transform.localPosition = tuning.WatermelonOffset;
            }
        }
        Collider trigger = fruit.GetComponent<Collider>();
        if (trigger == null) {
            trigger = fruit.GetComponentInChildren<Collider>(true);
        }
        return new SpiderKingMelon {
            root = fruit,
            branch = branch,
            trigger = trigger,
            backTime = -1f,
        }
        ;
    }

    public void ClearMelons() {
        foreach (SpiderKingMelon melon in rig.melons) {
            if (melon.branch != null) {
                ForgetFruits(melon.branch);
                UnityEngine.Object.Destroy(melon.branch);
            }
        }
        rig.melons.Clear();
        if (rig.goldenMelon != null && rig.goldenMelon.branch != null) {
            ForgetFruits(rig.goldenMelon.branch);
            UnityEngine.Object.Destroy(rig.goldenMelon.branch);
        }
        rig.goldenMelon = null;
        if (rig.goldenTwin != null && rig.goldenTwin.branch != null) {
            ForgetFruits(rig.goldenTwin.branch);
            UnityEngine.Object.Destroy(rig.goldenTwin.branch);
        }
        rig.goldenTwin = null;
    }

    // the golden melon grows beside the monkey now & then while it has no
    // armor; a pulsing gold melon, gone again after goldenShowS
    public void UpdateGoldenMelon() {
        if (rig.melons.Count == 0 || rig.teach != SpiderKingTeachStep.Done) {
            // none hangs about in a lesson or the mode choice
            if (rig.goldenMelon != null && rig.goldenMelon.branch != null
                && rig.goldenMelon.branch.activeSelf) {
                HideGoldenMelon();
            }
            return;
        }
        if (rig.goldenMelon == null) {
            rig.goldenMelon = AddMelon(0f, rig.entryY);
            rig.goldenMelon.isGolden = true;
            MakeGolden(rig.goldenMelon.root);
            // two at a time, one either side, so one is always easy to reach
            rig.goldenTwin = AddMelon(0f, rig.entryY);
            rig.goldenTwin.isGolden = true;
            MakeGolden(rig.goldenTwin.root);
            HideGoldenMelon();
            // an armor lost b4 it was made keeps its "at once"
            if (SpiderKingDamage.HasArmor()) {
                rig.nextGoldenTime = Time.time + GOLDEN_FIRST_S;
            }
        }
        if (rig.goldenMelon.root == null || rig.goldenTwin == null
            || rig.goldenTwin.root == null) {
            return;
        }
        // shown is its branch out, the melon still on it
        if (rig.goldenMelon.branch.activeSelf) {
            if (Time.time >= rig.goldenHideTime) {
                // missed: the next after a short wait the first time, then
                // the usual interval
                HideGoldenMelon();
                rig.goldenSinceArmor++;
                rig.nextGoldenTime = Time.time + (rig.goldenSinceArmor == 1
                    ? GOLDEN_AFTER_ARMOR_S : tuning.GoldenIntervalS);
            }
            return;
        }
        // not while the monkey is thrown off: it shows once it lands
        if (Time.time < rig.nextGoldenTime || SpiderKingDamage.HasArmor()
            || rig.isReturningToStart) {
            return;
        }
        // beside the monkey, a bit up, in reach, as the Cobra King's
        float side = Random.value < 0.5f ? -1f : 1f;
        float angle = rig.AngleOf(rig.monkey.position) + side * 30f;
        float y = Mathf.Clamp(rig.monkey.position.y + 3f, rig.entryY + 1f,
            rig.entryY + tuning.ArenaHeightU - 2f);
        rig.goldenMelon.branch.transform.SetPositionAndRotation(
            rig.PointOnTrunk(angle, y + BANANA_HANG_U, tuning.BranchRadiusU),
            Quaternion.Euler(0f, 90f - angle, 0f)
        );
        rig.goldenMelon.branch.SetActive(true);
        rig.goldenMelon.root.SetActive(true);
        // its twin on the other side of the monkey, a little lower
        float twinAngle = rig.AngleOf(rig.monkey.position) - side * 30f;
        float twinY = Mathf.Clamp(rig.monkey.position.y + 1.5f, rig.entryY + 1f,
            rig.entryY + tuning.ArenaHeightU - 2f);
        rig.goldenTwin.branch.transform.SetPositionAndRotation(
            rig.PointOnTrunk(twinAngle, twinY + BANANA_HANG_U,
                tuning.BranchRadiusU),
            Quaternion.Euler(0f, 90f - twinAngle, 0f)
        );
        rig.goldenTwin.branch.SetActive(true);
        rig.goldenTwin.root.SetActive(true);
        rig.Parry.Burst(rig.goldenTwin.root.transform.position,
            tuning.GoldenColor, 10);
        rig.goldenHideTime = Time.time + tuning.GoldenShowS;
        rig.Parry.Burst(rig.goldenMelon.root.transform.position,
            tuning.GoldenColor, 10);
        rig.hud.PopText("Golden watermelon!",
            rig.goldenMelon.root.transform.position,
            UIStyle.Gold);
        rig.hud.ShowBannerFor("Golden watermelon!",
            "Grab it for a Golden Armor",
            UIStyle.Gold, 3f);
    }

    // as the Cobra King's golden watermelon: its reach outline swapped for a
    // thick gold one, the melon tinted gold
    public void MakeGolden(GameObject melon) {
        foreach (FruitOutlineListener listener in
            melon.GetComponentsInChildren<FruitOutlineListener>(true)) {
            UnityEngine.Object.Destroy(listener);
        }
        Material outline = null;
        if (tuning.GoldenOutlineMaterial != null) {
            outline = new Material(tuning.GoldenOutlineMaterial);
            outline.SetColor(OUTLINE_COLOR, tuning.GoldenOutlineColor);
            outline.SetFloat(OUTLINE_WIDTH,
                tuning.GoldenOutlineMaterial.GetFloat(OUTLINE_WIDTH)
                    * tuning.GoldenOutlineWidthScale);
        }
        foreach (Renderer part in
            melon.GetComponentsInChildren<Renderer>(true)) {
            Material baseMaterial = part.sharedMaterial;
            if (outline != null) {
                part.sharedMaterials = new[] { baseMaterial, outline };
            }
            rig.Feedback.SetColor(part, tuning.GoldenColor);
        }
    }

    public void HideGoldenMelon() {
        if (rig.goldenMelon == null || rig.goldenMelon.branch == null) {
            return;
        }
        if (rig.goldenRestScale == Vector3.zero) {
            rig.goldenRestScale = rig.goldenMelon.root.transform.localScale;
        }
        rig.goldenMelon.root.transform.localScale = rig.goldenRestScale;
        ForgetFruits(rig.goldenMelon.branch);
        rig.goldenMelon.branch.SetActive(false);
        // both go together: one grabbed, the other is gone too
        if (rig.goldenTwin != null && rig.goldenTwin.branch != null) {
            rig.goldenTwin.root.transform.localScale = rig.goldenRestScale;
            ForgetFruits(rig.goldenTwin.branch);
            rig.goldenTwin.branch.SetActive(false);
        }
    }

    public void UpdateMelons() {
        foreach (SpiderKingMelon melon in rig.melons) {
            if (melon.root == null || melon.root.activeSelf
                || melon.backTime < 0f || Time.time < melon.backTime) {
                continue;
            }
            melon.root.SetActive(true);
            melon.backTime = -1f;
        }
    }

    // the arena starts full of sticky webs, kept off the melons & the
    // monkey's start spot, never overlapping
    // the spider spins all the way it crawls: a web every webLayEveryU
    // where it is, off the monkey, the melons & other webs; none in a dash
    public void LayWebs(float movedU) {
        if (rig.teach != SpiderKingTeachStep.Done || rig.isDashing) {
            return;
        }
        rig.layDistanceU += movedU;
        if (rig.layDistanceU < tuning.WebLayEveryU) {
            return;
        }
        Vector3 spot =
            rig.PointOnTrunk(rig.spiderAngle, rig.spiderY,
                tuning.TrunkRadiusU + 0.02f);
        if (IsWebSpotFree(spot)) {
            rig.layDistanceU = 0f;
            PlaceWeb(rig.spiderAngle, rig.spiderY);
        }
    }

    // each web goes by itself after webLifeS, shrinking its last second;
    // never the one holding the monkey
    public void UpdateWebs() {
        for (int i = rig.stickyWebs.Count - 1; i >= 0; i--) {
            SpiderKingLaidWeb laid = rig.stickyWebs[i];
            if (laid.web == null) {
                rig.stickyWebs.RemoveAt(i);
                continue;
            }
            if (IsWebHoldingMonkey(laid.web)) {
                continue;
            }
            float leftS = laid.endTime - Time.time;
            if (leftS <= 0f) {
                laid.web.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(laid.web.gameObject);
                rig.stickyWebs.RemoveAt(i);
                continue;
            }
            if (leftS < 1f) {
                laid.web.transform.localScale = laid.restScale
                    * Mathf.Lerp(0.2f, 1f, leftS);
            }
        }
    }

    public bool IsWebSpotFree(Vector3 spot) {
        if (Vector3.Distance(spot,
            rig.monkey.position) < tuning.WebMonkeyGapU) {
            return false;
        }
        foreach (SpiderKingLaidWeb laid in rig.stickyWebs) {
            if (laid.web != null
                && Vector3.Distance(laid.web.transform.position,
                    spot) < tuning.WebGapU) {
                return false;
            }
        }
        foreach (SpiderKingMelon melon in rig.melons) {
            if (melon.root != null
                && Vector3.Distance(melon.root.transform.position,
                    spot) < 2.5f) {
                return false;
            }
        }
        return true;
    }

    // as the segments' spawner: the web's face looks away from the trunk; a
    // copy of the light template, not the 158 object prefab, as the spider
    // lays several a second
    public void PlaceWeb(float angle, float y) {
        SpiderWebTrap web = UnityEngine.Object.Instantiate(
            LaidWebTemplate(),
            rig.PointOnTrunk(angle, y, tuning.TrunkRadiusU + 0.02f),
            Quaternion.Euler(0f, -angle, 0f),
            rig.host
        );
        rig.stickyWebs.Add(new SpiderKingLaidWeb {
            web = web,
            endTime = Time.time + tuning.WebLifeS,
            restScale = web.transform.localScale,
        });
    }

    // the web prefab once, kept under an inactive holder (so nothing of it
    // runs), its strand cubes merged into 1 mesh on its root: the same look
    // & trap in 2 objects & 1 draw instead of 158 & 157
    private SpiderWebTrap LaidWebTemplate() {
        if (laidWebTemplate != null) {
            return laidWebTemplate;
        }
        GameObject holder = new GameObject("LaidWebTemplate");
        holder.SetActive(false);
        holder.transform.SetParent(rig.host, false);
        SpiderWebTrap template = UnityEngine.Object.Instantiate(
            tuning.WebPrefab, holder.transform, false);
        Transform root = template.transform;
        MeshFilter[] strands = root.GetComponentsInChildren<MeshFilter>(true);
        List<CombineInstance> parts = new List<CombineInstance>();
        Material material = null;
        foreach (MeshFilter strand in strands) {
            if (strand.transform == root || strand.sharedMesh == null) {
                continue;
            }
            Renderer strandRenderer = strand.GetComponent<Renderer>();
            if (material == null && strandRenderer != null) {
                material = strandRenderer.sharedMaterial;
            }
            parts.Add(new CombineInstance {
                mesh = strand.sharedMesh,
                transform = root.worldToLocalMatrix
                    * strand.transform.localToWorldMatrix,
            });
        }
        if (parts.Count == 0) {
            laidWebTemplate = template;
            return laidWebTemplate;
        }
        Mesh merged = new Mesh { name = "LaidWebStrands" };
        merged.CombineMeshes(parts.ToArray(), true, true);
        foreach (MeshFilter strand in strands) {
            if (strand.transform != root) {
                UnityEngine.Object.DestroyImmediate(strand.gameObject);
            }
        }
        root.gameObject.AddComponent<MeshFilter>().sharedMesh = merged;
        MeshRenderer look = root.gameObject.AddComponent<MeshRenderer>();
        look.sharedMaterial = material;
        look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laidWebTemplate = template;
        return laidWebTemplate;
    }

    private SpiderWebTrap laidWebTemplate;

    public bool IsWebHoldingMonkey(SpiderWebTrap web) {
        return rig.struggle != null && rig.struggle.IsTrapped
            && rig.monkey != null
            && Vector3.Distance(web.transform.position,
                rig.monkey.position) < 2f;
    }

    public void ClearStickyWebs() {
        foreach (SpiderKingLaidWeb laid in rig.stickyWebs) {
            if (laid.web != null) {
                UnityEngine.Object.Destroy(laid.web.gameObject);
            }
        }
        rig.stickyWebs.Clear();
    }

    // Mode Choice  ------------------------------------------------------------
    // the two choice melons beside where the fight began; the rest hidden
    public void SpawnChoices() {
        ClearChoices();
        foreach (SpiderKingMelon melon in rig.melons) {
            if (melon.branch != null) {
                ForgetFruits(melon.branch);
                melon.branch.SetActive(false);
            }
        }
        float angle = rig.AngleOf(rig.entryPosition);
        float y = rig.entryY + tuning.ChoiceAboveU;
        rig.choiceEasy = AddMelon(angle - tuning.ChoiceSideDeg, y);
        rig.choiceNormal = AddMelon(angle + tuning.ChoiceSideDeg, y);
        MakeGolden(rig.choiceNormal.root);
    }

    public void ClearChoices() {
        foreach (SpiderKingMelon choice in new[] { rig.choiceEasy,
            rig.choiceNormal }) {
            if (choice != null && choice.branch != null) {
                ForgetFruits(choice.branch);
                UnityEngine.Object.Destroy(choice.branch);
            }
        }
        rig.choiceEasy = null;
        rig.choiceNormal = null;
        rig.hud.SetChoiceTags(false, Vector3.zero, Vector3.zero);
    }

    public void UpdateChoiceTags() {
        if (rig.teach != SpiderKingTeachStep.Choose || rig.choiceEasy == null
            || rig.choiceNormal == null) {
            return;
        }
        rig.hud.SetChoiceTags(true, rig.choiceEasy.root.transform.position,
            rig.choiceNormal.root.transform.position);
    }

    // easy: less health, slower & gentler, never past the 2nd stage
    public void ChooseMode(bool isEasyPick) {
        rig.isEasy = isEasyPick;
        ClearChoices();
        foreach (SpiderKingMelon melon in rig.melons) {
            if (melon.branch != null) {
                melon.branch.SetActive(true);
                melon.root.SetActive(true);
                melon.backTime = -1f;
            }
        }
        rig.maxHealth = rig.isEasy
            ? Mathf.Max(1,
                Mathf.RoundToInt(rig.BaseMaxHealth * tuning.EasyHealthScale))
            : rig.BaseMaxHealth;
        rig.health = rig.maxHealth;
        rig.hud.SetHealth(1f);
        rig.lastStage = 0;
        // the parry lesson again if it was never passed
        rig.teach = rig.hasTaughtWeb ? SpiderKingTeachStep.Ready
            : SpiderKingTeachStep.Parry;
        rig.teachTimeS = 0f;
        rig.Teaching.Teach(rig.isEasy ? "EASY" : "NORMAL", "", rig.isEasy
            ? new Color(0.55f, 0.95f, 0.55f)
            : UIStyle.Gold, tuning.ReadyS);
    }

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

}
