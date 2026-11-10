using System;
using UnityEngine;

/// <summary>
/// The Cobra King, built at runtime from Unity primitives only, kept simple
/// and cute: a round bead coil wrapped round the tree top (its nest), a neck
/// hanging down the trunk toward the monkey, and a big round head with two
/// eyes and a small gold crown; thrown bananas fly at the crown's front. The head
/// turns round the trunk after the monkey and strikes down at a point:
/// a butt or a bite. Once defeated it shatters into cubes and lets go of
/// its crown.
/// <para>
/// Every bead and the head are triggers reporting to a
/// <see cref="BossKingContact"/> on the root, which raises
/// <see cref="MonkeyTouched"/>; touching the king is not fatal by itself.
/// </para>
/// </summary>
public class BossKingBody: MonoBehaviour {
    // Public Members  #########################################################
    /// <summary>Raised while any part of the monkey touches the king.
    /// </summary>
    public event Action MonkeyTouched;

    /// <returns>world position of the crown jewel, the weak spot</returns>
    public Vector3 WeakSpotPosition => weakSpot != null
        ? weakSpot.transform.position
        : transform.position;

    /// <returns>world position of the head, eg for the speech bubble
    /// </returns>
    public Vector3 HeadPosition => head != null
        ? head.position
        : transform.position;

    /// <returns>world y of the head's chin at rest</returns>
    public float LowestY => restHeadPosition.y - headSize.y * 0.5f;

    /// <returns>how far the king reaches below its coil: the hanging neck
    /// and head; u</returns>
    public float HangDepthU => headDropU + headSize.y;

    /// <returns>lit material the king's primitives use, for other boss
    /// fight primitives to match</returns>
    public Material PrimitiveMaterial => bodyMaterial;

    /// <returns>the head's trigger collider, the source of a butt</returns>
    public Collider HeadCollider { get; private set; }

    /// <returns>if a strike (butt or bite) is under way</returns>
    public bool IsStriking => buttTimeS >= 0f;

    // Public Methods  #########################################################
    /// <summary>
    /// Builds the king coiled round the trunk at <paramref name="center"/>,
    /// its coil starting at <paramref name="coilBottomY"/> and the head
    /// hanging below it, facing <paramref name="startAngleDeg"/>.
    /// </summary>
    public void Build(Vector3 center, float coilBottomY, float startAngleDeg) {
        if (isBuilt) {
            return;
        }
        trunkCenter = new Vector3(center.x, 0f, center.z);
        this.coilBottomY = coilBottomY;
        headAngleDeg = startAngleDeg;

        colorBlock = new MaterialPropertyBlock();
        root = new GameObject("CobraKing").transform;
        root.SetParent(transform, false);

        // a kinematic body gathers every child trigger for the contact relay
        Rigidbody body = root.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        BossKingContact contact = root.gameObject.AddComponent<BossKingContact>();
        contact.Touched += () => MonkeyTouched?.Invoke();

        BuildCoil();
        BuildNeck();
        BuildHead();
        isBuilt = true;
        LayoutNeckAndHead();
    }

    /// <summary>Turns the head round the trunk toward the monkey.</summary>
    public void Track(Vector3 monkeyPosition) {
        if (!isBuilt) {
            return;
        }

        Vector3 toMonkey = monkeyPosition - trunkCenter;
        float targetDeg = Mathf.Atan2(toMonkey.z, toMonkey.x) * Mathf.Rad2Deg;
        headAngleDeg = Mathf.MoveTowardsAngle(
            headAngleDeg,
            targetDeg,
            headTurnSpeedDeg * Time.deltaTime
        );
        lookTarget = monkeyPosition;
        LayoutNeckAndHead();
        UpdateButt();
        UpdateWeakSpotGlow();
        UpdateHitFlash();
    }

    private void Update() {
        UpdateDebris();
    }

    /// <summary>
    /// Strikes the head at <paramref name="target"/>: rears back for
    /// <paramref name="windUpS"/>, dives quickly and eases back.
    /// <paramref name="onImpact"/> runs at the dive's end.
    /// </summary>
    public void StartStrike(Vector3 target, float windUpS, Action onImpact) {
        if (IsStriking || !isBuilt) {
            return;
        }
        strikeTarget = target;
        strikeWindUpS = Mathf.Max(0.05f, windUpS);
        buttTimeS = 0f;
        buttImpact = onImpact;
    }

