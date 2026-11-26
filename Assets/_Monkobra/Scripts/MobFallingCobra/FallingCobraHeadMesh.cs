using System.Collections.Generic;
using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>Builds the rigid head and the animated body mesh a falling cobra
/// shows.</summary>
public sealed class FallingCobraHeadMesh {
    // private members  ########################################################
    private readonly FallingCobraRig rig;
    private readonly FallingCobraConfig config;

    // directions the head's extreme points are taken in
    private const int SUPPORT_DIRECTIONS = 512;

    // the random directions that check the support points, and their seed
    private const int SUPPORT_CHECKS = 1000;
    private const int SUPPORT_CHECK_SEED = 7;

    public FallingCobraHeadMesh(FallingCobraRig rig) {
        this.rig = rig;
        config = rig.config;
    }

    private static readonly Dictionary<(Mesh, float, Vector3, float),
        HeadTemplate> headTemplates = new();

    // Public Methods  #########################################################
    public void BuildCartoonVisual() {
        if (!config.CartoonAppearance.enabled) {
            return;
        }
        List<Transform> segments = new List<Transform>();
        foreach (Transform child in rig.host) {
            if (child.name.StartsWith("Segment_")) {
                segments.Add(child);
            }
        }
        segments.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (segments.Count < 2
            || !segments[0].TryGetComponent(out Renderer source)) {
            return;
        }
        GameObject reference = config.CartoonAppearance.referenceBody;
        if (reference != null && config.BodyPrefab == null) {
            Debug.LogWarning("FallingCobraHeadMesh:\tno skinned body, run "
                + "Monkobra > Build Falling Cobra Body", rig.host);
        }
        if (reference != null && config.BodyPrefab != null
            && reference.TryGetComponent(out MeshFilter headSource)) {
            foreach (Transform segment in segments) {
                if (segment.TryGetComponent(out Renderer renderer)) {
                    renderer.enabled = false;
                }
            }
            GameObject visual = Object.Instantiate(config.BodyPrefab,
                rig.host);
            visual.name = "CartoonSnakeBody";
            BuildFallingReference(headSource.sharedMesh, visual);
            return;
        }
        rig.cartoonVisual =
            new CartoonCobraVisual(rig.host, source.sharedMaterial,
                config.CartoonAppearance);
        Transform head = new GameObject("CartoonHeadAnchor").transform;
        head.SetParent(rig.host, false);
        head.localPosition = segments[0].localPosition;
        head.localRotation = Quaternion.Euler(0f, 0f, 180f);
        rig.cartoonVisual.BuildHead(head, config.HitRadiusU * 0.78f);
        Vector3[] centers = new Vector3[segments.Count];
        Vector3[] outward = new Vector3[segments.Count];
        float[] radii = new float[segments.Count];
        for (int i = 0; i < segments.Count; i++) {
            float t = i / (float)(segments.Count - 1);
            Vector3 local = segments[i].localPosition;
            local.x += Mathf.Sin(t * Mathf.PI * 3f) * config.HitRadiusU * 0.38f;
            centers[i] = rig.host.TransformPoint(local);
            outward[i] = -rig.host.forward;
            radii[i] = config.HitRadiusU * 0.37f * (1f - t * t);
            if (segments[i].TryGetComponent(out Renderer renderer)) {
                renderer.enabled = false;
            }
        }
        rig.cartoonVisual.UpdateBody(centers, radii, outward);
    }

    public struct HeadPoint {
        public Vector3 point, normal;
        public Vector2 uv;
        public static HeadPoint Lerp(HeadPoint a, HeadPoint b, float t) =>
            new HeadPoint {
            point = Vector3.Lerp(a.point, b.point, t),
                normal = Vector3.Lerp(a.normal, b.normal, t).normalized,
            uv = Vector2.Lerp(a.uv, b.uv, t)
        }
        ;
    }

    public sealed class HeadTemplate {
        public Mesh mesh;
        public Vector3[] vertices, rim, rimNormals;
        // the extreme vertices of the head, in about SUPPORT_DIRECTIONS
        // directions; FitHeadToBark scans these, not every vertex
        public Vector3[] supportPoints;
        public Vector2[] rimUV;
        public Bounds bounds;
        public float front;
        public Quaternion faceBasis;
        public Vector3 faceCenter;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetHeadTemplates() {
        ReleaseHeadTemplates();
        Application.quitting -= ReleaseHeadTemplates;
        Application.quitting += ReleaseHeadTemplates;
    }

    public static void ReleaseHeadTemplates() {
        foreach (HeadTemplate template in headTemplates.Values) {
            if (template.mesh != null) {
                Object.Destroy(template.mesh);
            }
        }
        headTemplates.Clear();
    }

