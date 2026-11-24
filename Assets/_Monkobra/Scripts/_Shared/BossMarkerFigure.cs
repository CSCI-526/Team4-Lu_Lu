using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A small living look-alike of a boss on its trunk height marker, holding
/// the marker's plate: the Cobra King coils round the plate, slithering, its
/// crowned head swaying over its corner; the Spider King sits on the plate's
/// top edge, its legs gripping the face & sides, flexing, one front leg
/// tapping. Built from primitives in the label's space; the marker system
/// tells it the plate's center & size each frame (<see cref="Fit"/>).
/// </summary>
public class BossMarkerFigure: MonoBehaviour {
    /// <summary>which boss the figure shows</summary>
    public enum Kind {
        Cobra,
        Spider,
    }

    // Public Methods  #########################################################
    /// <summary>
    /// Builds a figure of <paramref name="kind"/> in the boss body's material
    /// & colors; it lies hidden till fitted to a plate.
    /// </summary>
    /// <param name="material">the boss body's lit material</param>
    /// <param name="bodyColor">its body</param>
    /// <param name="accentColor">the cobra's crown, the spider's red
    /// mark</param>
    /// <param name="eyeColor">the spider's glowing eyes; unused by the cobra
    /// </param>
    public static Transform Create(
        Kind kind,
        Material material,
        Color bodyColor,
        Color accentColor,
        Color eyeColor
    ) {
        GameObject root = new GameObject(kind + "MarkerFigure");
        BossMarkerFigure figure = root.AddComponent<BossMarkerFigure>();
        figure.kind = kind;
        figure.material = material;
        figure.bodyColor = bodyColor;
        figure.accentColor = accentColor;
        figure.eyeColor = eyeColor;
        figure.Build();
        return root.transform;
    }

    /// <summary>
    /// Fits the figure to the plate it holds, both in the label's space.
    /// </summary>
    public void Fit(Vector3 plateCenter, Vector2 plateSize) {
        center = plateCenter;
        size = plateSize;
        isFitted = true;
        Pose(Time.time);
    }

    // MonoBehaviour Lifecycle  ################################################
    private void LateUpdate() {
        if (isFitted) {
            Pose(Time.time);
        }
    }

    // private members  ########################################################
    private const int COIL_BEADS = 22;
    private const float COIL_TURNS = 1.5f; // round each end of the plate
    private const float END_SHARE = 0.3f; // of the coil, round each end
    private const int LEG_COUNT = 8;

    private Kind kind;
    private Material material;
    private Color bodyColor;
    private Color accentColor;
    private Color eyeColor;
    private Vector3 center;
    private Vector2 size = new Vector2(4f, 1.6f);
    private bool isFitted;

    private Transform[] beads; // the cobra's coil, tail first
    private Transform head; // the cobra's head or the spider's body
    private Transform abdomen; // the spider's
    private Transform[] upperLegs; // the spider's, hip to knee
    private Transform[] lowerLegs; // knee to foot
    private MaterialPropertyBlock colorBlock;

    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR = Shader.PropertyToID("_Color");

    // private methods  ########################################################
    private void Build() {
        colorBlock = new MaterialPropertyBlock();
        if (kind == Kind.Cobra) {
            BuildCobra();
        } else {
            BuildSpider();
        }
    }

