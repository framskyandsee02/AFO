using System;
using UnityEngine;

/// <summary>
/// AGENT 01 — The gate. Listens to a PuzzleCondition and opens a zone when
/// it resolves: disables a barrier, enables the new area, notifies GameState.
/// </summary>
public class ZoneUnlock : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PuzzleCondition requiredPuzzle;

    [Header("Zone")]
    [SerializeField] private int zoneId = 1;

    [Tooltip("Objects disabled when the zone unlocks (walls, debris, collapsed bridge).")]
    [SerializeField] private GameObject[] barriersToRemove = new GameObject[0];

    [Tooltip("Objects enabled when the zone unlocks (the new area, its props).")]
    [SerializeField] private GameObject[] contentToReveal = new GameObject[0];

    [Header("Feedback")]
    [SerializeField] private ParticleSystem unlockVFX;
    [SerializeField, TextArea] private string unlockMessage = "The path ahead has opened.";

    private bool isUnlocked;

    /// <summary>Fired as (zoneId) when this gate opens.</summary>
    public event Action<int> OnZoneUnlocked;

    public bool IsUnlocked => isUnlocked;
    public int ZoneId => zoneId;

    private void OnEnable()
    {
        if (requiredPuzzle != null) requiredPuzzle.OnSolved += HandlePuzzleSolved;
    }

    private void OnDisable()
    {
        if (requiredPuzzle != null) requiredPuzzle.OnSolved -= HandlePuzzleSolved;
    }

    private void Start()
    {
        // Content stays hidden until the gate opens.
        foreach (var go in contentToReveal)
            if (go != null) go.SetActive(false);

        // Already solved before this object woke up (load order, debug).
        if (requiredPuzzle != null && requiredPuzzle.IsSolved) Unlock();
    }

    private void HandlePuzzleSolved(PuzzleCondition puzzle) => Unlock();

    public void Unlock()
    {
        if (isUnlocked) return;
        isUnlocked = true;

        foreach (var go in barriersToRemove)
            if (go != null) go.SetActive(false);

        foreach (var go in contentToReveal)
            if (go != null) go.SetActive(true);

        if (unlockVFX != null) unlockVFX.Play();

        GameState.Instance?.UnlockZone(zoneId);
        HintTextManager.Instance?.ShowHint(unlockMessage, 4f);

        OnZoneUnlocked?.Invoke(zoneId);
    }
}
