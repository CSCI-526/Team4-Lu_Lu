using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Builds the meshes of the cartoon tree: tapering tubes for branches and
/// twigs, hanging moss strands and leaves, all drawn with the
/// <c>Monkobra/ToonBark</c> shader with vertex colours on.
/// <para>
/// Vertex colours steer the shader: R forces moss, G marks a leaf, B shifts
/// a leaf's tint and turns bark into green vine, A scales the silhouette. Tube
/// UV x runs 0 to 1 round the
/// tube and UV y is the distance along it; leaf UV x runs -1 to 1 across the
/// leaf and UV y 0 to 1 along it.
/// </para>
/// <para>
/// Every triangle is wound to face its intended normal, so callers never
/// have to think about winding order.
/// </para>
/// </summary>
public class StylizedMeshBuilder {
    // Public Members  #########################################################
    /// <summary>bark: no forced moss, full outline</summary>
    public static readonly Color BARK = new Color(0f, 0f, 0f, 1f);

    /// <summary>fruit stalk: green vine, never mossy, a thin outline</summary>
    public static readonly Color VINE = new Color(0f, 0f, 1f, 0.6f);

    /// <summary>moss strand: forced moss, a thin outline</summary>
    public static readonly Color MOSS_STRAND = new Color(1f, 0f, 0f, 0.35f);

    /// <returns>vertex count so far</returns>
    public int VertexCount => positions.Count;

    // Public Methods  #########################################################
    /// <summary>
    /// A tube along a centreline, each ring an ellipse, its UV y the distance
    /// travelled along the centreline from <paramref name="startAlongU"/>.
    /// </summary>
    /// <param name="centres">ring centres, 2 or more</param>
    /// <param name="ups">each ring's up, the ellipse's first axis</param>
    /// <param name="radiiUp">each ring's radius along its up</param>
    /// <param name="radiiSide">each ring's radius across its up</param>
    /// <param name="sides">vertices round each ring</param>
    /// <param name="color">vertex colour of the whole tube</param>
    /// <param name="capEnd">closes the last ring into a rounded tip</param>
    public void AddTube(
        IList<Vector3> centres,
        IList<Vector3> ups,
        IList<float> radiiUp,
        IList<float> radiiSide,
        int sides,
        Color color,
        bool capEnd,
        float startAlongU = 0f
    ) {
        int rings = centres.Count;
        float alongU = startAlongU;
        int[] ringStart = new int[rings];

        for (int r = 0; r < rings; r++) {
            if (r > 0) {
                alongU += Vector3.Distance(centres[r], centres[r - 1]);
            }
            Vector3 tangent = (centres[Mathf.Min(r + 1, rings - 1)]
                - centres[Mathf.Max(r - 1, 0)]).normalized;
            Vector3 up = Vector3.ProjectOnPlane(ups[r], tangent).normalized;
            Vector3 side = Vector3.Cross(tangent, up);
            ringStart[r] = positions.Count;

            // the last vertex repeats the first, so UV x can reach 1
            for (int s = 0; s <= sides; s++) {
                float angle = s * Mathf.PI * 2f / sides;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                Vector3 offset = up * (cos * radiiUp[r])
                    + side * (sin * radiiSide[r]);
                // an ellipse's normal leans toward its flatter side
                Vector3 normal = (
                    up * (cos / Mathf.Max(radiiUp[r], 0.0001f))
                    + side * (sin / Mathf.Max(radiiSide[r], 0.0001f))
                ).normalized;
                AddVertex(
                    centres[r] + offset,
                    normal,
                    new Vector2((float)s / sides, alongU),
                    color
                );
            }
        }

        for (int r = 0; r < rings - 1; r++) {
            for (int s = 0; s < sides; s++) {
                int a = ringStart[r] + s;
                int b = ringStart[r] + s + 1;
                int c = ringStart[r + 1] + s;
                int d = ringStart[r + 1] + s + 1;
                AddQuad(a, b, d, c);
            }
        }

        if (capEnd) {
            Vector3 last = centres[rings - 1];
            Vector3 tangent = (last - centres[rings - 2]).normalized;
            float radius = Mathf.Min(radiiUp[rings - 1], radiiSide[rings - 1]);
            int tip = AddVertex(
                last + tangent * radius * 0.8f,
                tangent,
                new Vector2(0.5f, alongU + radius * 0.8f),
                color
            );
            for (int s = 0; s < sides; s++) {
                AddTriangleFacing(
                    ringStart[rings - 1] + s,
                    ringStart[rings - 1] + s + 1,
                    tip
                );
            }
        }
    }

