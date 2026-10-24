using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AGENT 02 — Progression record: which zones are open, which puzzles are done.
/// Energy lives in EnergyManager; this class deliberately does not duplicate it.
/// No save system in the MVP — this is in-memory only.
/// </summary>
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("Zone ids unlocked when the game starts. Zone 0 is the opening area.")]
    [SerializeField] private int[] initiallyUnlockedZones = new int[] { 0 };

    private readonly HashSet<int> unlockedZones = new HashSet<int>();
    private readonly HashSet<int> restoredZones = new HashSet<int>();
    private readonly HashSet<string> solvedPuzzles = new HashSet<string>();

    /// <summary>Fired with the zone id when a zone opens.</summary>
    public event Action<int> OnZoneUnlocked;

    /// <summary>Fired with the zone id when a zone's restoration completes.</summary>
    public event Action<int> OnZoneRestored;

    /// <summary>Fired with the puzzle id when a puzzle resolves.</summary>
    public event Action<string> OnPuzzleSolved;

    public int UnlockedZoneCount => unlockedZones.Count;
    public int RestoredZoneCount => restoredZones.Count;
    public int SolvedPuzzleCount => solvedPuzzles.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (int zone in initiallyUnlockedZones)
            unlockedZones.Add(zone);
    }

    public void UnlockZone(int zoneId)
    {
        if (!unlockedZones.Add(zoneId)) return;
        OnZoneUnlocked?.Invoke(zoneId);
    }

    public void RestoreZone(int zoneId)
    {
        if (!restoredZones.Add(zoneId)) return;
        OnZoneRestored?.Invoke(zoneId);
    }

    public void MarkPuzzleSolved(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId)) return;
        if (!solvedPuzzles.Add(puzzleId)) return;
        OnPuzzleSolved?.Invoke(puzzleId);
    }

    public bool IsZoneUnlocked(int zoneId) => unlockedZones.Contains(zoneId);
    public bool IsZoneRestored(int zoneId) => restoredZones.Contains(zoneId);
    public bool IsPuzzleSolved(string puzzleId) => solvedPuzzles.Contains(puzzleId);
}
