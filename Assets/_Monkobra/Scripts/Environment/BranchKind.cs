using UnityEngine;

/// <summary>
/// The 4 kinds of branch the tree grows, each its own prefab with its own
/// chance curve in <see cref="WorldBalanceConfig"/>, so a branch's fruit is
/// decided by which prefab the segment instantiates.
/// </summary>
public enum BranchKind {
    /// <summary>no fruit</summary>
    Empty,
    /// <summary>a banana, restores stamina</summary>
    Banana,
    /// <summary>a coconut, thrown to freeze the great cobra</summary>
    Coconut,
    /// <summary>a pineapple, starts a leap</summary>
    Pineapple
}
