using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off Editor menu <c>Monkobra > Migrate Tuning to Configs</c>: copies
/// the serialized <c>tuning</c> object of each listed prefab into a Config
/// asset under <c>Settings/</c> and links the prefab to it, moving the
/// prefab-internal references the old object held onto the component. Safe
/// to run again: a prefab whose <c>config</c> is set is skipped.
/// </summary>
public static class TuningMigration {
    // Public Methods  #########################################################
    [MenuItem("Monkobra/Migrate Tuning to Configs")]
    public static void MigrateAll() {
        int migrated = 0;
        foreach (Job job in JOBS) {
            if (Migrate(job)) {
                migrated++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"TuningMigration:\t{migrated} of {JOBS.Length} prefabs migrated"
        );
    }

    // private types  ##########################################################
    private sealed class Job {
        public readonly string prefabPath;
        public readonly Type holderType;
        public readonly Type configType;
        public readonly string configPath;
        public readonly Dictionary<string, string> renames;
        public readonly string[] movedFields;

        public Job(
            string prefabPath,
            Type holderType,
            Type configType,
            string configPath,
            Dictionary<string, string> renames,
            string[] movedFields
        ) {
            this.prefabPath = prefabPath;
            this.holderType = holderType;
            this.configType = configType;
            this.configPath = configPath;
            this.renames = renames;
            this.movedFields = movedFields;
        }
    }

    // constants  ##############################################################
    private const string PREFABS = "Assets/_Monkobra/Prefabs/";
    private const string SETTINGS = "Assets/_Monkobra/Settings/";

    // private members  ########################################################
    // old field name -> new field name, per config; moved fields keep theirs
    private static readonly Job[] JOBS = {
        new Job(
            PREFABS + "MobGreatCobra/GreatCobra.prefab",
            typeof(CobraMotor),
            typeof(CobraMotorConfig),
            SETTINGS + "MobGreatCobra/CobraMotorConfig.asset",
            new Dictionary<string, string> {
                { "orbitRadius", "orbitRadiusU" },
                { "startHeight", "startHeightU" },
                { "climbSpeed", "climbSpeedUPerS" },
                { "orbitSpeedDegrees", "orbitSpeedDegPerS" },
                { "headTurnSpeedDegrees", "headTurnSpeedDegPerS" },
            },
            new[] {
                "pathCenter",
                "bodySegments",
                "cobraBalanceConfig",
                "playerBalanceConfig",
                "monkeyClimbConfig",
            }
        ),
        new Job(
            PREFABS + "MobFallingCobra/FallingCobra.prefab",
            typeof(FallingCobraController),
            typeof(FallingCobraConfig),
            SETTINGS + "MobFallingCobra/FallingCobraConfig.asset",
            new Dictionary<string, string> {
                { "waitingLookDownDegrees", "waitingLookDownDeg" },
                { "bodyTwistDegreesPerU", "bodyTwistDegPerU" },
                { "maximumSupportBellyAngle", "maximumSupportBellyAngleDeg" },
                { "headLookTurnSpeedDegrees", "headLookTurnSpeedDegPerS" },
                { "flatHeadPitchDegrees", "flatHeadPitchDeg" },
                { "upwardHeadPitchDegrees", "upwardHeadPitchDeg" },
                { "surfaceHeadTurnDegrees", "surfaceHeadTurnDeg" },
                { "trackingSurfaceTurnDegrees", "trackingSurfaceTurnDeg" },
                { "bodyBendDegrees", "bodyBendDeg" },
                { "maximumHeadLeanDegrees", "maximumHeadLeanDeg" },
            },
            new string[0]
        ),
    };

    // private methods  ########################################################
    private static bool Migrate(Job job) {
        GameObject root = PrefabUtility.LoadPrefabContents(job.prefabPath);
        try {
            Component holder = root.GetComponentInChildren(
                job.holderType, true
            );
            if (holder == null) {
                Debug.LogError(
                    $"TuningMigration:\tno {job.holderType.Name} in "
                        + job.prefabPath
                );
                return false;
            }
            SerializedObject holderObj = new SerializedObject(holder);
            SerializedProperty configProp = holderObj.FindProperty("config");
            SerializedProperty tuningProp = holderObj.FindProperty("tuning");
            if (configProp == null || tuningProp == null) {
                Debug.LogError(
                    $"TuningMigration:\t{job.holderType.Name} lacks the "
                        + "config or tuning field"
                );
                return false;
            }
            if (configProp.objectReferenceValue != null) {
                Debug.Log(
                    $"TuningMigration:\t{job.prefabPath} already has a "
                        + "config, skipped"
                );
                return false;
            }

            EnsureFolder(Path.GetDirectoryName(job.configPath));
            ScriptableObject config = ScriptableObject.CreateInstance(
                job.configType
            );
            AssetDatabase.CreateAsset(config, job.configPath);
            SerializedObject configObj = new SerializedObject(config);
            int copied = CopyTuning(tuningProp, configObj, job.renames);
            configObj.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            configProp.objectReferenceValue = config;
            foreach (string field in job.movedFields) {
                SerializedProperty from = tuningProp.FindPropertyRelative(
                    field
                );
                SerializedProperty to = holderObj.FindProperty(field);
                if (from == null || to == null) {
                    Debug.LogError(
                        $"TuningMigration:\tcannot move field {field} of "
                            + job.holderType.Name
                    );
                    continue;
                }
                CopyValue(from, to);
            }
            holderObj.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, job.prefabPath);
            Debug.Log(
                $"TuningMigration:\t{job.configPath}, {copied} values from "
                    + job.prefabPath
            );
            return true;
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // every top-level value of the config, read from the tuning object under
    // its old name; a value the tuning object lacks is reported, not skipped
    private static int CopyTuning(
        SerializedProperty tuning,
        SerializedObject config,
        Dictionary<string, string> renames
    ) {
        Dictionary<string, string> oldNames = new();
        foreach (KeyValuePair<string, string> pair in renames) {
            oldNames[pair.Value] = pair.Key;
        }
        int copied = 0;
        SerializedProperty it = config.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter)) {
            enter = false;
            if (it.propertyPath == "m_Script") {
                continue;
            }
            string sourceName = oldNames.TryGetValue(it.name, out string old)
                ? old : it.name;
            SerializedProperty source = tuning.FindPropertyRelative(
                sourceName
            );
            if (source == null) {
                Debug.LogError(
                    $"TuningMigration:\tno source value for {it.name}"
                );
                continue;
            }
            CopyValue(source, it);
            copied++;
        }
        return copied;
    }

