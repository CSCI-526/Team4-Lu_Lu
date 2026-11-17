using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every popup of the game as data: one <see cref="PopUpDefinition"/> per
/// <see cref="PopUpId"/>, read by <see cref="PopUpManager"/>. To add a popup,
/// add an id and a definition here, never a prefab.
/// </summary>
[CreateAssetMenu(
    fileName = "PopUpCatalog", menuName = "Monkobra/PopUp Catalog")]
public class PopUpCatalog: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>every definition, in the order authored</returns>
    public IReadOnlyList<PopUpDefinition> Definitions => definitions;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("one per popup id")]
    private List<PopUpDefinition> definitions = new();
}
