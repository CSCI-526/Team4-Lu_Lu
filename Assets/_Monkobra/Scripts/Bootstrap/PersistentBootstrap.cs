using UnityEngine;

/// <summary>
/// Creates the objects that live for the whole launch, once, before the
/// first scene loads, so no scene needs to hold a copy that destroys itself
/// on every scene load. Each prefab sits in <c>Resources/</c> under its own
/// name and keeps itself across loads (<c>DontDestroyOnLoad</c>).
/// <para>
/// Adding one: put its prefab in <c>Resources/</c>, give its script a static
/// <c>I</c>, and add one <see cref="EnsureExists"/> line below.
/// </para>
/// </summary>
public static class PersistentBootstrap {
    // Private Methods  ########################################################
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentObjects() {
        EnsureExists("Analytics", AnalyticsManager.I != null);
        EnsureExists("ControlStyleManager", ControlStyleManager.I != null);
    }

    // creates the prefab named in Resources unless its object already exists
    private static void EnsureExists(string prefabName, bool isPresent) {
        if (isPresent) {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null) {
            Debug.LogError(
                "PersistentBootstrap:\tfail to load prefab from Resources: "
                    + prefabName
            );
            return;
        }

        GameObject inst = Object.Instantiate(prefab);
        inst.name = prefabName;
    }
}