    /// <summary>
    /// Builds a small look-alike of the king, shown eg on the trunk height
    /// marker at the fight's height: a wavy bead coil thinning to its tail,
    /// a short neck, the round head w/ its eyes & the gold crown, all at
    /// <paramref name="scale"/> of the real king; no colliders. Its face
    /// looks toward the root's -z, so under a camera-facing label it faces
    /// the player.
    /// </summary>
    /// <returns>the figure's root, w/o a parent</returns>
    public Transform BuildFigure(float scale) {
        if (colorBlock == null) {
            colorBlock = new MaterialPropertyBlock();
        }
        Transform figure = new GameObject("KingFigure").transform;
        Transform shape = new GameObject("Shape").transform;
        shape.SetParent(figure, false);
        shape.localRotation = Quaternion.Euler(0f, 180f, 0f);
        shape.localScale = Vector3.one * scale;

        // the coil lies along the top, waving & thinning to the tail
        const int beadCount = 6;
        for (int i = 0; i < beadCount; i++) {
            float t = i / (beadCount - 1f);
            float size = Mathf.Lerp(1.3f, 0.6f, t);
            CreatePart(
                PrimitiveType.Sphere, "Bead" + i, shape,
                new Vector3(
                    Mathf.Lerp(1.2f, -3.4f, t),
                    size * 0.5f + 0.3f * Mathf.Sin(t * Mathf.PI * 2f),
                    0f
                ),
                Vector3.one * size, bodyColor, false, true
            );
        }
        // the neck rises from the coil's thick end
        CreatePart(
            PrimitiveType.Sphere, "Neck0", shape, new Vector3(2f, 1.3f, 0f),
            Vector3.one * 1.1f, bodyColor, false, true
        );
        CreatePart(
            PrimitiveType.Sphere, "Neck1", shape, new Vector3(2.4f, 2.2f, 0f),
            Vector3.one * 1f, bodyColor, false, true
        );

        Transform figureHead = new GameObject("Head").transform;
        figureHead.SetParent(shape, false);
        figureHead.localPosition = new Vector3(2.5f, 3.6f, 0f);
        Vector3 skull = headSize * 0.8f;
        CreatePart(
            PrimitiveType.Sphere, "Skull", figureHead, Vector3.zero, skull,
            bodyColor, false, true
        );
        for (int side = -1; side <= 1; side += 2) {
            Vector3 eyeCenter = new Vector3(
                side * skull.x * 0.24f, skull.y * 0.1f, skull.z * 0.4f
            );
            CreatePart(
                PrimitiveType.Sphere, "Eye", figureHead, eyeCenter,
                Vector3.one * 0.7f, Color.white, false, true
            );
            CreatePart(
                PrimitiveType.Sphere, "Pupil", figureHead,
                eyeCenter + new Vector3(0f, 0f, 0.25f), Vector3.one * 0.4f,
                new Color(0.12f, 0.08f, 0.15f), false, true
            );
        }
        Transform figureCrown = new GameObject("Crown").transform;
        figureCrown.SetParent(figureHead, false);
        figureCrown.localPosition = new Vector3(0f, skull.y * 0.47f, 0f);
        CreatePart(
            PrimitiveType.Cylinder, "Band", figureCrown, Vector3.zero,
            new Vector3(1.2f, 0.14f, 1.2f), crownColor, false, true
        );
        for (int i = 0; i < 3; i++) {
            float radians = (90f + (i - 1) * 50f) * Mathf.Deg2Rad;
            CreatePart(
                PrimitiveType.Cube, "Point" + i, figureCrown,
                new Vector3(
                    Mathf.Cos(radians) * 0.48f, 0.26f,
                    Mathf.Sin(radians) * 0.48f
                ),
                new Vector3(0.2f, 0.4f, 0.2f), crownColor, false, true
            );
        }
        return figure;
    }

    /// <summary>
    /// Drops a running strike's impact: the head still eases back, but
    /// <c>onImpact</c> never runs, eg when the fight restarts.
    /// </summary>
    public void CancelStrike() {
        buttImpact = null;
    }

