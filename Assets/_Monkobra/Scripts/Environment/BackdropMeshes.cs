using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Builds the simple meshes of the jungle backdrop: the sky dome, and batches
/// of low-poly tree trunks and crowns, each batch one mesh so the whole
/// backdrop is a couple of draws. Vertex colours are sRGB bytes, which the
/// BackdropSurface shader turns linear; no UVs, which nothing reads.
/// </summary>
public class BackdropMeshes {
    // Public Methods  #########################################################
    /// <summary>a sphere seen from inside, centred on the origin</summary>
    public static Mesh Dome(float radiusU) {
        BackdropMeshes mesh = new BackdropMeshes();
        const int AROUND = 32;
        const int DOWN = 16;
        for (int j = 0; j <= DOWN; j++) {
            float polar = Mathf.PI * j / DOWN;
            for (int i = 0; i <= AROUND; i++) {
                float azimuth = Mathf.PI * 2f * i / AROUND;
                Vector3 normal = new Vector3(
                    Mathf.Sin(polar) * Mathf.Cos(azimuth),
                    Mathf.Cos(polar),
                    Mathf.Sin(polar) * Mathf.Sin(azimuth)
                );
                mesh.Add(normal * radiusU, normal, Color.white);
            }
        }
        mesh.Grid(0, AROUND + 1, DOWN + 1);
        return mesh.Build("BackdropSky");
    }

