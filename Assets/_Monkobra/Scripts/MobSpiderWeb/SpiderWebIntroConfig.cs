using UnityEngine;

/// <summary>
/// Every tuned value of the Spider King's web intro, the scripted event that
/// teaches the web escape once per Normal Play run: when it fires, where the
/// king shows, and the timing of its wind up, throw and exit. Read-only at
/// runtime, so a value changed in Play mode persists in the asset.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Objects/SpiderWebIntroConfig")]
public class SpiderWebIntroConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>climbed height above the start the event fires at; u</returns>
    public float TriggerClimbedHeightU => triggerClimbedHeightU;

    /// <returns>how far above the monkey the king appears; u</returns>
    public float KingAboveU => kingAboveU;

    /// <returns>time the shot shows the empty trunk before the king comes
    /// in; s</returns>
    public float EstablishS => establishS;

    /// <returns>time the king takes to scuttle in round the trunk; s
    /// </returns>
    public float EnterTimeS => enterTimeS;

    /// <returns>how far round the trunk, from where it ends up, the king
    /// starts its scuttle in, out of the camera's sight; deg</returns>
    public float EnterAngleDeg => enterAngleDeg;

    /// <returns>time the king's eyes take to glow up before the throw; s
    /// </returns>
    public float WindUpS => windUpS;

    /// <returns>time the web takes to fly from the king to the monkey; s
    /// </returns>
    public float ThrowTimeS => throwTimeS;

    /// <returns>how far the web's flight arcs out from the trunk, at its
    /// middle; u</returns>
    public float ArcHeightU => arcHeightU;

    /// <returns>size of the web as it leaves the king, a share of its full
    /// size</returns>
    public float StartScale => startScale;

    /// <returns>time the shot lingers on the trapped monkey before it is
    /// released; s</returns>
    public float StickHoldS => stickHoldS;

    /// <returns>time the king takes to climb off and vanish; s</returns>
    public float ExitTimeS => exitTimeS;

    /// <returns>speed the king climbs off at; u/s</returns>
    public float ExitSpeedU => exitSpeedU;

    /// <returns>trunk radius the web sticks to, as the web spawner's; u
    /// </returns>
    public float TrunkRadiusU => trunkRadiusU;

    // Inspector Fields  #######################################################
    [Header("Trigger")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("climbed height above the start the event fires at, once per "
        + "Normal Play run; set below where webs begin to grow; u")]
    private float triggerClimbedHeightU = 250f;

    [Header("King")]
    [SerializeField]
    [Min(1f)]
    [Tooltip("how far above the monkey the king appears; u")]
    private float kingAboveU = 7f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("time the shot shows the empty trunk before the king comes in; s")]
    private float establishS = 1f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("time the king takes to scuttle in round the trunk; s")]
    private float enterTimeS = 1.6f;

    [SerializeField]
    [Range(0f, 180f)]
    [Tooltip("how far round the trunk from its resting spot the king starts "
        + "its scuttle in, behind the trunk from the camera; deg")]
    private float enterAngleDeg = 70f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("time the king's eyes take to glow up before the throw; s")]
    private float windUpS = 2f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("time the king takes to climb off and vanish; s")]
    private float exitTimeS = 2.2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("speed the king climbs off at; u/s")]
    private float exitSpeedU = 9f;

    [Header("Web")]
    [SerializeField]
    [Min(0.1f)]
    [Tooltip("time the web takes to fly from the king to the monkey; s")]
    private float throwTimeS = 1.3f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("how far the flight arcs out from the trunk, at its middle; u")]
    private float arcHeightU = 2f;

    [SerializeField]
    [Range(0.05f, 1f)]
    [Tooltip("size of the web as it leaves the king, a share of full size; "
        + "it grows to full on the way")]
    private float startScale = 0.2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("trunk radius the web sticks to, as SpiderWebSpawner's; u")]
    private float trunkRadiusU = 5.02f;

    [Header("Camera")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("time the shot lingers on the trapped monkey before it is "
        + "released, so the cut away ends before the escape is asked for; s")]
    private float stickHoldS = 1.6f;
}