    /// <summary>
    /// A straight tapering tube from <paramref name="start"/> along
    /// <paramref name="direction"/>, gently curving toward
    /// <paramref name="bend"/>.
    /// </summary>
    public void AddTaperedTube(
        Vector3 start,
        Vector3 direction,
        Vector3 bend,
        float lengthU,
        float startRadiusU,
        float endRadiusU,
        int rings,
        int sides,
        Color color
    ) {
        List<Vector3> centres = new List<Vector3>(rings);
        List<Vector3> ups = new List<Vector3>(rings);
        List<float> radii = new List<float>(rings);
        Vector3 axis = direction.normalized;
        Vector3 up = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f
            ? Vector3.forward
            : Vector3.up;
        for (int r = 0; r < rings; r++) {
            float t = (float)r / (rings - 1);
            centres.Add(start + axis * (lengthU * t) + bend * (t * t));
            ups.Add(up);
            radii.Add(Mathf.Lerp(startRadiusU, endRadiusU, t));
        }
        AddTube(centres, ups, radii, radii, sides, color, true);
    }

    /// <summary>
    /// A hanging moss strand: a thin wavy tube from
    /// <paramref name="top"/> down, swaying a little toward
    /// <paramref name="sway"/>.
    /// </summary>
    public void AddMossStrand(
        Vector3 top,
        Vector3 sway,
        float lengthU,
        float radiusU,
        System.Random random
    ) {
        const int RINGS = 6;
        List<Vector3> centres = new List<Vector3>(RINGS);
        List<Vector3> ups = new List<Vector3>(RINGS);
        List<float> radii = new List<float>(RINGS);
        float phase = (float)random.NextDouble() * Mathf.PI * 2f;
        for (int r = 0; r < RINGS; r++) {
            float t = (float)r / (RINGS - 1);
            Vector3 wave = sway * (t * t) + sway.normalized
                * (Mathf.Sin(phase + t * 5f) * 0.03f);
            centres.Add(top + Vector3.down * (lengthU * t) + wave);
            ups.Add(Vector3.forward);
            // full at the top, tapering to a point, a little lumpy
            radii.Add(
                radiusU * (1f - 0.8f * t) * (
                    0.85f + 0.3f * (float)random.NextDouble()
                )
            );
        }
        AddTube(centres, ups, radii, radii, 5, MOSS_STRAND, true);
    }

