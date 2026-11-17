using System.Collections.Generic;
using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>Builds the rigid head and the animated body mesh a falling cobra
/// shows.</summary>
public sealed class FallingCobraHeadMesh {
    // Private Members  ########################################################
    private readonly FallingCobraRig rig;
    private readonly FallingCobraTuning tuning;

    public FallingCobraHeadMesh(FallingCobraRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    private static readonly Dictionary<(Mesh, float, Vector3, float),
        HeadTemplate> headTemplates = new();

    // Public Methods  #########################################################
    public void BuildCartoonVisual() {
        if (!tuning.CartoonAppearance.enabled) {
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
        if (tuning.CartoonAppearance.referenceBody != null) {
            foreach (Transform segment in segments) {
                if (segment.TryGetComponent(out Renderer renderer)) {
                    renderer.enabled = false;
                }
            }
            GameObject visual =
                Object.Instantiate(tuning.CartoonAppearance.referenceBody,
                    rig.host);
            visual.name = "CartoonSnakeBody";
            MeshFilter meshFilter = visual.GetComponent<MeshFilter>();
            BuildFallingReference(meshFilter);
            return;
        }
        rig.cartoonVisual =
            new CartoonCobraVisual(rig.host, source.sharedMaterial,
                tuning.CartoonAppearance);
        Transform head = new GameObject("CartoonHeadAnchor").transform;
        head.SetParent(rig.host, false);
        head.localPosition = segments[0].localPosition;
        head.localRotation = Quaternion.Euler(0f, 0f, 180f);
        rig.cartoonVisual.BuildHead(head, tuning.HitRadiusU * 0.78f);
        Vector3[] centers = new Vector3[segments.Count];
        Vector3[] outward = new Vector3[segments.Count];
        float[] radii = new float[segments.Count];
        for (int i = 0; i < segments.Count; i++) {
            float t = i / (float)(segments.Count - 1);
            Vector3 local = segments[i].localPosition;
            local.x += Mathf.Sin(t * Mathf.PI * 3f) * tuning.HitRadiusU * 0.38f;
            centers[i] = rig.host.TransformPoint(local);
            outward[i] = -rig.host.forward;
            radii[i] = tuning.HitRadiusU * 0.37f * (1f - t * t);
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
            Vector3.ProjectOnPlane(align * tuning.ReferenceFaceDirection,
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
                Mathf.Lerp(1f, tuning.NeckWaistScale,
                    Mathf.SmoothStep(0f, 1f, collarT));
            float derivative =
                (tuning.NeckWaistScale - 1f) * 6f * collarT * (1f - collarT)
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
            Quaternion.LookRotation(yaw * tuning.ReferenceFaceDirection,
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
                    * tuning.NeckWaistScale;
            template.rimUV[side] = rimUV;
            template.rimNormals[side] =
                new Vector3(direction.x, 0f, direction.y);
        }
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

    public void BuildFallingReference(MeshFilter filter) {
        rig.swimPhaseOffset =
            (float)new System.Random(rig.host.GetInstanceID()).NextDouble()
                * Mathf.PI * 2f;
        Mesh source = filter.sharedMesh;
        float size =
            tuning.HitRadiusU * 2f * tuning.ReferenceSizeMultiplier
                / Mathf.Max(source.bounds.size.x, source.bounds.size.z);
        (Mesh, float, Vector3, float) key =
            (source, size, tuning.ReferenceFaceDirection,
                tuning.NeckWaistScale);
        if (!headTemplates.TryGetValue(key, out HeadTemplate template)) {
            template = CreateHeadTemplate(source, size);
            headTemplates.Add(key, template);
        }
        rig.originalHeadVertices = template.vertices;
        rig.headVertexCount = template.vertices.Length;
        rig.originalHeadFront = template.front;
        rig.originalHeadFaceBasis = template.faceBasis;
        rig.originalHeadFaceCenter = template.faceCenter;
        rig.neckRim = template.rim;
        rig.neckTextureUV = template.rimUV;
        rig.neckSourceNormals = template.rimNormals;
        rig.originalHeadBounds = template.bounds;
        foreach (Vector3 p in rig.neckRim) {
            rig.headNeckRadiusU = Mathf.Max(rig.headNeckRadiusU, p.magnitude);
        }
        Bounds headBounds = template.bounds;
        int total = BodyRings * BodyStride + 2;
        rig.referenceWaveVertices = new Vector3[total];
        rig.referenceWaveNormals = new Vector3[total];
        rig.paintedBellyDirections = new Vector3[total];
        Vector2[] coordinates = new Vector2[total];
        Vector2[] sourceUV = new Vector2[total];
        Color[] colors = new Color[total];
        List<int> indices = new List<int>(BodyRings * BodySides * 6);
        for (int ring = 0; ring < BodyRings; ring++) {
            float along = ring / (float)(BodyRings - 1);
            for (int side = 0; side <= BodySides; side++) {
                int vertex = ring * BodyStride + side;
                coordinates[vertex] =
                    new Vector2(side / (float)BodySides, along);
                sourceUV[vertex] = rig.neckTextureUV[side];
                colors[vertex] = new Color(1f, 0f, 0f, 1f);
                if (ring + 1 < BodyRings && side < BodySides) {
                    int next = vertex + BodyStride;
                    indices.Add(vertex);
                    indices.Add(next);
                    indices.Add(vertex + 1);
                    indices.Add(vertex + 1);
                    indices.Add(next);
                    indices.Add(next + 1);
                }
            }
        }
        int tip = total - 2;
        int neckCap = total - 1;
        colors[neckCap] = new Color(1f, 0f, 0f, 1f);
        coordinates[neckCap] = new Vector2(.5f, 0f);
        sourceUV[neckCap] = rig.neckTextureUV[0];
        for (int side = 0; side < BodySides; side++) {
            indices.Add(neckCap);
            indices.Add(side);
            indices.Add(side + 1);
        }
        colors[tip] = new Color(1f, 0f, 0f, 1f);
        coordinates[tip] = new Vector2(.5f, 1f);
        for (int side = 0; side < BodySides; side++) {
            indices.Add((BodyRings - 1) * BodyStride + side);
            indices.Add(tip);
            indices.Add((BodyRings - 1) * BodyStride + side + 1);
        }
        rig.animatedReferenceMesh = new Mesh { name = "Smooth spine snake" };
        rig.animatedReferenceMesh.MarkDynamic();
        rig.animatedReferenceMesh.vertices = rig.referenceWaveVertices;
        rig.animatedReferenceMesh.normals = rig.referenceWaveNormals;
        rig.animatedReferenceMesh.uv = coordinates;
        rig.animatedReferenceMesh.uv2 = sourceUV;
        rig.animatedReferenceMesh.colors = colors;
        rig.animatedReferenceMesh.triangles = indices.ToArray();
        filter.sharedMesh = rig.animatedReferenceMesh;
        rig.referenceVisual = filter.transform;
        rig.referenceVisual.localScale = Vector3.one;
        rig.referenceVisual.localPosition = Vector3.zero;
        rig.referenceRenderer = filter.GetComponent<Renderer>();
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
        rig.spineTangents = new Vector3[BodyRings];
        rig.spineRadii = new float[BodyRings];
        rig.spineContact = new float[BodyRings];
        rig.referenceHeadBelowPivotU =
            Mathf.Max(tuning.HitRadiusU, -headBounds.min.y + .05f);
        rig.hitCollider.radius =
            Mathf.Max(headBounds.extents.x,
                tuning.SmoothBodyRadiusU
                    + Mathf.Max(tuning.HeadSwayAmplitudeU,
                        tuning.StandingCoilOuterRadiusU));
        rig.hitCollider.height =
            Mathf.Max(rig.hitCollider.radius * 2f,
                tuning.SmoothBodyLengthU - headBounds.min.y);
        rig.hitCollider.center =
            Vector3.up * (tuning.SmoothBodyLengthU + headBounds.min.y) * .5f;
    }

}
