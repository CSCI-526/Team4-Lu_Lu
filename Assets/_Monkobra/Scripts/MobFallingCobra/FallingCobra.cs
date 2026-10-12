using UnityEngine;

/// <summary>
/// A cobra hanging dormant on the tree that drops on the monkey. Spawned as a
/// child of a tree segment by <see cref="FallingCobraSpawner"/>, which hands
/// it the balance config and the tree axis through
/// <see cref="Initialize"/>.
/// <para>
/// Three states. Dormant: nothing moves until the monkey comes within
/// <c>triggerDistanceU</c> (3D) and the cobra is at or above the monkey's
/// height. Warning: a red pulse for <c>warningDurationS</c>. Falling: a
/// constant downward speed, while each physics step the cobra re-reads the
/// monkey's angle about the tree axis and slides round the trunk toward it by
/// the shortest arc, so it keeps homing on a monkey that orbits. After
/// <c>maxFallDistanceU</c> it switches itself off; the segment's restart
/// destroys it later.
/// </para>
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FallingCobra: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>pivot to the far end of the snake, hit radius included; u
    /// </returns>
    public float LengthU => hitCollider.height;

    /// <returns>how far the snake's tail reaches below its pivot; u</returns>
    public float TailBelowPivotU => hitRadiusU;

    // Public Methods  #########################################################
    /// <summary>
    /// Arms the cobra. The ring it slides on is the horizontal distance from
    /// its current position to <paramref name="treeAxisPosition"/>, so call it
    /// once the cobra stands where it will hang.
    /// </summary>
    /// <param name="config">balance tuning; fall and slide speeds</param>
    /// <param name="treeAxisPosition">any world point on the trunk axis
    /// </param>
    public void Initialize(
        GameBalanceConfig config,
        Vector3 treeAxisPosition
    ) {
        if (config == null) {
            Debug.LogWarning(
                "FallingCobra:\tInitialize needs a GameBalanceConfig, stay "
                    + "dormant",
                this
            );
            return;
        }

        fallSpeedU = config.FallingCobraFallSpeedU;
        horizontalSpeedU = config.FallingCobraHorizontalSpeedU;
        axisXZ = new Vector2(treeAxisPosition.x, treeAxisPosition.z);
        Vector3 position = transform.position;
        ringRadiusU = Vector2.Distance(
            new Vector2(position.x, position.z),
            axisXZ
        );
        isInitialized = true;
    }

    // Inspector Fields  #######################################################
    [Header("Trigger")]
    [SerializeField]
    [Tooltip("3D distance to the monkey below which the cobra wakes; u")]
    private float triggerDistanceU = 12f;

    [Header("Warning and Fall")]
    [SerializeField]
    [Tooltip("time of the red pulse b4 the fall; s")]
    private float warningDurationS = 1.25f;

    [SerializeField]
    private Color warningColor = new Color(1f, 0.15f, 0.1f, 1f);

    [SerializeField]
    [Tooltip("red pulses per s while warning")]
    private float warningPulseHz = 4f;

    [SerializeField]
    [Tooltip("distance fallen b4 the cobra switches itself off; u")]
    private float maxFallDistanceU = 30f;

    [Header("Body")]
    [SerializeField]
    private float segmentSpacingU = 0.35f;

    [SerializeField]
    private float hitRadiusU = 0.5f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        body = GetComponent<Rigidbody>();
        hitCollider = GetComponent<CapsuleCollider>();
        if (body == null || hitCollider == null) {
            Debug.LogError(
                "FallingCobra:\trequires Rigidbody and CapsuleCollider",
                this
            );
            enabled = false;
            return;
        }

        body.isKinematic = true;
        body.useGravity = false;

        float highestSegmentY = 0f;
        for (int i = 0; i < transform.childCount; i++) {
            Transform segment = transform.GetChild(i);
            if (segment.name.StartsWith("Segment_")) {
                segment.localPosition = Vector3.up * (i * segmentSpacingU);
                highestSegmentY = Mathf.Max(
                    highestSegmentY,
                    segment.localPosition.y
                );
            }
        }

        // One trigger covers the whole snake, so one collision creates one
        // branch-hit event and camera impulse instead of one per segment.
        foreach (
            Collider segmentCollider in GetComponentsInChildren<Collider>()
        ) {
            if (segmentCollider != hitCollider) {
                segmentCollider.enabled = false;
            }
        }
        hitCollider.direction = 1;
        hitCollider.radius = hitRadiusU;
        hitCollider.height = highestSegmentY + hitRadiusU * 2f;
        hitCollider.center = Vector3.up * highestSegmentY * 0.5f;
        hitCollider.isTrigger = true;
        hitCollider.enabled = false;

        snakeRenderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[snakeRenderers.Length];
        warningBlock = new MaterialPropertyBlock();

        for (int i = 0; i < snakeRenderers.Length; i++) {
            Material material = snakeRenderers[i].sharedMaterial;
            baseColors[i] =
                material != null && material.HasProperty(BASE_COLOR_ID)
                    ? material.GetColor(BASE_COLOR_ID)
                    : Color.white;
        }
    }

    private void Start() {
        GameObject playerObject = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObject != null) {
            player = playerObject.transform;
        }
    }

    private void Update() {
        if (hitMonkey && hitCollider.enabled) {
            hitCollider.enabled = false;
        }

        switch (state) {
            case State.Dormant:
                TryWake();
                break;
            case State.Warning:
                PulseWarning();
                break;
        }
    }

    private void FixedUpdate() {
        if (state == State.Warning && Time.time >= fallStartTime) {
            StartFalling();
        }
        if (state == State.Falling) {
            StepFall();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (
            other.CompareTag(PLAYER_TAG)
            || other.GetComponent<HitBranchDetection>() != null
        ) {
            // The monkey's detector handles the drop. Stop later contacts
            // after this physics step, once its own trigger callback has run.
            hitMonkey = true;
        }
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        triggerDistanceU = Mathf.Max(0f, triggerDistanceU);
        warningDurationS = Mathf.Max(0f, warningDurationS);
        warningPulseHz = Mathf.Max(0f, warningPulseHz);
        maxFallDistanceU = Mathf.Max(0f, maxFallDistanceU);
        segmentSpacingU = Mathf.Max(0f, segmentSpacingU);
        hitRadiusU = Mathf.Max(0.01f, hitRadiusU);
    }

    // constants  ##############################################################
    private const string PLAYER_TAG = "Player";
    private static readonly int BASE_COLOR_ID =
        Shader.PropertyToID("_BaseColor");

    // private types  ############################################################
    private enum State {
        Dormant,
        Warning,
        Falling,
        Done,
    }

    // private members  ########################################################
    private State state = State.Dormant;
    private bool isInitialized; // if the spawner armed this cobra
    private bool hitMonkey;
    private float fallSpeedU; // from config; u/s
    private float horizontalSpeedU; // from config; u/s along the ring
    private Vector2 axisXZ; // tree axis, world xz
    private float ringRadiusU; // horizontal distance to the axis; u
    private float warningStartTime;
    private float fallStartTime;
    private float fallenU;

    // cached references  ------------------------------------------------------
    private Rigidbody body;
    private CapsuleCollider hitCollider;
    private Renderer[] snakeRenderers;
    private Color[] baseColors;
    private MaterialPropertyBlock warningBlock;
    private Transform player;

    // private methods  ########################################################
    // wakes when the monkey is close and the cobra is not below it, a fall
    // from beside or under the monkey is useless
    private void TryWake() {
        if (!isInitialized || player == null) {
            return;
        }
        Vector3 position = transform.position;
        if (position.y < player.position.y) {
            return;
        }
        if (Vector3.Distance(position, player.position) >= triggerDistanceU) {
            return;
        }

        state = State.Warning;
        warningStartTime = Time.time;
        fallStartTime = warningStartTime + warningDurationS;
    }

    private void PulseWarning() {
        float pulse =
            0.5f
            + 0.5f
                * Mathf.Cos(
                    (Time.time - warningStartTime)
                        * warningPulseHz
                        * 2f
                        * Mathf.PI
                );
        ApplyWarningColor(pulse);
    }

    private void ApplyWarningColor(float pulse) {
        for (int i = 0; i < snakeRenderers.Length; i++) {
            Renderer snakeRenderer = snakeRenderers[i];
            snakeRenderer.GetPropertyBlock(warningBlock);
            warningBlock.SetColor(
                BASE_COLOR_ID,
                Color.Lerp(baseColors[i], warningColor, pulse)
            );
            snakeRenderer.SetPropertyBlock(warningBlock);
        }
    }

    private void StartFalling() {
        state = State.Falling;
        foreach (Renderer snakeRenderer in snakeRenderers) {
            snakeRenderer.SetPropertyBlock(null);
        }
        hitCollider.enabled = true;
    }

    // constant drop, plus a slide round the trunk toward the monkey's live
    // angle; the slide is clamped to the remaining arc so it never overshoots
    private void StepFall() {
        float dropU = fallSpeedU * Time.fixedDeltaTime;
        Vector3 position = body.position;
        position.y -= dropU;

        if (player != null && ringRadiusU > MIN_RING_RADIUS_U) {
            Vector2 fromAxis = new Vector2(position.x, position.z) - axisXZ;
            Vector2 toPlayer =
                new Vector2(player.position.x, player.position.z) - axisXZ;
            if (toPlayer.sqrMagnitude > MIN_RING_RADIUS_U * MIN_RING_RADIUS_U) {
                float angleDeg =
                    Mathf.Atan2(fromAxis.x, fromAxis.y) * Mathf.Rad2Deg;
                float targetDeg =
                    Mathf.Atan2(toPlayer.x, toPlayer.y) * Mathf.Rad2Deg;
                float maxStepDeg =
                    horizontalSpeedU
                    * Time.fixedDeltaTime
                    / ringRadiusU
                    * Mathf.Rad2Deg;
                float stepDeg = Mathf.Clamp(
                    Mathf.DeltaAngle(angleDeg, targetDeg),
                    -maxStepDeg,
                    maxStepDeg
                );

                float newRad = (angleDeg + stepDeg) * Mathf.Deg2Rad;
                position.x = axisXZ.x + Mathf.Sin(newRad) * ringRadiusU;
                position.z = axisXZ.y + Mathf.Cos(newRad) * ringRadiusU;
            }
        }
        body.MovePosition(position);

        fallenU += dropU;
        if (fallenU >= maxFallDistanceU) {
            state = State.Done;
            hitCollider.enabled = false;
            gameObject.SetActive(false);
        }
    }

    private const float MIN_RING_RADIUS_U = 0.001f;
}
