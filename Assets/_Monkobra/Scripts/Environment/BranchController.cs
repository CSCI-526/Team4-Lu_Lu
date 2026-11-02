using UnityEngine;

/// <summary>
/// Prefab root for one branch. Decides on <see cref="Initialize"/> which fruit
/// this branch bears: a pineapple, a banana, or none. A branch w/o fruit
/// disables the Banana + Banana Stem children; one w/ fruit scales the stem
/// and places the fruit on it.
/// </summary>
public class BranchController: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Rolls this branch's fruit, pineapple first, then shows or hides the
    /// fruit children. Called once by the spawner after the branch is placed.
    /// </summary>
    /// <param name="bananaProbability">chance of a banana; 0~1</param>
    /// <param name="pineappleProbability">chance of a pineapple; 0~1</param>
    public void Initialize(
        float bananaProbability,
        float pineappleProbability
    ) {
        if (banana == null || bananaStem == null) {
            return;
        }

        if (
            pineapplePrefab != null
            && Random.value < pineappleProbability
        ) {
            PlaceFruitOnStem();
            AttachPineapple();
        } else if (Random.value < bananaProbability) {
            PlaceFruitOnStem();
        } else {
            banana.SetActive(false);
            bananaStem.SetActive(false);
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("banana child; disabled w/o banana")]
    private GameObject banana;

    [SerializeField]
    [Tooltip("banana stem child; scaled w/ fruit")]
    private GameObject bananaStem;

    [SerializeField]
    [Tooltip("pineapple spawned at banana's spot on stem")]
    private GameObject pineapplePrefab;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (banana == null) {
            Debug.LogWarning(
                "BranchController:\tmust assign Inspector Field: banana",
                this
            );
        }
        if (bananaStem == null) {
            Debug.LogWarning(
                "BranchController:\t"
                    + "must assign Inspector Field: bananaStem",
                this
            );
        }
        if (pineapplePrefab == null) {
            Debug.LogWarning(
                "BranchController:\t"
                    + "must assign Inspector Field: pineapplePrefab",
                this
            );
        }
    }

    // constants  ##############################################################
    private const float STEM_Y_SCALE_MIN = 0.5f;
    private const float STEM_Y_SCALE_MAX = 2.0f;
    private const float BANANA_Y_OFFSET = -.2f;

    // private methods  ########################################################
    // scale stem, carry banana child to its tip at random turn
    private void PlaceFruitOnStem() {
        Vector3 stemBasePosition = bananaStem.transform.localPosition;
        Vector3 bananaBaseOffset =
            banana.transform.localPosition - stemBasePosition;

        float stemYScale = Random.Range(STEM_Y_SCALE_MIN, STEM_Y_SCALE_MAX);
        Vector3 stemScale = bananaStem.transform.localScale;
        bananaStem.transform.localScale = new Vector3(
            stemScale.x,
            stemScale.y * stemYScale,
            stemScale.z
        );

        banana.transform.localPosition =
            stemBasePosition
            + bananaBaseOffset * stemYScale
            + Vector3.up * BANANA_Y_OFFSET;
        banana.transform.localRotation = Quaternion.Euler(
            0f,
            Random.Range(0f, 360f),
            0f
        );
    }

    // pineapple takes the placed banana's pose; banana stays off
    private void AttachPineapple() {
        GameObject pineapple = Instantiate(
            pineapplePrefab,
            banana.transform.parent
        );
        pineapple.transform.SetLocalPositionAndRotation(
            banana.transform.localPosition,
            banana.transform.localRotation
        );
        banana.SetActive(false);
    }
}
