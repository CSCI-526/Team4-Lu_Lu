using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Temporary tool of the config reorganization: dumps every serialized field
/// of every config asset under <c>Settings/</c> to one JSON file, so later
/// movements can prove no tuned value was lost. Removed once the
/// reorganization is verified.
/// </summary>
public static class ConfigSnapshot {
    // Public Members  #########################################################
    public const string SettingsFolder = "Assets/_Monkobra/Settings";
    public const string DefaultOutPath = "tools/config-baseline.json";

    /// <summary>
    /// Batch entry point, <c>-executeMethod ConfigSnapshot.DumpBatch</c>,
    /// optional <c>-snapshotOut &lt;path&gt;</c> relative to the project root.
    /// </summary>
    public static void DumpBatch() {
        string outPath = GetArg("-snapshotOut") ?? DefaultOutPath;
        Dump(outPath);
        Debug.Log($"[ConfigSnapshot] wrote {outPath}");
    }

    /// <summary>
    /// Writes the snapshot to <paramref name="relOutPath"/>.
    /// </summary>
    public static void Dump(string relOutPath) {
        string json = BuildJson();
        string full = Path.Combine(GetProjectRoot(), relOutPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllText(full, json, new UTF8Encoding(false));
    }

    /// <summary>
    /// Flat map of one asset, property path to value text, keys sorted.
    /// </summary>
    public static SortedDictionary<string, string> ReadFields(
        UnityEngine.Object asset
    ) {
        var fields = new SortedDictionary<string, string>(
            StringComparer.Ordinal
        );
        var so = new SerializedObject(asset);
        SerializedProperty it = so.GetIterator();
        bool enter = true;
        while (it.Next(enter)) {
            enter = true;
            if (it.depth == 0 && IsSkippedRoot(it.propertyPath)) {
                enter = false;
                continue;
            }
            if (it.propertyType == SerializedPropertyType.Generic) {
                continue;
            }
            // runtime instance ids of an object reference, differ per run
            if (it.propertyPath.EndsWith(".m_FileID", StringComparison.Ordinal)
                || it.propertyPath.EndsWith(
                    ".m_PathID", StringComparison.Ordinal
                )) {
                continue;
            }
            fields[it.propertyPath] = ReadValue(it);
        }
        return fields;
    }

    // Aux Members  ############################################################
    private static bool IsSkippedRoot(string path) {
        return path == "m_ObjectHideFlags"
            || path == "m_CorrespondingSourceObject"
            || path == "m_PrefabInstance"
            || path == "m_PrefabAsset"
            || path == "m_GameObject"
            || path == "m_Enabled"
            || path == "m_EditorHideFlags"
            || path == "m_Script"
            || path == "m_Name"
            || path == "m_EditorClassIdentifier";
    }

    private static string ReadValue(SerializedProperty p) {
        switch (p.propertyType) {
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character:
                return p.longValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.Boolean:
                return p.boolValue ? "true" : "false";
            case SerializedPropertyType.Float:
                return p.floatValue.ToString("R", CultureInfo.InvariantCulture);
            case SerializedPropertyType.String:
                return p.stringValue;
            case SerializedPropertyType.Enum:
                return p.enumValueIndex.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.ObjectReference:
                return ReadReference(p.objectReferenceValue);
            default:
                return "<" + p.propertyType + ">";
        }
    }

    private static string ReadReference(UnityEngine.Object obj) {
        if (obj == null) {
            return "null";
        }
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                obj, out string guid, out long fileId
            )) {
            return guid + ":" + fileId.ToString(CultureInfo.InvariantCulture);
        }
        return "unresolved:" + obj.name;
    }

    private static string BuildJson() {
        var entries = new SortedDictionary<string, string>(
            StringComparer.Ordinal
        );
        string[] guids = AssetDatabase.FindAssets(
            "t:ScriptableObject", new[] { SettingsFolder }
        );
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) {
                // a missing script must show up in the dump, not vanish
                entries[guid] = BuildEntry(path, "<MissingScript>", null);
                continue;
            }
            string className = asset.GetType().Name;
            if (!className.EndsWith("Config", StringComparison.Ordinal)) {
                continue;
            }
            entries[guid] = BuildEntry(path, className, ReadFields(asset));
        }

        var sb = new StringBuilder();
        sb.Append("{\n");
        int i = 0;
        foreach (KeyValuePair<string, string> e in entries) {
            sb.Append("  ").Append(Quote(e.Key)).Append(": ").Append(e.Value);
            sb.Append(++i < entries.Count ? ",\n" : "\n");
        }
        sb.Append("}\n");
        return sb.ToString();
    }

    private static string BuildEntry(
        string path,
        string className,
        SortedDictionary<string, string> fields
    ) {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("    \"class\": ").Append(Quote(className)).Append(",\n");
        sb.Append("    \"fields\": {");
        if (fields != null && fields.Count > 0) {
            sb.Append('\n');
            int i = 0;
            foreach (KeyValuePair<string, string> f in fields) {
                sb.Append("      ").Append(Quote(f.Key)).Append(": ");
                sb.Append(Quote(f.Value));
                sb.Append(++i < fields.Count ? ",\n" : "\n");
            }
            sb.Append("    ");
        }
        sb.Append("}\n  }");
        return sb.ToString();
    }

    private static string Quote(string s) {
        var sb = new StringBuilder("\"");
        foreach (char c in s) {
            switch (c) {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.Append('"').ToString();
    }

    private static string GetProjectRoot() {
        return Directory.GetParent(Application.dataPath).FullName;
    }

    private static string GetArg(string name) {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] == name) {
                return args[i + 1];
            }
        }
        return null;
    }

    [MenuItem("Monkobra/Config/Dump Snapshot")]
    private static void DumpMenu() {
        Dump(DefaultOutPath);
        Debug.Log($"[ConfigSnapshot] wrote {DefaultOutPath}");
    }
}
