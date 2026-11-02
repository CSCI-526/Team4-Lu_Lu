using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the monkey: its hack key spawns a branch bearing a pineapple, straight
/// above the monkey at a fixed height. Belongs to no segment, so it stays put
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
        Key spawnKey = hackShortcuts.GetKey(HackAction.SpawnPineapple);
        if (
            keyboard != null
            && spawnKey != Key.None
            && keyboard[spawnKey].wasPressedThisFrame
        ) {
            SpawnBranch();
        }
    }

    // private methods  ########################################################
    // branch grows from trunk axis, parent of monkey's orbit, at monkey's angle
    private void SpawnBranch() {
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
        branch.Initialize(0f, 1f);

        if (Debug.isDebugBuild) {
            Debug.Log(
                $"HackPineappleSpawner:\tpineapple branch at y {position.y}",
                this
            );
        }
    }
}