    private void BuildCobra() {
        beads = new Transform[COIL_BEADS];
        for (int i = 0; i < COIL_BEADS; i++) {
            beads[i] = Part(PrimitiveType.Sphere, "Bead", transform, bodyColor);
        }
        head = new GameObject("Head").transform;
        head.SetParent(transform, false);
        Transform skull = Part(PrimitiveType.Sphere, "Skull", head, bodyColor);
        skull.localScale = new Vector3(1f, 0.85f, 0.9f);
        for (int side = -1; side <= 1; side += 2) {
            Transform eye =
                Part(PrimitiveType.Sphere, "Eye", head, Color.white);
            eye.localPosition = new Vector3(side * 0.26f, 0.12f, -0.36f);
            eye.localScale = Vector3.one * 0.32f;
            Transform pupil = Part(PrimitiveType.Sphere, "Pupil", head,
                new Color(0.12f, 0.08f, 0.15f));
            pupil.localPosition = new Vector3(side * 0.26f, 0.12f, -0.48f);
            pupil.localScale = Vector3.one * 0.17f;
        }
        Transform band = Part(PrimitiveType.Cylinder, "Crown", head,
            accentColor);
        band.localPosition = new Vector3(0f, 0.45f, 0f);
        band.localScale = new Vector3(0.6f, 0.07f, 0.6f);
        for (int i = 0; i < 3; i++) {
            Transform point = Part(PrimitiveType.Cube, "Point", head,
                accentColor);
            point.localPosition = new Vector3((i - 1) * 0.2f, 0.62f, -0.05f);
            point.localScale = new Vector3(0.1f, 0.22f, 0.1f);
        }
    }

    private void BuildSpider() {
        head = Part(PrimitiveType.Sphere, "Body", transform, bodyColor);
        abdomen = Part(PrimitiveType.Sphere, "Abdomen", transform, bodyColor);
        Transform mark = Part(PrimitiveType.Sphere, "Mark", abdomen,
            accentColor);
        mark.localPosition = new Vector3(0f, 0.3f, 0.05f);
        mark.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        for (int side = -1; side <= 1; side += 2) {
            Transform eye = Part(PrimitiveType.Sphere, "Eye", head, eyeColor);
            eye.localPosition = new Vector3(side * 0.2f, 0.1f, -0.42f);
            eye.localScale = Vector3.one * 0.22f;
        }
        upperLegs = new Transform[LEG_COUNT];
        lowerLegs = new Transform[LEG_COUNT];
        for (int i = 0; i < LEG_COUNT; i++) {
            upperLegs[i] = Part(PrimitiveType.Cylinder, "Leg", transform,
                bodyColor);
            lowerLegs[i] = Part(PrimitiveType.Cylinder, "Shin", transform,
                bodyColor);
        }
    }

    private Transform Part(PrimitiveType type, string partName,
        Transform parent, Color color) {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        Renderer partRenderer = part.GetComponent<Renderer>();
        if (material != null) {
            partRenderer.sharedMaterial = material;
        }
        partRenderer.shadowCastingMode = ShadowCastingMode.Off;
        partRenderer.receiveShadows = false;
        colorBlock.SetColor(BASE_COLOR, color);
        colorBlock.SetColor(COLOR, color);
        partRenderer.SetPropertyBlock(colorBlock);
        return part.transform;
    }

    private void Pose(float time) {
        if (kind == Kind.Cobra) {
            PoseCobra(time);
        } else {
            PoseSpider(time);
        }
    }

    // coiled round both ends of the plate & draped along its top edge, so
    // the number stays clear, slithering along; the head rises over the
    // right top corner & sways
    private void PoseCobra(float time) {
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;
        float thick = size.y * 0.24f;
        float slither = time * 1.6f;
        for (int i = 0; i < COIL_BEADS; i++) {
            float s = i / (COIL_BEADS - 1f);
            Vector3 at;
            if (s < END_SHARE || s > 1f - END_SHARE) {
                // a coil round one end of the plate, front & back, so the
                // number in the middle stays clear
                bool isLeft = s < END_SHARE;
                float u = isLeft ? s / END_SHARE
                    : (s - (1f - END_SHARE)) / END_SHARE;
                float angle = u * COIL_TURNS * Mathf.PI * 2f
                    + (isLeft ? slither : -slither);
                float edge = isLeft ? -halfW : halfW;
                float x = edge + (isLeft ? 1f : -1f) * thick
                    * Mathf.Lerp(-0.4f, 0.9f, u);
                at = new Vector3(
                    x,
                    Mathf.Cos(angle) * (halfH + thick * 0.35f),
                    -Mathf.Sin(angle) * thick * 0.9f
                );
            } else {
                // draped along the top edge b/t the two coils, rippling
                float u = (s - END_SHARE) / (1f - 2f * END_SHARE);
                at = new Vector3(
                    Mathf.Lerp(-halfW + thick, halfW - thick, u),
                    halfH + thick * (0.45f + 0.18f
                        * Mathf.Sin(u * Mathf.PI * 4f - slither * 2f)),
                    -thick * 0.2f
                );
            }
            beads[i].localPosition = center + at;
            beads[i].localScale = Vector3.one * thick
                * Mathf.Lerp(0.55f, 1.05f, s);
        }
        float sway = Mathf.Sin(time * 2.2f);
        float headSize = size.y * 0.42f;
        head.localPosition = center + new Vector3(
            halfW + headSize * 0.45f + sway * headSize * 0.15f,
            halfH + headSize * (0.95f + 0.08f * Mathf.Sin(time * 3.1f)),
            -thick
        );
        head.localRotation = Quaternion.Euler(0f, 0f, sway * 12f);
        head.localScale = Vector3.one * headSize;
    }

