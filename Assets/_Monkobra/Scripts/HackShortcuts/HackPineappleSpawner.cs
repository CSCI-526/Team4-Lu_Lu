using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the monkey: its hack keys spawn a branch bearing a pineapple or a
/// coconut, straight above the monkey at a fixed height. Belongs to no
/// segment, so it stays put
/// when the tree recycles.
/// </summary>
public class HackPineappleSpawner: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("dev/hack keys, this one works in a development build only")]
    private HackShortcutConfig hackShortcuts;

    [SerializeField]
    [Tooltip("branch prefab to spawn, same one the tree segments use")]
    private BranchController branchPrefab;

    [SerializeField]
    [Tooltip("coconut hung on the coconut branch's stem")]
    private GameObject coconutPrefab;

    [SerializeField]
    [Tooltip("gives branch's distance from trunk axis")]
    private BranchFruitPlacementConfig placementConfig;

    [SerializeField]
    [Min(0f)]
    [Tooltip("height of branch above monkey; u")]
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
        if (branchPrefab == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Field: "
                    + "branchPrefab",
                this
            );
        }
        if (coconutPrefab == null) {
            Debug.LogWarning(
                "HackPineappleSpawner:\tmust assign Inspector Field: "
                    + "coconutPrefab",
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
    }

    private void Update() {
        if (
            hackShortcuts == null
            || branchPrefab == null
            || placementConfig == null
            || !HackShortcutConfig.IsActive
            || Time.timeScale <= 0f
        ) {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) {
            return;
        }
        Key pineappleKey = hackShortcuts.GetKey(HackAction.SpawnPineapple);
        if (
            pineappleKey != Key.None
            && keyboard[pineappleKey].wasPressedThisFrame
        ) {
            SpawnBranch(false);
        }
        Key coconutKey = hackShortcuts.GetKey(HackAction.SpawnCoconut);
        if (
            coconutKey != Key.None
            && keyboard[coconutKey].wasPressedThisFrame
        ) {
            SpawnBranch(true);
        }
    }

    // private methods  ########################################################
    // branch grows from trunk axis, parent of monkey's orbit, at monkey's angle
    private void SpawnBranch(bool isCoconut) {
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
            branchPrefab,
            position,
            Quaternion.LookRotation(outward)
        );
        if (isCoconut) {
            // bare branch first, then the coconut on its stem tip
            branch.Initialize(0f, 0f);
            branch.TryHangFruit(coconutPrefab, null);
        } else {
            branch.Initialize(0f, 1f);
        }

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
