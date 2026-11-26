using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Sticks <see cref="SpiderWebTrap"/> webs flat on the trunk of each
/// <see cref="TreeSegmentController"/> as soon as it has rolled its
/// branches, on its first Start and after every restart by
/// <see cref="DynamicTreeSegmentPool"/>, by listening to
/// <see cref="TreeSegmentController.BranchesGenerated"/>. Put one on
/// any object active from scene load, never under the pool's mock tree,
/// which is deactivated at runtime.
/// <para>
/// Spaced like branches by <see cref="TrunkScatter"/>, on its Web channel:
/// the spots whose height falls in the segment, from the web density curve
/// of <see cref="WorldBalanceConfig"/> read at each height's ramped
/// difficulty from <see cref="RampedDifficultyManager"/>. A spot keeps its
/// height, a blocked one retries its yaw.
/// </para>
/// <para>
/// A web lies on its segment's own stretch of bark facing outward: the
/// prefab's strands sit in its local YZ plane, so local +X, the face normal,
/// is turned away from the trunk axis. A candidate spot is dropped if it is
/// too close to another web, or its trigger volume overlaps any branch or
/// fruit. Out of retries, the web is skipped rather than placed overlapping.
/// </para>
/// <para>
/// Webs are children of their segment, so a restart's clear removes them.
/// The spawner also drops a segment's previous webs before rolling it again,
/// so none linger and none double up.
/// </para>
/// </summary>
public class SpiderWebSpawner: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip(
        "web prefab, its trigger BoxCollider is the volume kept clear of "
            + "branches and fruit"
    )]
    private SpiderWebTrap webPrefab;

    [SerializeField]
    [Tooltip("world balance tuning; web density curve")]
    private WorldBalanceConfig worldBalanceConfig;

    [Header("Web Position")]
    [SerializeField]
    [Tooltip(
        "trunk radius the web sits on, half the TreeSegment cylinder's "
            + "X scale; u"
    )]
    private float trunkRadiusU = 5f;

    [SerializeField]
    [Tooltip("gap b/t bark and web plane, keeps strands out of the trunk; u")]
    private float surfaceOffsetU = 0.02f;

    [SerializeField]
    [Tooltip("min distance b/t web centers; u")]
    private float minWebSeparationU = 4f;

    [SerializeField]
    [Tooltip(
        "padding around the web trigger that must also be free of branches "
            + "and fruit; u"
    )]
    private float clearanceU = 0.3f;

    [Header("Testing")]
    [SerializeField]
    [Tooltip(
        "replace the balance density curve w/ the test density below. Tick it "
            + "b4 Play to cover the first stack, and leave the scene unsaved"
    )]
    private bool useTestOverride = false;

    [SerializeField]
    [Min(0f)]
    [FormerlySerializedAs("testSpawnChance")]
    [Tooltip(
        "expected webs per 10 u of trunk while testing, any difficulty. A "
            + "web may land on the monkey's start spot"
    )]
    private float testDensity = 3f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (webPrefab == null) {
            Debug.LogWarning(
                "SpiderWebSpawner:\tmust assign Inspector Field: webPrefab",
                this
            );
        }
        if (worldBalanceConfig == null) {
            Debug.LogWarning(
                "SpiderWebSpawner:\tmust assign Inspector Field: "
                    + "worldBalanceConfig",
                this
            );
        }
        if (webPrefab == null || worldBalanceConfig == null) {
            enabled = false;
            return;
        }

        // the prefab's trigger, scaled by its root, as each web will carry it
        prefabTrigger = webPrefab.GetComponent<BoxCollider>();
        Vector3 prefabScale = webPrefab.transform.localScale;
        webTriggerCenter = Vector3.Scale(prefabTrigger.center, prefabScale);
        webTriggerHalfSize =
            Vector3.Scale(prefabTrigger.size, prefabScale) * 0.5f;
    }

    // scene OnEnables all run b4 any Start, so no segment rolls unheard.
    // First wins: a 2nd listener would roll every segment twice
    private void OnEnable() {
        if (listener != null && listener != this) {
            Debug.LogWarning(
                "SpiderWebSpawner:\tanother spawner already listens, this "
                    + "one stays idle",
                this
            );
            return;
        }
        listener = this;
        TreeSegmentController.BranchesGenerated += OnBranchesGenerated;
    }

    private void OnDisable() {
        if (listener != this) {
            return;
        }
        TreeSegmentController.BranchesGenerated -= OnBranchesGenerated;
        listener = null;
    }

    // trigger volume of every spawned web, visible in the Scene view, and in
    // the Game view with Gizmos on
    private void OnDrawGizmos() {
        if (prefabTrigger == null) {
            return;
        }

        Gizmos.color = GIZMO_WEB_COLOR;
        foreach (SpiderWebTrap web in spawnedWebs) {
            if (web == null) {
                continue;
            }
            Gizmos.matrix = web.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(prefabTrigger.center, prefabTrigger.size);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        trunkRadiusU = Mathf.Max(0f, trunkRadiusU);
        surfaceOffsetU = Mathf.Max(0f, surfaceOffsetU);
        minWebSeparationU = Mathf.Max(0f, minWebSeparationU);
        clearanceU = Mathf.Max(0f, clearanceU);
    }

    // Event Handlers  #########################################################
    private void OnBranchesGenerated(TreeSegmentController segment) {
        PopulateSegment(segment);
    }

    // constants  ##############################################################
    private const string BRANCH_TAG = "Branch";
    private const string FRUIT_TAG = "FruitCollider";
    private static readonly Color GIZMO_WEB_COLOR = Color.yellow;

    // private members  ########################################################
    private static SpiderWebSpawner listener; // the one spawner subscribed

    private Vector3 webTriggerCenter; // web-local, prefab scale applied
    private Vector3 webTriggerHalfSize; // web-local, prefab scale applied
    private bool hasWarnedNoDifficulty; // missing service logged once
    private readonly List<SpiderWebTrap> spawnedWebs = new();
    private readonly List<ScatterItem> scatterItems = new();

    // cached references  ------------------------------------------------------
    private BoxCollider prefabTrigger;

    // private methods  ########################################################
    // rolls this spawner's web attempts for segment, which already holds its
    // branches
    private void PopulateSegment(TreeSegmentController segment) {
        if (segment == null || prefabTrigger == null) {
            return;
        }
        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null) {
            if (!hasWarnedNoDifficulty) {
                Debug.LogWarning(
                    "SpiderWebSpawner:\tno RampedDifficultyManager in scene, "
                        + "skip webs",
                    this
                );
                hasWarnedNoDifficulty = true;
            }
            return;
        }

        ClearWebs(segment);

        if (TrunkScatter.I == null) {
            Debug.LogWarning(
                "SpiderWebSpawner:\tno TrunkScatter in scene, skip webs",
                this
            );
            return;
        }

        Bounds bounds = segment.GetBounds();
        scatterItems.Clear();
        TrunkScatter.I.GetItems(
            ScatterChannel.Web,
            bounds.min.y,
            bounds.max.y,
            GetDensity,
            scatterItems
        );
        if (scatterItems.Count == 0) {
            return;
        }

        // branches were posed through their transforms this frame, push the
        // poses into physics so the overlap test can see them
        Physics.SyncTransforms();

        foreach (ScatterItem item in scatterItems) {
            TrySpawnWeb(segment, item);
        }
    }

    // a segment's previous webs go b4 it rolls again: its restart has
    // already cleared them, any still active are destroyed here, so a
    // segment never carries 2 rolls. Destroyed webs, e.g. broken free, are
    // forgotten on the way
    private void ClearWebs(TreeSegmentController segment) {
        for (int i = spawnedWebs.Count - 1; i >= 0; i--) {
            SpiderWebTrap web = spawnedWebs[i];
            if (web != null && web.transform.parent != segment.transform) {
                continue;
            }
            // inactive first, Destroy only lands at the end of the frame
            if (web != null && web.gameObject.activeSelf) {
                web.gameObject.SetActive(false);
                Destroy(web.gameObject);
            }
            spawnedWebs.RemoveAt(i);
        }
    }

    // expected webs per 10 u of trunk at a world height
    private float GetDensity(float worldY) {
        if (useTestOverride) {
            return testDensity;
        }
        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null) {
            return 0f;
        }
        return Mathf.Max(
            0f,
            worldBalanceConfig.WebDensityCurve.Evaluate(
                difficulty.GetRampedValueFromYPosition(worldY)
            )
        );
    }

    private bool TrySpawnWeb(TreeSegmentController segment, ScatterItem item) {
        Transform segmentXfm = segment.transform;

        // the trunk cylinder is centered on the segment pivot, so this keeps
        // the whole web on the segment's own bark, never past the tree top
        float webHalfHeight =
            webTriggerHalfSize.y + Mathf.Abs(webTriggerCenter.y);
        float maxLocalY = Mathf.Max(
            0f,
            segment.GetHeight() * 0.5f - webHalfHeight
        );
        // the spot keeps its height, only pulled in off the segment ends
        float localY = Mathf.Clamp(
            item.Y - segmentXfm.position.y,
            -maxLocalY,
            maxLocalY
        );

        int maxAttempts = TrunkScatter.I.MaxRetries + 1;
        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            Quaternion yaw = Quaternion.Euler(
                0f,
                item.GetRetryYawDeg(attempt),
                0f
            );
            Vector3 localPosition =
                yaw * Vector3.forward * (trunkRadiusU + surfaceOffsetU)
                + Vector3.up * localY;
            // -90 yaw turns local +X onto forward, then yaw turns forward
            // onto the outward direction, so the face looks away from trunk
            Quaternion localRotation = yaw * Quaternion.Euler(0f, -90f, 0f);

            Vector3 position = segmentXfm.TransformPoint(localPosition);
            Quaternion rotation = segmentXfm.rotation * localRotation;
            // only the boss fight grows anything in the boss zone
            if (IsInBossZone(position) || !IsSpotClear(position, rotation)) {
                continue;
            }

            SpiderWebTrap web = Instantiate(
                webPrefab,
                position,
                rotation,
                segmentXfm
            );
            spawnedWebs.Add(web);
            return true;
        }

        return false;
    }

    // iff the world y lies in the boss zone; false w/o a service
    private static bool IsInBossZone(Vector3 position) {
        return RampedDifficultyManager.I != null
            && RampedDifficultyManager.I.IsInBossZoneY(position.y);
    }

    private bool IsSpotClear(Vector3 position, Quaternion rotation) {
        // a web already cleared off a restarted segment is inactive and
        // never counts
        foreach (SpiderWebTrap web in spawnedWebs) {
            if (
                web != null
                && web.gameObject.activeInHierarchy
                && Vector3.Distance(web.transform.position, position)
                    < minWebSeparationU
            ) {
                return false;
            }
        }

        // triggers too, the fruit's grab volume is one
        Collider[] hits = Physics.OverlapBox(
            position + rotation * webTriggerCenter,
            webTriggerHalfSize + Vector3.one * clearanceU,
            rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );
        foreach (Collider hit in hits) {
            if (IsBranchOrFruit(hit)) {
                return false;
            }
        }
        return true;
    }

    // only branches and their fruit block a web. The web hugs the trunk, so
    // the trunk's own collider overlaps every candidate and must never count,
    // nor must anything else such as the monkey
    private static bool IsBranchOrFruit(Collider hit) {
        return hit.CompareTag(BRANCH_TAG)
            || hit.CompareTag(FRUIT_TAG)
            || hit.GetComponentInParent<BranchController>() != null;
    }
}
