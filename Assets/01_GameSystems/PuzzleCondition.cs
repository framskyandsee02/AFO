using System;
using UnityEngine;

/// <summary>
/// AGENT 01 — Watches a set of TimeTargets and fires once they all sit at
/// their required states. One of these per puzzle; ZoneUnlock and
/// RestorationController subscribe to it.
/// </summary>
public class PuzzleCondition : MonoBehaviour
{
    [Serializable]
    public class Requirement
    {
        public TimeTarget target;
        [Tooltip("State index this target must be in.")]
        public int requiredStateIndex;
    }

    [Header("Identity")]
    [SerializeField] private string puzzleId = "puzzle_01";

    [Header("Requirements (all must match)")]
    [SerializeField] private Requirement[] requirements = new Requirement[0];

    [Header("Behaviour")]
    [Tooltip("Once solved, stop re-evaluating so the payoff cannot be undone.")]
    [SerializeField] private bool latchWhenSolved = true;

    private bool isSolved;

    /// <summary>Fired once (or every time, if latch is off) when all requirements match.</summary>
    public event Action<PuzzleCondition> OnSolved;

    /// <summary>Fired as (matched, total) whenever progress changes — for hint/HUD display.</summary>
    public event Action<int, int> OnProgressChanged;

    public string PuzzleId => puzzleId;
    public bool IsSolved => isSolved;
    public int RequirementCount => requirements.Length;

    private void OnEnable()
    {
        foreach (var req in requirements)
            if (req != null && req.target != null) req.target.OnAgeChanged += HandleAgeChanged;
    }

    private void OnDisable()
    {
        foreach (var req in requirements)
            if (req != null && req.target != null) req.target.OnAgeChanged -= HandleAgeChanged;
    }

    private void Start() => Evaluate();

    private void HandleAgeChanged(TimeTarget target) => Evaluate();

    private void Evaluate()
    {
        if (isSolved && latchWhenSolved) return;

        int matched = 0;
        foreach (var req in requirements)
        {
            if (req == null || req.target == null) continue;
            if (req.target.IsAtState(req.requiredStateIndex)) matched++;
        }

        OnProgressChanged?.Invoke(matched, requirements.Length);

        bool nowSolved = requirements.Length > 0 && matched == requirements.Length;
        if (nowSolved && !isSolved)
        {
            isSolved = true;
            GameState.Instance?.MarkPuzzleSolved(puzzleId);
            OnSolved?.Invoke(this);
        }
        else if (!nowSolved && !latchWhenSolved)
        {
            isSolved = false;
        }
    }

    /// <summary>Progress as 0..1, for HUD bars.</summary>
    public float GetProgressNormalized()
    {
        if (requirements.Length == 0) return 0f;
        int matched = 0;
        foreach (var req in requirements)
            if (req != null && req.target != null && req.target.IsAtState(req.requiredStateIndex)) matched++;
        return (float)matched / requirements.Length;
    }
}
