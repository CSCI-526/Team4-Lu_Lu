using System.Collections.Generic;
using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>Tracking eyes and the hit contacts that follow the visible
/// snake.</summary>
public sealed class FallingCobraEyes {
    // Private Members  ########################################################
    private readonly FallingCobraRig rig;
    private readonly FallingCobraTuning tuning;
    private readonly System.Action<Collider> onHeadHit;

    public FallingCobraEyes(FallingCobraRig rig,
        System.Action<Collider> onHeadHit) {
        this.rig = rig;
        tuning = rig.tuning;
        this.onHeadHit = onHeadHit;
    }

    // Public Methods  #########################################################
    public void CreateTrackingEyes() {
        if (!tuning.EyeAnchorsReady || rig.referenceHeadVisual == null) {
            return;
        }
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        rig.eyeWhiteMaterial =
            new Material(shader){name = "Snake eye white",
                hideFlags = HideFlags.DontSave};
        rig.eyeWhiteMaterial.SetColor("_BaseColor", new Color(1f, .98f, .9f));
        rig.eyeWhiteMaterial.SetFloat("_Smoothness", .35f);
        rig.eyePupilMaterial =
            new Material(shader){name = "Snake pupil",
                hideFlags = HideFlags.DontSave};
        rig.eyePupilMaterial.SetColor("_BaseColor",
            new Color(.004f, .008f, .003f));
        rig.eyePupilMaterial.SetFloat("_Smoothness", .5f);
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        int sides = 24, rings = 5;
        float aperture =
            Mathf.Asin(Mathf.Clamp(tuning.EyePupilRadiusU
                / tuning.EyeWhiteRadiusU, .1f, .85f));
        for (int ring = 0; ring <= rings; ring++)for (int side =
            0; side <= sides; side++) {
            float theta = aperture * ring / rings,
                phi = side * Mathf.PI * 2f / sides;
            Vector3 direction =
                new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));
            vertices.Add(direction * (tuning.EyeWhiteRadiusU + .004f));
            normals.Add(direction);
            if (ring<rings && side<sides) {
                int a = ring * (sides + 1) + side, b = a + sides + 1;
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(b + 1);
                triangles.Add(a);
                triangles.Add(b + 1);
                triangles.Add(a + 1);
            }
        }
        rig.eyePupilMesh =
            new Mesh{name = "Tracking pupil cap",
                hideFlags = HideFlags.DontSave};
        rig.eyePupilMesh.SetVertices(vertices);
        rig.eyePupilMesh.SetNormals(normals);
        rig.eyePupilMesh.SetTriangles(triangles, 0);
        rig.eyePupilMesh.RecalculateBounds();
        rig.eyePupils = new Transform[2];
        Vector3[] points =
            {tuning.LeftEyeSurfaceLocal, tuning.RightEyeSurfaceLocal},
                directions =
                    {tuning.LeftEyeNormalLocal, tuning.RightEyeNormalLocal};
        for (int i = 0; i<2; i++) {
            Vector3 center =
                points[i] - directions[i].normalized
                    * (tuning.EyeWhiteRadiusU - .006f);
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = i == 0?"TrackingEyeLeft":"TrackingEyeRight";
            eye.transform.SetParent(rig.referenceHeadVisual, false);
            eye.transform.localPosition = center;
            eye.transform.localScale =
                Vector3.one * (tuning.EyeWhiteRadiusU * 2f);
            eye.GetComponent<Collider>().enabled = false;
            Object.Destroy(eye.GetComponent<Collider>());
            eye.GetComponent<Renderer>().sharedMaterial = rig.eyeWhiteMaterial;
            GameObject pupil =
                new GameObject("TrackingPupil", typeof(MeshFilter),
                    typeof(MeshRenderer));
            pupil.transform.SetParent(rig.referenceHeadVisual, false);
            pupil.transform.localPosition = center;
            pupil.GetComponent<MeshFilter>().sharedMesh = rig.eyePupilMesh;
            pupil.GetComponent<Renderer>().sharedMaterial =
                rig.eyePupilMaterial;
            rig.eyePupils[i] = pupil.transform;
        }
    }

    public void UpdateTrackingEyes() {
        if (rig.eyePupils == null) {
            return;
        }
        for (int i = 0; i<rig.eyePupils.Length; i++) {
            Vector3 normal =
                (i == 0?tuning.LeftEyeNormalLocal
                    :tuning.RightEyeNormalLocal).normalized;
            Vector3 direction = rig.monkeyInHeadRange
                ? rig.referenceHeadVisual.InverseTransformDirection(
                    rig.committedTargetPosition - rig.eyePupils[i].position)
                    .normalized
                : normal;
            Vector3 tangent = Vector3.ProjectOnPlane(direction, normal);
            Vector3 gaze =
                (normal * Mathf.Max(.28f, Vector3.Dot(direction, normal))
                    + tangent).normalized;
            rig.eyePupils[i].localRotation =
                Quaternion.LookRotation(gaze,
                    rig.originalHeadFaceBasis * Vector3.up);
        }
    }

    public void CreateVisibleContacts() {
        if (rig.referenceHeadVisual == null) {
            return;
        }
        rig.hitCollider.radius = tuning.HeadContactRadiusU;
        rig.hitCollider.height = tuning.HeadContactRadiusU * 2f;
        rig.visibleBodyContacts =
            new CapsuleCollider[tuning.BodyContactSections];
        for (int i = 0; i<rig.visibleBodyContacts.Length; i++) {
            GameObject part = new GameObject("VisibleSnakeContact_" + i);
            part.transform.SetParent(rig.host, false);
            part.layer = rig.host.gameObject.layer;
            part.tag = "Branch";
            CapsuleCollider shape = part.AddComponent<CapsuleCollider>();
            shape.direction = 1;
            shape.isTrigger = true;
            shape.enabled = false;
            rig.visibleBodyContacts[i] = shape;
        }
    }

    public void UpdateVisibleContacts() {
        if (rig.referenceHeadVisual == null
            || rig.visibleBodyContacts == null) {
            return;
        }
        bool active = rig.state == FallingCobraState.Falling;
        if (!active) {
            rig.hitCollider.enabled = false;
            foreach (var part in rig.visibleBodyContacts) {
                part.enabled = false;
            }
            return;
        }
        rig.hitCollider.center =
            rig.host.InverseTransformPoint(rig.ThreatPosition);
        rig.hitCollider.radius = tuning.HeadContactRadiusU;
        rig.hitCollider.height = tuning.HeadContactRadiusU * 2f;
        rig.hitCollider.enabled = active;
        for (int i = 0; i<rig.visibleBodyContacts.Length; i++) {
            int first =
                Mathf.RoundToInt(Mathf.Lerp(0f, 1f,
                    i / (float)rig.visibleBodyContacts.Length)
                        * (BodyRings - 1));
            int last =
                Mathf.RoundToInt(Mathf.Lerp(0f, 1f,
                    (i + 1f) / rig.visibleBodyContacts.Length)
                        * (BodyRings - 1));
            Vector3 a = rig.spineCenters[first], b = rig.spineCenters[last],
                delta = b - a;
            CapsuleCollider shape = rig.visibleBodyContacts[i];
            shape.transform.SetPositionAndRotation((a + b) * .5f,
                delta.sqrMagnitude>.000001f
                    ?Quaternion.FromToRotation(Vector3.up, delta)
                        :Quaternion.identity);
            float startRadius =
                Mathf.Lerp(rig.headNeckRadiusU, rig.spineRadii[first],
                    Mathf.SmoothStep(0f, 1f,
                        first * tuning.SmoothBodyLengthU / (BodyRings - 1)
                            / .4f));
            float endRadius =
                Mathf.Lerp(rig.headNeckRadiusU, rig.spineRadii[last],
                    Mathf.SmoothStep(0f, 1f,
                        last * tuning.SmoothBodyLengthU / (BodyRings - 1)
                            / .4f));
            shape.radius = Mathf.Max(.015f, Mathf.Max(startRadius, endRadius));
            shape.height = delta.magnitude + shape.radius * 2f;
            shape.enabled = active;
        }
    }

    public void SweepVisibleHead() {
        Vector3 point = rig.ThreatPosition;
        if (!rig.headContactReady) {
            rig.previousHeadContact = point;
            rig.headContactReady = true;
        }
        int count =
            Physics.OverlapCapsuleNonAlloc(rig.previousHeadContact, point,
                tuning.HeadContactRadiusU, rig.sweptHeadHits, ~0,
                    QueryTriggerInteraction.Collide);
        rig.previousHeadContact = point;
        for (int i = 0; i<count
            && rig.state == FallingCobraState.Falling; i++) {
            onHeadHit(rig.sweptHeadHits[i]);
        }
    }

}
