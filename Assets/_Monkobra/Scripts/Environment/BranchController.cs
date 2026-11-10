using UnityEngine;

/// <summary>
/// Prefab root for one branch. Each <see cref="BranchKind"/> is its own
/// prefab (a variant of the base branch), so the kind is data on the prefab:
/// on <see cref="Setup"/> the branch shows the fruit its kind names, or none.
/// A branch w/o fruit disables the Banana + Banana Stem children; one w/
/// fruit scales the stem and places the fruit on it. A coconut or pineapple
/// branch hangs its own <c>fruitPrefab</c> where the banana would grow.
/// </summary>
public class BranchController: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>which fruit this branch bears, if any</returns>
    public BranchKind Kind => kind;

    // Public Methods  #########################################################
    /// <summary>
    /// Shows this branch's fruit by its kind and hides the rest. Called once
    /// by the spawner after the branch is placed.
    /// </summary>
    public void Setup() {
        if (banana == null || bananaStem == null) {
            return;
        }

        switch (kind) {
            case BranchKind.Banana:
                PlaceFruitOnStem();
                break;
            case BranchKind.Pineapple:
                PlaceFruitOnStem();
                AttachPineapple();
                break;
            case BranchKind.Coconut:
                PlaceFruitOnStem();
                AttachCoconut();
                break;
            default:
                banana.SetActive(false);
                bananaStem.SetActive(false);
                break;
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("which fruit this branch bears; the prefab variant sets it")]
    private BranchKind kind = BranchKind.Banana;

    [SerializeField]
    [Tooltip("banana child; disabled w/o banana")]
    private GameObject banana;

    [SerializeField]
    [Tooltip("banana stem child; scaled w/ fruit")]
    private GameObject bananaStem;

    [SerializeField]
    [Tooltip(
        "fruit hung on the stem of a coconut or pineapple branch; its pivot "
            + "is where it attaches, ie its top"
    )]
    private GameObject fruitPrefab;

    [SerializeField]
    [Tooltip("nudge of the hung fruit, in the banana's local axes; u")]
    private Vector3 fruitOffsetU = Vector3.zero;

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
        if (
            (kind == BranchKind.Coconut || kind == BranchKind.Pineapple)
            && fruitPrefab == null
        ) {
            Debug.LogWarning(
                "BranchController:\t"
                    + "must assign Inspector Field: fruitPrefab for a "
                    + kind + " branch",
                this
            );
        }
    }

    // constants  ##############################################################
    private const float STEM_Y_SCALE_MIN = 0.5f;
    private const float STEM_Y_SCALE_MAX = 2.0f;
    private const float BANANA_Y_OFFSET = -.2f;

    // lower end of the stem: a capsule, 2 u long at scale 1 on its local y
    private Vector3 StemTipPosition =>
        bananaStem.transform.TransformPoint(Vector3.down);

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
        if (fruitPrefab == null) {
            return;
        }
        GameObject pineapple = Instantiate(
            fruitPrefab,
            banana.transform.parent
        );
        pineapple.transform.SetLocalPositionAndRotation(
            banana.transform.localPosition
                + banana.transform.localRotation * fruitOffsetU,
            banana.transform.localRotation
        );
        banana.SetActive(false);
    }

    // coconut's pivot, its top, sits on the stem's lower tip, turned like
    // the banana; banana stays off
    private void AttachCoconut() {
        if (fruitPrefab == null) {
            return;
        }
        GameObject coconut = Instantiate(fruitPrefab, banana.transform.parent);
        coconut.transform.SetPositionAndRotation(
            StemTipPosition
                + banana.transform.rotation * fruitOffsetU,
            banana.transform.rotation
        );
        banana.SetActive(false);
    }
}
