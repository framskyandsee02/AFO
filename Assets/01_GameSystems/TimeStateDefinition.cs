using UnityEngine;

/// <summary>
/// AGENT 01 — Data definition for one discrete time state of a TimeTarget.
/// Index-based so Tree (Young/Mature/Old) and Bridge (Intact/Worn/Broken)
/// share the same machinery with different semantics.
/// </summary>
[System.Serializable]
public class TimeStateDefinition
{
    [Tooltip("Display name shown in hints/debug, e.g. 'Young', 'Broken'")]
    public string stateName = "State";

    [Tooltip("Visual root enabled while this state is active. Children of the TimeTarget.")]
    public GameObject visualRoot;

    [Header("Gameplay Flags")]
    [Tooltip("Can the player walk across/through this object in this state? (bridges, vines)")]
    public bool isTraversable = false;

    [Tooltip("Does this state count as 'healthy/restored' for puzzle checks?")]
    public bool isHealthy = false;

    [Tooltip("Optional collider enabled only in this state.")]
    public Collider stateCollider;
}