    /// <summary>
    /// Shatters the king into tumbling cubes and frees its crown, which is
    /// returned so it can be carried off; null if never built.
    /// </summary>
    public Transform Shatter() {
        if (!isBuilt) {
            return null;
        }
        // cubes burst from every visible part
        foreach (Renderer part in root.GetComponentsInChildren<Renderer>()) {
            if (part.transform.IsChildOf(crown)) {
                continue;
            }
            for (int i = 0; i < debrisPerPart; i++) {
                SpawnDebris(part.transform.position, part.bounds.extents.x);
            }
        }
        crown.SetParent(transform, true);
        root.gameObject.SetActive(false);
        return crown;
    }

    /// <summary>Lights or dims the weak spot.</summary>
    public void SetWeakSpotLit(bool isLit) {
        isWeakSpotLit = isLit;
    }

    /// <summary>Flashes the whole king white for a moment.</summary>
    public void FlashHit() {
        hitFlashLeftS = hitFlashS;
    }

    /// <summary>Hides the king, eg once defeated.</summary>
    public void Hide() {
        if (root != null) {
            root.gameObject.SetActive(false);
        }
    }

    // Inspector Fields  #######################################################
    [Header("Material")]
    [SerializeField]
    [Tooltip("lit material every primitive uses, tinted per part through a "
        + "property block, eg the great cobra's")]
    private Material bodyMaterial;

    [Header("Coil")]
    [SerializeField]
    [Tooltip("trunk radius the coil wraps; u")]
    private float trunkRadiusU = 5f;

    [SerializeField]
    [Min(4)]
    [Tooltip("beads in the coil round the tree top")]
    private int coilBeadCount = 34;

    [SerializeField]
    [Tooltip("loops the coil makes round the trunk")]
    private float coilTurns = 1.25f;

    [SerializeField]
    [Tooltip("height the coil rises per loop; u")]
    private float coilPitchU = 2f;

    [SerializeField]
    [Tooltip("radius of a coil bead; u")]
    private float beadRadiusU = 1.1f;


    [Header("Neck & Head")]
    [SerializeField]
    [Min(2)]
    [Tooltip("beads from the coil down to the head")]
    private int neckBeadCount = 9;

    [SerializeField]
    [Tooltip("how far the head hangs below the coil; u")]
    private float headDropU = 3.5f;

    [SerializeField]
    [Tooltip("how far out from the trunk surface the head sits; u")]
    private float headOutU = 3f;

    [SerializeField]
    [Tooltip("head size: width, height, length; round for a cute look; u")]
    private Vector3 headSize = new Vector3(2.8f, 2.4f, 2.6f);

    [SerializeField]
    [Tooltip("how fast the head turns round the trunk after the monkey; "
        + "deg/s")]
    private float headTurnSpeedDeg = 45f;

    [Header("Strike")]
    [SerializeField]
    [Tooltip("dive time down to the monkey; s")]
    private float buttDiveS = 0.15f;

    [SerializeField]
    [Tooltip("time back to rest after the dive; s")]
    private float buttReturnS = 0.5f;

    [Header("Shatter")]
    [SerializeField]
    [Min(0)]
    [Tooltip("cubes each part bursts into")]
    private int debrisPerPart = 2;

    [SerializeField]
    [Tooltip("cubes fly off at up to this speed; u/s")]
    private float debrisSpeedU = 9f;

    [SerializeField]
    [Tooltip("cubes shrink away over this; s")]
    private float debrisLifeS = 1.6f;

    [Header("Colors")]
    [SerializeField]
    private Color bodyColor = new Color(0.72f, 0.56f, 0.95f);


    [SerializeField]
    private Color crownColor = new Color(1f, 0.8f, 0.2f);

    [SerializeField]
    private Color weakSpotDimColor = new Color(0.75f, 0.2f, 0.35f);

    [SerializeField]
    private Color weakSpotLitColor = new Color(1f, 0.25f, 0.4f);

    [SerializeField]
    [Tooltip("weak spot pulses this often while lit; Hz")]
    private float weakSpotPulseHz = 2f;