    /// <summary>
    /// Quads for the BackdropClouds shader, which turns them to the camera &
    /// draws each puff: per cloud x, y, z its centre, w its width.
    /// </summary>
    public static Mesh Clouds(List<Vector4> clouds, System.Random random) {
        List<Vector3> centres = new();
        List<Vector2> corners = new();
        List<Vector2> sizeSeeds = new();
        List<int> quads = new();
        foreach (Vector4 cloud in clouds) {
            int start = centres.Count;
            float seed = (float)random.NextDouble() * 100f;
            for (int i = 0; i < 4; i++) {
                centres.Add(cloud);
                corners.Add(
                    new Vector2(i % 2 == 0 ? -1f : 1f, i < 2 ? -1f : 1f)
                );
                sizeSeeds.Add(new Vector2(cloud.w, seed));
            }
            quads.AddRange(
                new[] {
                    start, start + 1, start + 2, start + 2, start + 1, start + 3
                }
            );
        }
        Mesh mesh = new Mesh { name = "BackdropClouds" };
        mesh.SetVertices(centres);
        mesh.SetUVs(0, corners);
        mesh.SetUVs(1, sizeSeeds);
        mesh.SetTriangles(quads, 0);
        // the shader moves every corner, so bounds can't be measured
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * HUGE_U);
        mesh.UploadMeshData(true);
        return mesh;
    }

    /// <summary>
    /// Birds for the BackdropBirds shader, which flies & flaps them: per bird
    /// its path (radius u, start angle rad, height over the eye u, angular
    /// speed rad/s) and its flap phase & span.
    /// </summary>
    public static Mesh Birds(List<Vector4> paths, List<Vector2> flapSpans) {
        // a gull's outline, 1 u span: beak, tail, wing tips swept back
        Vector3[] shape = {
            new Vector3(0f, 0f, 0.35f), new Vector3(0f, 0f, -0.25f),
            new Vector3(-0.5f, 0f, -0.12f), new Vector3(0.5f, 0f, -0.12f),
        };
        List<Vector3> positions = new();
        List<Vector4> pathUvs = new();
        List<Vector2> flapUvs = new();
        List<int> wings = new();
        for (int b = 0; b < paths.Count; b++) {
            int start = positions.Count;
            foreach (Vector3 point in shape) {
                positions.Add(point);
                pathUvs.Add(paths[b]);
                flapUvs.Add(flapSpans[b]);
            }
            wings.AddRange(
                new[] {
                    start, start + 1, start + 2, start, start + 3, start + 1
                }
            );
        }
        Mesh mesh = new Mesh { name = "BackdropBirds" };
        mesh.SetVertices(positions);
        mesh.SetUVs(0, pathUvs);
        mesh.SetUVs(1, flapUvs);
        mesh.SetTriangles(wings, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * HUGE_U);
        mesh.UploadMeshData(true);
        return mesh;
    }

    /// <summary>
    /// A tapering upright trunk from <paramref name="bottom"/> up to
    /// <paramref name="top"/>, its foot flared by
    /// <paramref name="flare"/> as rainforest giants' buttresses are.
    /// </summary>
    public void AddTrunk(
        Vector3 bottom,
        Vector3 top,
        float radiusU,
        float flare,
        int sides,
        int rings,
        Color color
    ) {
        int start = positions.Count;
        Vector3 axis = top - bottom;
        for (int j = 0; j <= rings; j++) {
            float t = (float)j / rings;
            float radius = radiusU
                * (Mathf.Lerp(1f, 0.55f, t) + flare * Mathf.Pow(1f - t, 8f));
            Vector3 centre = bottom + axis * t;
            for (int i = 0; i <= sides; i++) {
                float angle = Mathf.PI * 2f * i / sides;
                Vector3 outward =
                    new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Add(centre + outward * radius, outward, color);
            }
        }
        Grid(start, sides + 1, rings + 1);
    }

    /// <summary>
    /// A tree crown: a squashed, slightly lumpy ball, darker underneath.
    /// </summary>
    public void AddCrown(
        Vector3 centre,
        float radiusU,
        float squash,
        int around,
        int down,
        Color color,
        System.Random random
    ) {
        int start = positions.Count;
        float phase = (float)random.NextDouble() * 6.28f;
        int lumps = 4 + random.Next(3);
        for (int j = 0; j <= down; j++) {
            float polar = Mathf.PI * j / down;
            for (int i = 0; i <= around; i++) {
                float azimuth = Mathf.PI * 2f * i / around;
                Vector3 normal = new Vector3(
                    Mathf.Sin(polar) * Mathf.Cos(azimuth),
                    Mathf.Cos(polar),
                    Mathf.Sin(polar) * Mathf.Sin(azimuth)
                );
                float lump = 1f + 0.08f * Mathf.Sin(
                    azimuth * lumps + phase
                ) * Mathf.Sin(polar);
                Vector3 offset = new Vector3(
                    normal.x, normal.y * squash, normal.z
                ) * (radiusU * lump);
                // the shaded underside of a crown
                Color shaded = color
                    * Mathf.Lerp(0.55f, 0.95f, (normal.y + 1f) * 0.5f);
                Add(centre + offset, normal, shaded);
            }
        }
        Grid(start, around + 1, down + 1);
    }

    /// <summary>
    /// Writes everything added so far into a new mesh, its CPU copy freed
    /// once on the GPU.
    /// </summary>
    public Mesh Build(string name) {
        Mesh mesh = new Mesh { name = name };
        if (positions.Count > 65000) {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        mesh.SetVertices(positions);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        return mesh;
    }

    // private members  ########################################################
    // bounds of a mesh the shader moves about, never culled
    private const float HUGE_U = 100000f;

    private readonly List<Vector3> positions = new List<Vector3>();
    private readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<Color32> colors = new List<Color32>();
    private readonly List<int> triangles = new List<int>();

    private void Add(Vector3 position, Vector3 normal, Color color) {
        positions.Add(position);
        normals.Add(normal);
        color.a = 1f;
        colors.Add(color);
    }

    // a grid of columns × rows vertices from start, each triangle wound to
    // face its vertices' normals: Unity shows a triangle whose
    // Cross(b - a, c - a) faces the viewer
    private void Grid(int start, int columns, int rows) {
        for (int j = 0; j < rows - 1; j++) {
            for (int i = 0; i < columns - 1; i++) {
                int a = start + j * columns + i;
                int b = a + 1;
                int c = a + columns;
                int d = c + 1;
                Facing(a, b, d);
                Facing(a, d, c);
            }
        }
    }

    private void Facing(int a, int b, int c) {
        Vector3 face = Vector3.Cross(
            positions[b] - positions[a], positions[c] - positions[a]
        );
        if (face.sqrMagnitude < 1e-12f) {
            return; // a pole's sliver
        }
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
