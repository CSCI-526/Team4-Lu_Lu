using System;
using UnityEngine;

/// <summary>
/// Where a boss fight stands & how tall its arena is: the 2 inputs every
/// arena of both kings, in both modes, is built from. The bare zone, the
/// height marker, the seen waiting height, the guard, & the great cobra's
/// drop after a win all derive from them, so no arena keeps heights of its
/// own.
/// </summary>
[Serializable]
public struct BossArenaLayout {
    // Public Members  #########################################################
    /// <returns>climbed height the fight starts at, where its height marker
    /// stands; u</returns>
    public float locationU;

    /// <returns>height of the bare arena, centered on the location; u
    /// </returns>
    public float heightU;

    public BossArenaLayout(float locationU, float heightU) {
        this.locationU = locationU;
        this.heightU = heightU;
    }

    /// <returns>climbed height where the bare zone begins; u</returns>
    public readonly float BareBottomU => locationU - heightU * 0.5f;

    /// <returns>climbed height where the bare zone ends; u</returns>
    public readonly float BareTopU => locationU + heightU * 0.5f;

    /// <returns>climbed height of the trunk height marker; u</returns>
    public readonly float MarkerU => locationU;

    /// <returns>climbed height from which a king is seen waiting above its
    /// location; u</returns>
    public readonly float SeenWaitingU => locationU - heightU;

    /// <returns>how far down the great cobra restarts its chase after a win,
    /// a full arena height; u</returns>
    public readonly float DropU => heightU;

    /// <param name="guardDepthU">how far below the start the king's fight
    /// wants its guard cobra; u</param>
    /// <returns>climbed height of the guard cobra, never deeper than half
    /// the arena below the location; u</returns>
    public readonly float GuardU(float guardDepthU) {
        return locationU - Mathf.Min(guardDepthU, heightU * 0.5f);
    }

    /// <param name="fightSpaceU">height the fight needs above the location,
    /// eg a nest or a Spider arena; u</param>
    /// <returns>if the fight fits in the bare half above the location
    /// </returns>
    public readonly bool FitsAbove(float fightSpaceU) {
        return fightSpaceU <= heightU * 0.5f;
    }

    /// <param name="climbedU">any climbed height; u</param>
    /// <returns>if <paramref name="climbedU"/> lies in the bare zone</returns>
    public readonly bool Contains(float climbedU) {
        return climbedU >= BareBottomU && climbedU <= BareTopU;
    }

    /// <param name="startY">the run's start y, the climb baseline; world u
    /// </param>
    /// <returns>x: bottom, y: top of the bare zone in world y</returns>
    public readonly Vector2 WorldBare(float startY) {
        return new Vector2(startY + BareBottomU, startY + BareTopU);
    }
}
