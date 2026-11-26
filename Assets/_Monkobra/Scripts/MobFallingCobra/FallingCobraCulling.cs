using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One Unity <see cref="CullingGroup"/> shared by every falling cobra, so a
/// sleeping snake rebuilds its pose only while it is inside the gameplay
/// camera's view and within the gate around the monkey. Frustum only: unlike
/// <c>Renderer.isVisible</c> it ignores shadow maps and the Scene view, so an
/// off screen sleeper no longer rebuilds for them.
/// <para>
/// The group is made when the first cobra registers and disposed when the
/// last one leaves. A cobra registers in <c>OnEnable</c>, leaves in
/// <c>OnDisable</c>, and moves its sphere each time it asks.
/// </para>
/// </summary>
public static class FallingCobraCulling {
    // Public Members  #########################################################
    /// <summary>a cobra's seat in the group</summary>
    public sealed class Handle {
        internal int index;
    }

    /// <returns>how many cobras the group holds</returns>
    public static int RegisteredCount => handles.Count;

    // Public Methods  #########################################################
    /// <summary>
    /// Adds a cobra to the group, making the group for the first one.
    /// </summary>
    /// <param name="center">world center of the cobra's sphere</param>
    /// <param name="radiusU">radius of the sphere, covering the perched
    /// snake; u</param>
    /// <param name="gateU">distance from the monkey beyond which a sleeper
    /// idles; u</param>
    public static Handle Register(Vector3 center, float radiusU,
        float gateU) {
        if (group == null) {
            group = new CullingGroup();
            spheres = new BoundingSphere[INITIAL_CAPACITY];
            group.SetBoundingSpheres(spheres);
            group.SetBoundingSphereCount(0);
        }
        if (!Mathf.Approximately(gateU, currentGateU)) {
            currentGateU = gateU;
            group.SetBoundingDistances(new[] { gateU });
        }
        if (handles.Count == spheres.Length) {
            System.Array.Resize(ref spheres, spheres.Length * 2);
            group.SetBoundingSpheres(spheres);
        }
        Handle handle = new Handle { index = handles.Count };
        spheres[handle.index] = new BoundingSphere(center, radiusU);
        handles.Add(handle);
        group.SetBoundingSphereCount(handles.Count);
        return handle;
    }

    /// <summary>
    /// Takes a cobra out of the group, disposing the group with the last one.
    /// </summary>
    public static void Unregister(Handle handle) {
        if (handle == null || group == null || handle.index < 0
            || handle.index >= handles.Count
            || handles[handle.index] != handle) {
            return;
        }
        int last = handles.Count - 1;
        if (handle.index != last) {
            Handle moved = handles[last];
            handles[handle.index] = moved;
            spheres[handle.index] = spheres[last];
            moved.index = handle.index;
        }
        handles.RemoveAt(last);
        handle.index = -1;
        group.SetBoundingSphereCount(handles.Count);
        if (handles.Count == 0) {
            group.Dispose();
            group = null;
            spheres = null;
            currentGateU = -1f;
        }
    }

    /// <summary>
    /// Moves the cobra's sphere, then tells if a sleeping cobra may hold its
    /// last pose: it is out of the gameplay camera's view, or beyond the gate
    /// from the monkey. A cobra not in the group, or a group without a
    /// camera, never idles.
    /// </summary>
    /// <param name="handle">the cobra's seat</param>
    /// <param name="center">where the cobra's sphere is now</param>
    /// <param name="monkey">the monkey the gate is measured from</param>
    /// <returns>if the cobra may skip rebuilding its pose</returns>
    public static bool IsIdle(Handle handle, Vector3 center,
        Transform monkey) {
        if (handle == null || group == null || handle.index < 0) {
            return false;
        }
        spheres[handle.index].position = center;
        if (group.targetCamera == null) {
            group.targetCamera = Camera.main;
        }
        if (group.targetCamera == null || monkey == null) {
            return false;
        }
        group.SetDistanceReferencePoint(monkey);
        return !group.IsVisible(handle.index)
            || group.GetDistance(handle.index) > 0;
    }

    // Static Reset  ###########################################################
    // a play session that skips the domain reload keeps statics, so clear
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        group?.Dispose();
        group = null;
        spheres = null;
        currentGateU = -1f;
        handles.Clear();
    }

    // constants  ##############################################################
    private const int INITIAL_CAPACITY = 32;

    // private members  ########################################################
    private static CullingGroup group;
    private static BoundingSphere[] spheres;
    private static float currentGateU = -1f;
    private static readonly List<Handle> handles = new();
}
