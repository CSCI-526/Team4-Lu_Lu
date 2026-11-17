using UnityEngine;


/// <summary>
/// Two bone IK for the skinned monkey model's limbs: bends a root, middle and
/// end bone chain (shoulder, elbow, wrist or hip, knee, ankle) so the end
/// lands on a target, the middle joint bending toward a pole direction.
/// Works on world positions and rotations only, so it never depends on how a
/// bone's local axes were authored.
/// </summary>
public static class ModelTwoBoneIK {
    // Public Methods  #########################################################
    /// <summary>
    /// Turns <paramref name="upper"/> and <paramref name="lower"/> so the
    /// end bone sits on <paramref name="target"/>, or as near as the chain's
    /// length allows. Call it on a chain already reset to its rest pose.
    /// </summary>
    /// <param name="upper">root bone, eg the upper arm or thigh</param>
    /// <param name="lower">middle bone, eg the forearm or shin</param>
    /// <param name="end">end bone, eg the hand or foot</param>
    /// <param name="target">where the end bone goes, world space</param>
    /// <param name="pole">direction the middle joint bends toward, world
    /// space</param>
    /// <param name="jointFrontInUpper">optional: the way the middle joint's
    /// front faces at rest (a kneecap's forward), in the upper bone's own
    /// frame. Given, the upper bone also twists round its length so that front
    /// faces the bend, so the joint folds the way it is built to, never
    /// backward. Zero skips the twist</param>
    public static void Solve(
        Transform upper,
        Transform lower,
        Transform end,
        Vector3 target,
        Vector3 pole,
        Vector3 jointFrontInUpper = default
    ) {
        Vector3 root = upper.position;
        float upperU = Vector3.Distance(root, lower.position);
        float lowerU = Vector3.Distance(lower.position, end.position);
        Vector3 toTarget = target - root;
        float reachU = toTarget.magnitude;
        if (upperU < MIN_LENGTH_U || lowerU < MIN_LENGTH_U
            || reachU < MIN_LENGTH_U) {
            return;
        }

        Vector3 direction = toTarget / reachU;
        reachU = Mathf.Clamp(
            reachU,
            Mathf.Abs(upperU - lowerU) + MIN_LENGTH_U,
            (upperU + lowerU) * MAX_STRAIGHTNESS
        );

        // law of cosines: the angle at the root between target and joint
        float cosRoot = Mathf.Clamp(
            (upperU * upperU + reachU * reachU - lowerU * lowerU)
                / (2f * upperU * reachU),
            -1f,
            1f
        );
        float sinRoot = Mathf.Sqrt(1f - cosRoot * cosRoot);

        Vector3 bend = Vector3.ProjectOnPlane(pole, direction);
        if (bend.sqrMagnitude < MIN_LENGTH_U * MIN_LENGTH_U) {
            bend = Vector3.ProjectOnPlane(Vector3.up, direction);
        }
        if (bend.sqrMagnitude < MIN_LENGTH_U * MIN_LENGTH_U) {
            bend = Vector3.ProjectOnPlane(Vector3.right, direction);
        }
        bend.Normalize();

        Vector3 joint =
            root + direction * (upperU * cosRoot) + bend * (upperU * sinRoot);
        Vector3 goal = root + direction * reachU;

        upper.rotation =
            Quaternion.FromToRotation(lower.position - root, joint - root)
            * upper.rotation;

        // spin the upper bone round its own length, which leaves the joint
        // where it is, till the joint's front faces the bend
        if (jointFrontInUpper != Vector3.zero) {
            Vector3 axis = (joint - root).normalized;
            Vector3 front = Vector3.ProjectOnPlane(
                upper.rotation * jointFrontInUpper,
                axis
            );
            Vector3 facing = Vector3.ProjectOnPlane(bend, axis);
            if (front.sqrMagnitude > MIN_LENGTH_U * MIN_LENGTH_U
                && facing.sqrMagnitude > MIN_LENGTH_U * MIN_LENGTH_U) {
                upper.rotation =
                    Quaternion.AngleAxis(
                        Vector3.SignedAngle(front, facing, axis),
                        axis
                    )
                    * upper.rotation;
            }
        }

        lower.rotation =
            Quaternion.FromToRotation(
                end.position - lower.position,
                goal - lower.position
            )
            * lower.rotation;
    }

    /// <returns>the transform named <paramref name="boneName"/> under
    /// <paramref name="root"/>, itself included, else null</returns>
    public static Transform FindBone(Transform root, string boneName) {
        if (root == null) {
            return null;
        }
        if (root.name == boneName) {
            return root;
        }
        foreach (Transform child in root) {
            Transform found = FindBone(child, boneName);
            if (found != null) {
                return found;
            }
        }
        return null;
    }

    // private members  ########################################################
    private const float MIN_LENGTH_U = 0.0001f;

    // a fully straight limb has no defined bend plane, it snaps & flickers
    private const float MAX_STRAIGHTNESS = 0.999f;
}