    private static void CopyValue(
        SerializedProperty from,
        SerializedProperty to
    ) {
        bool isList = from.isArray
            && from.propertyType != SerializedPropertyType.String;
        if (isList) {
            to.arraySize = from.arraySize;
            for (int i = 0; i < from.arraySize; i++) {
                CopyValue(
                    from.GetArrayElementAtIndex(i),
                    to.GetArrayElementAtIndex(i)
                );
            }
            return;
        }
        switch (from.propertyType) {
            case SerializedPropertyType.Generic:
                CopyChildren(from, to);
                break;
            case SerializedPropertyType.Integer:
                to.longValue = from.longValue;
                break;
            case SerializedPropertyType.Boolean:
                to.boolValue = from.boolValue;
                break;
            case SerializedPropertyType.Float:
                to.floatValue = from.floatValue;
                break;
            case SerializedPropertyType.String:
                to.stringValue = from.stringValue;
                break;
            case SerializedPropertyType.Color:
                to.colorValue = from.colorValue;
                break;
            case SerializedPropertyType.ObjectReference:
                to.objectReferenceValue = from.objectReferenceValue;
                break;
            case SerializedPropertyType.LayerMask:
                to.intValue = from.intValue;
                break;
            case SerializedPropertyType.Enum:
                to.enumValueIndex = from.enumValueIndex;
                break;
            case SerializedPropertyType.Vector2:
                to.vector2Value = from.vector2Value;
                break;
            case SerializedPropertyType.Vector3:
                to.vector3Value = from.vector3Value;
                break;
            case SerializedPropertyType.Vector4:
                to.vector4Value = from.vector4Value;
                break;
            case SerializedPropertyType.Vector2Int:
                to.vector2IntValue = from.vector2IntValue;
                break;
            case SerializedPropertyType.Vector3Int:
                to.vector3IntValue = from.vector3IntValue;
                break;
            case SerializedPropertyType.Rect:
                to.rectValue = from.rectValue;
                break;
            case SerializedPropertyType.Quaternion:
                to.quaternionValue = from.quaternionValue;
                break;
            case SerializedPropertyType.AnimationCurve:
                to.animationCurveValue = from.animationCurveValue;
                break;
            default:
                Debug.LogError(
                    $"TuningMigration:\tcannot copy {from.propertyPath}, "
                        + $"type {from.propertyType}"
                );
                break;
        }
    }

    // a nested serializable object: each direct child by name
    private static void CopyChildren(
        SerializedProperty from,
        SerializedProperty to
    ) {
        SerializedProperty child = from.Copy();
        SerializedProperty end = from.GetEndProperty();
        bool enter = true;
        while (child.Next(enter)
            && !SerializedProperty.EqualContents(child, end)
        ) {
            enter = false;
            SerializedProperty target = to.FindPropertyRelative(child.name);
            if (target != null) {
                CopyValue(child, target);
            }
        }
    }

    private static void EnsureFolder(string folder) {
        folder = folder.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(folder)) {
            return;
        }
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