    // perched on the top edge's middle: 4 front legs over the face, 4 hooking
    // the sides; the body breathes, the legs flex in turn & one front leg
    // taps the plate now & then
    private void PoseSpider(float time) {
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;
        float bodySize = size.y * 0.42f;
        float breathe = 0.04f * Mathf.Sin(time * 3f);
        Vector3 body = center + new Vector3(0f, halfH + bodySize * 0.45f,
            -bodySize * 0.25f);
        head.localPosition = body + new Vector3(0f, breathe * bodySize, 0f);
        head.localScale = Vector3.one * bodySize * (1f + breathe);
        abdomen.localPosition = body + new Vector3(0f,
            bodySize * 0.45f, bodySize * 0.55f);
        abdomen.localScale = new Vector3(1.25f, 1.05f, 1.25f)
            * bodySize * (1f + breathe * 1.5f);
        float legThick = bodySize * 0.12f;
        // a tap every 1.6 s by the first front leg
        float tapT = Mathf.Repeat(time, 1.6f);
        float tap = tapT < 0.3f ? Mathf.Sin(tapT / 0.3f * Mathf.PI) : 0f;
        for (int i = 0; i < LEG_COUNT; i++) {
            float side = i < LEG_COUNT / 2 ? -1f : 1f;
            int k = i % (LEG_COUNT / 2); // 0, 1 front; 2, 3 back
            Vector3 hip = body + new Vector3(side * bodySize * 0.35f,
                0f, (k - 1.5f) * bodySize * 0.12f);
            Vector3 foot;
            if (k < 2) {
                // over the top edge, down the face toward the camera
                foot = center + new Vector3(
                    side * (bodySize * 0.55f + k * halfW * 0.35f),
                    halfH - size.y * (0.12f + 0.08f * k),
                    -size.y * 0.12f
                );
            } else {
                // hooked round a side edge
                foot = center + new Vector3(
                    side * (halfW + legThick),
                    halfH - size.y * (0.35f + 0.3f * (k - 2)),
                    0f
                );
            }
            float flex = Mathf.Sin(time * 4f + i * 0.9f);
            bool isTapper = i == LEG_COUNT / 2;
            if (isTapper) {
                foot += new Vector3(0f, tap * size.y * 0.25f,
                    -tap * size.y * 0.15f);
            }
            Vector3 mid = (hip + foot) * 0.5f;
            Vector3 knee = mid + new Vector3(side * bodySize * 0.35f,
                bodySize * (0.55f + 0.08f * flex), -bodySize * 0.1f);
            SetLine(upperLegs[i], hip, knee, legThick);
            SetLine(lowerLegs[i], knee, foot, legThick * 0.8f);
        }
    }

    // a cylinder from one point to another, in this figure's space
    private static void SetLine(Transform line, Vector3 from, Vector3 to,
        float thickness) {
        Vector3 span = to - from;
        if (span.sqrMagnitude < 0.000001f) {
            return;
        }
        line.localPosition = (from + to) * 0.5f;
        line.localRotation = Quaternion.FromToRotation(Vector3.up,
            span.normalized);
        line.localScale = new Vector3(thickness, span.magnitude * 0.5f,
            thickness);
    }
}