    [SerializeField]
    [Tooltip("how long the white hit flash lasts; s")]
    private float hitFlashS = 0.12f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (bodyMaterial == null) {
            Debug.LogWarning(
                "BossKingBody:\tmust assign Inspector Field: bodyMaterial",
                this
            );
        }
    }

    // private methods  ########################################################
    private void BuildCoil() {
        float orbitRadius = trunkRadiusU + beadRadiusU * 0.8f;
        coilRenderers = new Renderer[coilBeadCount];
        for (int i = 0; i < coilBeadCount; i++) {
            float t = i / (float)(coilBeadCount - 1);
            float angle = headAngleDeg + t * coilTurns * 360f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 position = trunkCenter + new Vector3(
                Mathf.Cos(radians) * orbitRadius,
                coilBottomY + t * coilTurns * coilPitchU,
                Mathf.Sin(radians) * orbitRadius
            );
            // round beads with a tapering tail
            float scale = beadRadiusU * 2f * Mathf.Lerp(1f, 0.6f, t * t);
            coilRenderers[i] = CreatePart(
                PrimitiveType.Sphere, "Coil" + i, root, position,
                Vector3.one * scale, bodyColor, true
            );
        }
    }

    private void BuildNeck() {
        neckBeads = new Transform[neckBeadCount];
        neckRenderers = new Renderer[neckBeadCount];
        for (int i = 0; i < neckBeadCount; i++) {
            float scale = beadRadiusU * 2f * 0.9f;
            Renderer bead = CreatePart(
                PrimitiveType.Sphere, "Neck" + i, root, Vector3.zero,
                Vector3.one * scale, bodyColor, true
            );
            neckBeads[i] = bead.transform;
            neckRenderers[i] = bead;
        }
    }

    // the head is an unscaled holder, so its eyes & crown keep their shape;
    // its +z faces the monkey
    private void BuildHead() {
        head = new GameObject("Head").transform;
        head.SetParent(root, false);

        skullRenderer = CreatePart(
            PrimitiveType.Sphere, "Skull", head, Vector3.zero, headSize,
            bodyColor, true, true
        );
        HeadCollider = skullRenderer.GetComponent<Collider>();

        // two big eyes, each a white ball & a dark pupil
        pupils = new Transform[2];
        for (int side = -1; side <= 1; side += 2) {
            Vector3 eyeCenter = new Vector3(
                side * headSize.x * 0.24f,
                headSize.y * 0.1f,
                headSize.z * 0.4f
            );
            CreatePart(
                PrimitiveType.Sphere, side < 0 ? "EyeL" : "EyeR", head,
                eyeCenter, Vector3.one * 0.85f, Color.white, false, true
            );
            Renderer pupil = CreatePart(
                PrimitiveType.Sphere, side < 0 ? "PupilL" : "PupilR", head,
                eyeCenter + new Vector3(0f, 0f, 0.3f), Vector3.one * 0.48f,
                new Color(0.12f, 0.08f, 0.15f), false, true
            );
            pupils[(side + 1) / 2] = pupil.transform;
        }

        // small crown: a gold band, 3 points & the jewel weak spot in front
        crown = new GameObject("Crown").transform;
        crown.SetParent(head, false);
        crown.localPosition = new Vector3(0f, headSize.y * 0.47f, -0.05f);
        CreatePart(
            PrimitiveType.Cylinder, "Band", crown, Vector3.zero,
            new Vector3(1.4f, 0.16f, 1.4f), crownColor, false, true
        );
        for (int i = 0; i < 3; i++) {
            float radians = (90f + (i - 1) * 50f) * Mathf.Deg2Rad;
            CreatePart(
                PrimitiveType.Cube, "Point" + i, crown,
                new Vector3(
                    Mathf.Cos(radians) * 0.55f,
                    0.3f,
                    Mathf.Sin(radians) * 0.55f
                ),
                new Vector3(0.24f, 0.45f, 0.24f), crownColor, false, true
            );
        }
        // the spot thrown bananas fly at, on the crown's front; kept invisible
        weakSpot = CreatePart(
            PrimitiveType.Sphere, "Target", crown,
            new Vector3(0f, 0.12f, 0.72f), Vector3.one * 0.55f,
            weakSpotDimColor, false, true
        );
        weakSpot.enabled = false;
    }

    // the neck runs from the coil's bottom bead at the head's angle down to
    // the head, which hangs out from the trunk & looks at the monkey
    private void LayoutNeckAndHead() {
        float radians = headAngleDeg * Mathf.Deg2Rad;
        Vector3 outward = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        float coilRadius = trunkRadiusU + beadRadiusU * 0.8f;
        Vector3 start = trunkCenter + outward * coilRadius
            + Vector3.up * coilBottomY;
        restHeadPosition = trunkCenter + outward * (trunkRadiusU + headOutU)
            + Vector3.up * (coilBottomY - headDropU);

        Vector3 end = restHeadPosition + ButtOffset();
        Vector3 control = start + outward * headOutU * 1.2f;
        for (int i = 0; i < neckBeads.Length; i++) {
            float t = (i + 1f) / (neckBeads.Length + 1f);
            neckBeads[i].position = Bezier(start, control, end, t);
        }

        head.position = end;
        Vector3 look = lookTarget - end;
        if (look.sqrMagnitude < 0.01f) {
            look = outward;
        }
        // face the monkey, but keep the crown up
        Vector3 faceDirection = Vector3.Lerp(outward, look.normalized, 0.6f);
        head.rotation = Quaternion.LookRotation(faceDirection, Vector3.up);

        // pupils peek toward the monkey
        if (pupils != null) {
            Vector3 localLook = head.InverseTransformDirection(look.normalized);
            for (int i = 0; i < pupils.Length; i++) {
                float side = i == 0 ? -1f : 1f;
                pupils[i].localPosition = new Vector3(
                    side * headSize.x * 0.24f + localLook.x * 0.12f,
                    headSize.y * 0.12f + localLook.y * 0.12f,
                    headSize.z * 0.4f + 0.3f
                );
            }
        }
    }

    // rear back during the wind-up, dive at the monkey, ease back
    private Vector3 ButtOffset() {
        if (buttTimeS < 0f) {
            return Vector3.zero;
        }
        Vector3 dive = strikeTarget - restHeadPosition;
        Vector3 rearBack = -dive.normalized * 1.2f;
        if (buttTimeS < strikeWindUpS) {
            float t = buttTimeS / strikeWindUpS;
            return rearBack * Mathf.Sin(t * Mathf.PI * 0.5f);
        }
        float diveTime = buttTimeS - strikeWindUpS;
        if (diveTime < buttDiveS) {
            return Vector3.Lerp(rearBack, dive, diveTime / buttDiveS);
        }
        float back = (diveTime - buttDiveS) / Mathf.Max(0.01f, buttReturnS);
        return Vector3.Lerp(dive, Vector3.zero, back);
    }

    private void UpdateButt() {
        if (buttTimeS < 0f) {
            return;
        }
        float before = buttTimeS;
        buttTimeS += Time.deltaTime;
        float impactTime = strikeWindUpS + buttDiveS;
        if (before < impactTime && buttTimeS >= impactTime) {
            buttImpact?.Invoke();
            buttImpact = null;
        }
        if (buttTimeS >= impactTime + buttReturnS) {
            buttTimeS = -1f;
        }
    }

    private void UpdateWeakSpotGlow() {
        if (weakSpot == null || !weakSpot.enabled) {
            return;
        }
        Color color = weakSpotDimColor;
        float scale = 0.55f;
        if (isWeakSpotLit) {
            float pulse = 0.5f + 0.5f * Mathf.Sin(
                Time.time * weakSpotPulseHz * 2f * Mathf.PI
            );
            color = Color.Lerp(weakSpotLitColor * 0.7f, weakSpotLitColor, pulse);
            scale = Mathf.Lerp(0.55f, 0.72f, pulse);
        }
        weakSpot.transform.localScale = Vector3.one * scale;
        if (hitFlashLeftS <= 0f) {
            SetColor(weakSpot, color);
        }
    }

    private void UpdateHitFlash() {
        if (hitFlashLeftS <= 0f) {
            return;
        }
        hitFlashLeftS -= Time.deltaTime;
        bool isFlashing = hitFlashLeftS > 0f;
        Color body = isFlashing ? Color.white : bodyColor;
        foreach (Renderer part in coilRenderers) {
            SetColor(part, body);
        }
        foreach (Renderer part in neckRenderers) {
            SetColor(part, body);
        }
        SetColor(skullRenderer, isFlashing ? Color.white : bodyColor);
        SetColor(weakSpot, isFlashing ? Color.white : weakSpotDimColor);
    }

    private void SpawnDebris(Vector3 position, float spreadU) {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Debris";
        Destroy(cube.GetComponent<Collider>());
        cube.transform.SetParent(transform, true);
        cube.transform.position = position
            + UnityEngine.Random.insideUnitSphere * spreadU;
        cube.transform.rotation = UnityEngine.Random.rotation;
        float size = UnityEngine.Random.Range(0.3f, 0.6f);
        cube.transform.localScale = Vector3.one * size;
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (bodyMaterial != null) {
            cubeRenderer.sharedMaterial = bodyMaterial;
        }
        SetColor(cubeRenderer, bodyColor);
        Vector3 velocity = UnityEngine.Random.insideUnitSphere * debrisSpeedU;
        velocity.y = Mathf.Abs(velocity.y) + debrisSpeedU * 0.4f;
        debris.Add(new Debris {
            cube = cube.transform,
            velocity = velocity,
            size = size,
            ageS = 0f
        });
    }

    // cubes fall, tumble & shrink away
    private void UpdateDebris() {
        for (int i = debris.Count - 1; i >= 0; i--) {
            Debris piece = debris[i];
            piece.ageS += Time.deltaTime;
            if (piece.ageS >= debrisLifeS || piece.cube == null) {
                if (piece.cube != null) {
                    Destroy(piece.cube.gameObject);
                }
                debris.RemoveAt(i);
                continue;
            }
            piece.velocity += Physics.gravity * Time.deltaTime;
            piece.cube.position += piece.velocity * Time.deltaTime;
            piece.cube.Rotate(piece.velocity, 360f * Time.deltaTime);
            piece.cube.localScale = Vector3.one * piece.size
                * (1f - piece.ageS / debrisLifeS);
            debris[i] = piece;
        }
    }

    private Renderer CreatePart(
        PrimitiveType type,
        string partName,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Color color,
        bool isSolid,
        bool isLocal = false
    ) {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent, false);
        if (isLocal) {
            part.transform.localPosition = position;
        } else {
            part.transform.position = position;
        }
        part.transform.localScale = scale;

        Collider partCollider = part.GetComponent<Collider>();
        if (isSolid) {
            // a trigger: contact reports to the root's relay, never blocks
            partCollider.isTrigger = true;
        } else if (partCollider != null) {
            Destroy(partCollider);
        }

        Renderer partRenderer = part.GetComponent<Renderer>();
        if (bodyMaterial != null) {
            partRenderer.sharedMaterial = bodyMaterial;
        }
        SetColor(partRenderer, color);
        return partRenderer;
    }

    private void SetColor(Renderer target, Color color) {
        if (target == null) {
            return;
        }
        target.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BASE_COLOR, color);
        colorBlock.SetColor(COLOR, color);
        target.SetPropertyBlock(colorBlock);
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t) {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    // private members  ########################################################
    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR = Shader.PropertyToID("_Color");

    private bool isBuilt;
    private Vector3 trunkCenter; // trunk axis, y zeroed
    private float coilBottomY; // world y the coil starts at
    private float headAngleDeg; // angle round the trunk the head hangs at
    private Vector3 restHeadPosition; // head position without a butt
    private Vector3 lookTarget; // world point the head looks at
    private bool isWeakSpotLit;
    private float hitFlashLeftS;
    private float buttTimeS = -1f; // time into the running strike, -1 if none
    private Action buttImpact;
    private Vector3 strikeTarget; // world point the running strike dives at
    private float strikeWindUpS; // rear-back time of the running strike

    private struct Debris {
        public Transform cube;
        public Vector3 velocity;
        public float size;
        public float ageS;
    }

    private readonly System.Collections.Generic.List<Debris> debris =
        new System.Collections.Generic.List<Debris>();

    // cached references  ------------------------------------------------------
    private MaterialPropertyBlock colorBlock;
    private Transform root;
    private Transform head;
    private Transform[] neckBeads;
    private Transform[] pupils;
    private Transform crown;
    private Renderer[] coilRenderers;
    private Renderer[] neckRenderers;
    private Renderer skullRenderer;
    private Renderer weakSpot;
}