    // Original high-density head stays rigid and is prepared only once per
    // source/size.
    public HeadTemplate CreateHeadTemplate(Mesh source, float size) {
        Vector3[] points = source.vertices, normals = source.normals;
        Vector2[] uv = source.uv;
        int[] sourceTriangles = source.triangles;
        Vector3 headAnchor = Vector3.zero, tailAnchor = Vector3.zero;
        int heads = 0, tails = 0;
        foreach (Vector3 point in points) {
            if (point.y<source.bounds.min.y + source.bounds.size.y * .3f) {
                headAnchor += point;
                heads++;
            }
            if (point.y>source.bounds.max.y - source.bounds.size.y * .2f) {
                tailAnchor += point;
                tails++;
            }
        }
        headAnchor /= Mathf.Max(1, heads);
        tailAnchor /= Mathf.Max(1, tails);
        Vector3 sourceAxis = (tailAnchor - headAnchor).normalized;
        float cut =
            Vector3.Dot(headAnchor + sourceAxis
                * Vector3.Distance(headAnchor, tailAnchor) * .32f, sourceAxis);
        List<HeadPoint> head = new List<HeadPoint>();
        List<HeadPoint> rim = new List<HeadPoint>();
        for (int index = 0; index < sourceTriangles.Length; index += 3) {
            HeadPoint[] triangle = new HeadPoint[3];
            for (int corner = 0; corner < 3; corner++) {
                int vertex = sourceTriangles[index + corner];
                triangle[corner] =
                    new HeadPoint {point = points[vertex],
                        normal = normals[vertex], uv = uv[vertex]};
            }
            List<HeadPoint> polygon = new List<HeadPoint>(4);
            for (int corner = 0; corner < 3; corner++) {
                HeadPoint a = triangle[corner], b = triangle[(corner + 1)%3];
                float distanceA = Vector3.Dot(a.point, sourceAxis),
                    distanceB = Vector3.Dot(b.point, sourceAxis);
                bool keepA = distanceA <= cut, keepB = distanceB <= cut;
                if (keepA) {
                    polygon.Add(a);
                }
                if (keepA != keepB) {
                    HeadPoint crossing =
                        HeadPoint.Lerp(a, b,
                            (cut - distanceA) / (distanceB - distanceA));
                    polygon.Add(crossing);
                    rim.Add(crossing);
                }
            }
            for (int corner = 1; corner + 1<polygon.Count; corner++) {
                head.Add(polygon[0]);
                head.Add(polygon[corner]);
                head.Add(polygon[corner + 1]);
            }
        }
        Bounds rimBounds = new Bounds(rim[0].point, Vector3.zero);
        foreach (var vertex in rim) {
            rimBounds.Encapsulate(vertex.point);
        }
        Vector3 pivot = rimBounds.center;
        pivot += sourceAxis * (cut - Vector3.Dot(pivot, sourceAxis));
        Quaternion align = Quaternion.FromToRotation(sourceAxis, Vector3.up);
        Vector3 face =
            Vector3.ProjectOnPlane(align * config.ReferenceFaceDirection,
                Vector3.up).normalized;
        Quaternion yaw =
            Quaternion.FromToRotation(face, Vector3.forward) * align;
        Bounds localRimBounds =
            new Bounds(yaw * (rim[0].point - pivot) * size, Vector3.zero);
        foreach (var vertex in rim) {
            localRimBounds.Encapsulate(yaw * (vertex.point - pivot) * size);
        }
        Vector3 localCenter =
            new Vector3(localRimBounds.center.x, 0f, localRimBounds.center.z);
        pivot += Quaternion.Inverse(yaw) * localCenter / size;

        int count = head.Count;
        HeadTemplate template = new HeadTemplate {
            vertices = new Vector3[count], rim = new Vector3[BodyStride],
            rimUV = new Vector2[BodyStride],
                rimNormals = new Vector3[BodyStride],
            bounds = new Bounds(Vector3.zero, Vector3.zero)
        }
        ;
        Vector3[] headNormals = new Vector3[count];
        Vector2[] coordinates = new Vector2[count];
        Color[] colors = new Color[count];
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) {
            Vector3 vertex = yaw * (head[i].point - pivot) * size;
            Vector3 normal = yaw * head[i].normal;
            float collarT = Mathf.InverseLerp(-.4f, 0f, vertex.y);
            float collarScale =
                Mathf.Lerp(1f, config.NeckWaistScale,
                    Mathf.SmoothStep(0f, 1f, collarT));
            float derivative =
                (config.NeckWaistScale - 1f) * 6f * collarT * (1f - collarT)
                    / .4f;
            normal = new Vector3(normal.x / collarScale,
                normal.y - derivative
                    * (vertex.x * normal.x + vertex.z * normal.z) / collarScale,
                        normal.z / collarScale).normalized;
            float lengthT = Mathf.InverseLerp(-.6f, 0f, vertex.y);
            float lengthDerivative =
                1f - .2f * 6f * lengthT * (1f - lengthT) / .6f;
            normal.y /= lengthDerivative;
            normal.Normalize();
            vertex.y += .2f * (1f - Mathf.SmoothStep(0f, 1f, lengthT));
            vertex.x *= collarScale;
            vertex.z *= collarScale;
            template.vertices[i] = vertex;
            Vector3 collarNormal =
                new Vector3(vertex.x, 0f, vertex.z).normalized;
            headNormals[i] =
                Vector3.Slerp(normal, collarNormal,
                    Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(-HeadCollarBlendU, 0f,
                            vertex.y))).normalized;
            coordinates[i] = head[i].uv;
            float neckBlend =
                Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(-HeadCollarBlendU, 0f, vertex.y));
            colors[i] =
                new Color(0f, neckBlend, collarNormal.x * .5f + .5f,
                    collarNormal.z * .5f + .5f);
            indices[i] = i;
            template.front = Mathf.Max(template.front, vertex.z);
            template.bounds.Encapsulate(vertex);
        }
        template.faceBasis =
            Quaternion.LookRotation(yaw * config.ReferenceFaceDirection,
                yaw * Vector3.up);
        template.faceCenter = template.bounds.center;
        // Intersect rays with the actual clipped boundary. The first body ring
        // fits the original neck.
        for (int side = 0; side <= BodySides; side++) {
            float angle = (side % BodySides) * Mathf.PI * 2f / BodySides;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float radius = 0f;
            Vector2 rimUV = rim[0].uv;
            Vector3 rimNormal = yaw * rim[0].normal;
            for (int edge = 0; edge + 1 < rim.Count; edge += 2) {
                Vector3 a3 = yaw * (rim[edge].point - pivot) * size;
                Vector3 b3 = yaw * (rim[edge + 1].point - pivot) * size;
                Vector2 a = new Vector2(a3.x, a3.z),
                    delta = new Vector2(b3.x - a3.x, b3.z - a3.z);
                float cross = direction.x * delta.y - direction.y * delta.x;
                if (Mathf.Abs(cross) < .000001f) {
                    continue;
                }
                float alongRay = (a.x * delta.y - a.y * delta.x) / cross;
                float alongEdge =
                    (a.x * direction.y - a.y * direction.x) / cross;
                if (alongRay > radius && alongEdge >= -.001f
                    && alongEdge <= 1.001f) {
                    radius = alongRay;
                    rimUV = Vector2.Lerp(rim[edge].uv, rim[edge + 1].uv,
                        Mathf.Clamp01(alongEdge));
                    rimNormal =
                        (yaw * Vector3.Lerp(rim[edge].normal,
                            rim[edge + 1].normal,
                                Mathf.Clamp01(alongEdge))).normalized;
                }
            }
            if (radius < .001f) {
                radius = Mathf.Min(localRimBounds.extents.x,
                    localRimBounds.extents.z);
            }
            template.rim[side] =
                new Vector3(direction.x, 0f, direction.y) * radius
                    * config.NeckWaistScale;
            template.rimUV[side] = rimUV;
            template.rimNormals[side] =
                new Vector3(direction.x, 0f, direction.y);
        }
        template.supportPoints = BuildSupportPoints(template.vertices);
        template.mesh = new Mesh {
            name = "Original rigid snake head", hideFlags = HideFlags.DontSave,
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = template.vertices, normals = headNormals,
                uv = coordinates,
            colors = colors, triangles = indices, bounds = template.bounds
        }
        ;
        template.mesh.UploadMeshData(true);
        return template;
    }

    // for each of SUPPORT_DIRECTIONS evenly spread directions, the head vertex
    // farthest against it; a vertex wanted by several directions counts once.
    // The least dot product of these points with any direction is the head's
    // support depth there, to within the spread of the directions
    private static Vector3[] BuildSupportPoints(Vector3[] vertices) {
        List<Vector3> points = new List<Vector3>();
        HashSet<int> chosen = new HashSet<int>();
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (int k = 0; k < SUPPORT_DIRECTIONS; k++) {
            float y = 1f - 2f * (k + .5f) / SUPPORT_DIRECTIONS;
            float ring = Mathf.Sqrt(1f - y * y);
            Vector3 direction =
                new Vector3(Mathf.Cos(goldenAngle * k) * ring, y,
                    Mathf.Sin(goldenAngle * k) * ring);
            int best = 0;
            float bestDot = float.PositiveInfinity;
            for (int i = 0; i < vertices.Length; i++) {
                float dot = Vector3.Dot(vertices[i], direction);
                if (dot < bestDot) {
                    bestDot = dot;
                    best = i;
                }
            }
            if (chosen.Add(best)) {
                points.Add(vertices[best]);
            }
        }
        if (Debug.isDebugBuild) {
            Debug.Log("FallingCobraHeadMesh:	head support "
                + points.Count + " points of " + vertices.Length
                + " vertices, worst depth error "
                + WorstSupportError(vertices, points) + " u");
        }
        return points.ToArray();
    }

    // largest gap between the support depth of all vertices and of the
    // support points, over fixed random directions; u
    private static float WorstSupportError(Vector3[] vertices,
        List<Vector3> points) {
        System.Random random = new System.Random(SUPPORT_CHECK_SEED);
        float worst = 0f;
        for (int k = 0; k < SUPPORT_CHECKS; k++) {
            Vector3 direction =
                new Vector3((float)random.NextDouble() - .5f,
                    (float)random.NextDouble() - .5f,
                    (float)random.NextDouble() - .5f).normalized;
            float all = float.PositiveInfinity, few = float.PositiveInfinity;
            foreach (Vector3 vertex in vertices) {
                all = Mathf.Min(all, Vector3.Dot(vertex, direction));
            }
            foreach (Vector3 point in points) {
                few = Mathf.Min(few, Vector3.Dot(point, direction));
            }
            worst = Mathf.Max(worst, few - all);
        }
        return worst;
    }

    // the rigid head from the source mesh, and the baked skinned body the
    // spine poses each frame
    public void BuildFallingReference(Mesh source, GameObject body) {
        rig.swimPhaseOffset =
            (float)new System.Random(rig.host.GetInstanceID()).NextDouble()
                * Mathf.PI * 2f;
        if (!body.TryGetComponent(out SkinnedMeshRenderer skinned)
            || skinned.bones.Length != BodyRings) {
            Debug.LogError("FallingCobraHeadMesh:\tbody prefab needs a "
                + "SkinnedMeshRenderer of " + BodyRings + " bones, run "
                + "Monkobra > Build Falling Cobra Body", rig.host);
            return;
        }
        float size =
            config.HitRadiusU * 2f * config.ReferenceSizeMultiplier
                / Mathf.Max(source.bounds.size.x, source.bounds.size.z);
        (Mesh, float, Vector3, float) key =
            (source, size, config.ReferenceFaceDirection,
                config.NeckWaistScale);
        if (!headTemplates.TryGetValue(key, out HeadTemplate template)) {
            template = CreateHeadTemplate(source, size);
            headTemplates.Add(key, template);
        }
        rig.headSupportPoints = template.supportPoints;
        rig.originalHeadFront = template.front;
        rig.originalHeadFaceBasis = template.faceBasis;
        rig.originalHeadFaceCenter = template.faceCenter;
        rig.neckRim = template.rim;
        rig.originalHeadBounds = template.bounds;
        foreach (Vector3 p in rig.neckRim) {
            rig.headNeckRadiusU = Mathf.Max(rig.headNeckRadiusU, p.magnitude);
        }
        Bounds headBounds = template.bounds;
        rig.bodyBones = skinned.bones;
        rig.referenceVisual = body.transform;
        rig.referenceVisual.localScale = Vector3.one;
        rig.referenceVisual.localPosition = Vector3.zero;
        rig.referenceRenderer = skinned;
        rig.referenceHeadVisual =
            new GameObject("CartoonSnakeHead", typeof(MeshFilter),
                typeof(MeshRenderer)).transform;
        rig.referenceHeadVisual.SetParent(rig.host, false);
        rig.referenceHeadVisual.GetComponent<MeshFilter>().sharedMesh =
            template.mesh;
        rig.referenceHeadRenderer =
            rig.referenceHeadVisual.GetComponent<Renderer>();
        rig.referenceHeadRenderer.sharedMaterial =
            rig.referenceRenderer.sharedMaterial;
        rig.referenceRenderer.enabled = false;
        rig.referenceHeadRenderer.enabled = false;
        rig.referenceHeadVisual.gameObject.layer =
            rig.referenceVisual.gameObject.layer;
        rig.surfaceBlock = new MaterialPropertyBlock();
        rig.spineCenters = new Vector3[BodyRings];
        rig.spineRadii = new float[BodyRings];
        rig.referenceHeadBelowPivotU =
            Mathf.Max(config.HitRadiusU, -headBounds.min.y + .05f);
        rig.hitCollider.radius =
            Mathf.Max(headBounds.extents.x,
                config.SmoothBodyRadiusU
                    + Mathf.Max(config.HeadSwayAmplitudeU,
                        config.StandingCoilOuterRadiusU));
        rig.hitCollider.height =
            Mathf.Max(rig.hitCollider.radius * 2f,
                config.SmoothBodyLengthU - headBounds.min.y);
        rig.hitCollider.center =
            Vector3.up * (config.SmoothBodyLengthU + headBounds.min.y) * .5f;
    }

}