    /// <summary>
    /// A double sided leaf, pointed, cupped along its rib, its tip curling
    /// toward <paramref name="face"/>.
    /// </summary>
    /// <param name="stalk">where the leaf grows from</param>
    /// <param name="direction">the way it points</param>
    /// <param name="face">the way its upper side faces</param>
    /// <param name="tint">0 the leaf colour, 1 its variant</param>
    public void AddLeaf(
        Vector3 stalk,
        Vector3 direction,
        Vector3 face,
        float lengthU,
        float widthU,
        float curl,
        float tint
    ) {
        const int ROWS = 7;
        Vector3 along = direction.normalized;
        Vector3 up = Vector3.ProjectOnPlane(face, along).normalized;
        Vector3 side = Vector3.Cross(up, along);
        Color color = new Color(0f, 1f, tint, 0f);

        int[] front = new int[ROWS * 3];
        int[] back = new int[ROWS * 3];
        for (int i = 0; i < ROWS; i++) {
            float t = (float)i / (ROWS - 1);
            // round near the stalk, pointed at the tip
            float halfWidth =
                widthU * 0.5f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f));
            Vector3 centre =
                stalk + along * (lengthU * t) + up * (curl * lengthU * t * t);
            Vector3 rowUp = (up - along * (2f * curl * t)).normalized;
            for (int k = 0; k < 3; k++) {
                float across = k - 1f;
                // the edges lift a little: a shallow cup along the rib
                Vector3 position = centre + side * (across * halfWidth)
                    + rowUp * (Mathf.Abs(across) * halfWidth * 0.3f);
                Vector3 normal = (rowUp - side * (across * 0.3f)).normalized;
                Vector2 uv = new Vector2(across, t);
                front[i * 3 + k] = AddVertex(position, normal, uv, color);
                back[i * 3 + k] = AddVertex(position, -normal, uv, color);
            }
        }
        for (int i = 0; i < ROWS - 1; i++) {
            for (int k = 0; k < 2; k++) {
                int a = i * 3 + k;
                int b = i * 3 + k + 1;
                int c = (i + 1) * 3 + k;
                int d = (i + 1) * 3 + k + 1;
                AddQuad(front[a], front[b], front[d], front[c]);
                AddQuad(back[a], back[b], back[d], back[c]);
            }
        }
    }

    /// <summary>
    /// A rosette of leaves fanning out round <paramref name="normal"/>, the
    /// way the painted plants grow out of the moss.
    /// </summary>
    public void AddRosette(
        Vector3 centre,
        Vector3 normal,
        int leaves,
        float lengthU,
        System.Random random
    ) {
        Vector3 n = normal.normalized;
        Vector3 t1 = Vector3.ProjectOnPlane(
            Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up,
            n
        ).normalized;
        Vector3 t2 = Vector3.Cross(n, t1);
        float start = (float)random.NextDouble() * Mathf.PI * 2f;
        float tint = (float)random.NextDouble();
        for (int i = 0; i < leaves; i++) {
            float angle = start + i * Mathf.PI * 2f / leaves
                + ((float)random.NextDouble() - 0.5f) * 0.5f;
            Vector3 flat = t1 * Mathf.Cos(angle) + t2 * Mathf.Sin(angle);
            // fan out, rising off the surface
            Vector3 direction = (
                flat + n * (0.45f + 0.3f * (float)random.NextDouble())
            ).normalized;
            float length = lengthU
                * (0.75f + 0.5f * (float)random.NextDouble());
            AddLeaf(
                centre,
                direction,
                n,
                length,
                length * 0.42f,
                -0.18f,
                Mathf.Clamp01(tint + ((float)random.NextDouble() - 0.5f) * 0.4f)
            );
        }
    }

    /// <summary>
    /// Writes everything into <paramref name="mesh"/>, replacing what it held.
    /// </summary>
    public void Fill(Mesh mesh) {
        mesh.Clear();
        if (positions.Count > 65000) {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        mesh.SetVertices(positions);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    public void Clear() {
        positions.Clear();
        normals.Clear();
        uvs.Clear();
        colors.Clear();
        triangles.Clear();
    }

    // private members  ########################################################
    private readonly List<Vector3> positions = new List<Vector3>();
    private readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<Vector2> uvs = new List<Vector2>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> triangles = new List<int>();

    private int AddVertex(
        Vector3 position, Vector3 normal, Vector2 uv, Color color
    ) {
        positions.Add(position);
        normals.Add(normal);
        uvs.Add(uv);
        colors.Add(color);
        return positions.Count - 1;
    }

    // a, b, c, d round the quad's edge
    private void AddQuad(int a, int b, int c, int d) {
        AddTriangleFacing(a, b, c);
        AddTriangleFacing(a, c, d);
    }

    // winds the triangle so its face points along its vertices' normals:
    // Unity shows a triangle whose Cross(b - a, c - a) faces the viewer
    private void AddTriangleFacing(int a, int b, int c) {
        Vector3 face = Vector3.Cross(
            positions[b] - positions[a], positions[c] - positions[a]
        );
        Vector3 normal = normals[a] + normals[b] + normals[c];
        if (Vector3.Dot(face, normal) >= 0f) {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        } else {
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
        }
    }
}
