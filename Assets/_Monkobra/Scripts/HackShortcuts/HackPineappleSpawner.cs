using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the monkey: its hack keys spawn a branch bearing a pineapple or a
/// coconut, a falling cobra, or a spider web, straight above the monkey at a
/// fixed height. Belongs to no segment, so it stays put when the tree
/// recycles.
/// </summary>
public class HackPineappleSpawner: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("dev/hack keys, this one works in a development build only")]
    private HackShortcutConfig hackShortcuts;

    [SerializeField]
    [Tooltip("pineapple branch prefab, the one the tree segments use")]
    private BranchController pineappleBranchPrefab;

    [SerializeField]
    [Tooltip("coconut branch prefab, the one the tree segments use")]
    private BranchController coconutBranchPrefab;

    [SerializeField]
    [Tooltip("gives branch's distance from trunk axis")]
    private BranchFruitPlacementConfig placementConfig;

    [SerializeField]
    [Tooltip("falling cobra prefab, hung dormant above the monkey")]
    private FallingCobraController cobraPrefab;

    [SerializeField]
    [Tooltip("cobra balance tuning; fall and slide speeds of the cobra")]
    private CobraBalanceConfig cobraBalanceConfig;

    [SerializeField]
    [Tooltip("spider web prefab, stuck on the trunk above the monkey")]
    private SpiderWebTrap webPrefab;

    [SerializeField]
    [Min(0f)]
    [Tooltip("trunk radius the web sits on, as in SpiderWebSpawner; u")]
    private float trunkRadiusU = 5.02f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("height of the spawn above monkey; u")]
    private float spawnAboveU = 8f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (hackShortcuts == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Field: "
                    + "hackShortcuts",
                this
            );
        }
        if (pineappleBranchPrefab == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Field: "
                    + "pineappleBranchPrefab",
                this
            );
        }
        if (coconutBranchPrefab == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Field: "
                    + "coconutBranchPrefab",
                this
            );
        }
        if (placementConfig == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Field: "
                    + "placementConfig",
                this
            );
        }
        if (cobraPrefab == null || cobraBalanceConfig == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Fields: "
                    + "cobraPrefab & cobraBalanceConfig",
                this
            );
        }
        if (webPrefab == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Field: "
                    + "webPrefab",
                this
            );
        }
    }

    private void Update() {
        if (
            hackShortcuts == null
            || !HackShortcutConfig.IsActive
            || Time.timeScale <= 0f
        ) {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) {
            return;
        }
        if (IsPressed(keyboard, HackAction.SpawnPineapple)) {
            SpawnBranch(false);
        }
        if (IsPressed(keyboard, HackAction.SpawnCoconut)) {
            SpawnBranch(true);
        }
        if (IsPressed(keyboard, HackAction.SpawnFallingCobra)) {
            SpawnFallingCobra();
        }
        if (IsPressed(keyboard, HackAction.SpawnSpiderWeb)) {
            SpawnSpiderWeb();
        }
    }

    // private methods  ########################################################
    // if the action's key went down this frame
    private bool IsPressed(Keyboard keyboard, HackAction action) {
        Key key = hackShortcuts.GetKey(action);
        return key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    // horizontal direction from the trunk axis out to the monkey, zero if the
    // monkey stands on the axis
    private Vector3 GetOutward() {
        Vector3 outward = transform.position - transform.parent.position;
        outward.y = 0f;
        return outward;
    }

    // a dormant falling cobra on the monkey's ring & angle, above the monkey,
    // so it wakes and drops on it
    private void SpawnFallingCobra() {
        Vector3 outward = GetOutward();
        if (
            cobraPrefab == null
            || cobraBalanceConfig == null
            || outward.sqrMagnitude <= Mathf.Epsilon
        ) {
            return;
        }

        Vector3 axis = transform.parent.position;
        Vector3 position =
            axis
            + outward
            + Vector3.up * (transform.position.y - axis.y + spawnAboveU);
        FallingCobraController cobra = Instantiate(
            cobraPrefab,
            position,
            Quaternion.identity
        );
        Physics.SyncTransforms();
        cobra.Initialize(cobraBalanceConfig, axis);

        if (Debug.isDebugBuild) {
            Debug.Log(
                $"HackPineappleSpawner:\tfalling cobra at y {position.y}",
                this
            );
        }
    }

    // a web flat on the bark above the monkey, face turned away from the trunk
    // as SpiderWebSpawner lays it
    private void SpawnSpiderWeb() {
        Vector3 outward = GetOutward();
        if (webPrefab == null || outward.sqrMagnitude <= Mathf.Epsilon) {
            return;
        }

        Vector3 axis = transform.parent.position;
        Quaternion yaw = Quaternion.LookRotation(outward.normalized);
        Vector3 position =
            axis
            + outward.normalized * trunkRadiusU
            + Vector3.up * (transform.position.y - axis.y + spawnAboveU);
        Instantiate(webPrefab, position, yaw * Quaternion.Euler(0f, -90f, 0f));

        if (Debug.isDebugBuild) {
            Debug.Log(
                $"HackPineappleSpawner:\tspider web at y {position.y}",
                this
            );
        }
    }

    // branch grows from trunk axis, parent of monkey's orbit, at monkey's angle
    private void SpawnBranch(bool isCoconut) {
        BranchController prefab =
            isCoconut ? coconutBranchPrefab : pineappleBranchPrefab;
        if (prefab == null || placementConfig == null) {
            return;
        }

        Vector3 axis = transform.parent.position;
        Vector3 outward = transform.position - axis;
        outward.y = 0f;
        if (outward.sqrMagnitude <= Mathf.Epsilon) {
            return;
        }

        Vector3 position =
            axis
            + outward.normalized * placementConfig.BranchRadius
            + Vector3.up * (transform.position.y - axis.y + spawnAboveU);
        BranchController branch = Instantiate(
            prefab,
            position,
            Quaternion.LookRotation(outward)
        );
        branch.Setup();

        if (Debug.isDebugBuild) {
            Debug.Log(
                "HackPineappleSpawner:\t"
                    + (isCoconut ? "coconut" : "pineapple")
                    + $" branch at y {position.y}",
                this
            );
        }
    }
}
