using UnityEngine;

/// <summary>Optional one-shot audio. Empty clip slots deliberately remain silent.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class AudioCueManager : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private TimeGauntletController gauntlet;
    [SerializeField] private EnergyManager energy;
    [SerializeField] private PuzzleCondition puzzle;
    [SerializeField] private ZoneUnlock zone;
    [SerializeField] private AudioClip rewind;
    [SerializeField] private AudioClip accelerate;
    [SerializeField] private AudioClip rejected;
    [SerializeField] private AudioClip insufficientEnergy;
    [SerializeField] private AudioClip puzzleSolved;
    [SerializeField] private AudioClip zoneUnlocked;
    private bool insufficientThisUse;

    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
    }
    private void OnEnable() => Bind();
    private void Start() { Unbind(); Bind(); }
    private void OnDisable() => Unbind();
    private void Bind()
    {
        if (gauntlet == null) gauntlet = TimeGauntletController.Instance;
        if (energy == null) energy = EnergyManager.Instance;
        if (gauntlet != null)
        {
            gauntlet.OnGauntletUsed += OnUsed;
            gauntlet.OnUseRejected += OnRejected;
        }
        if (energy != null) energy.OnInsufficientEnergy += OnInsufficient;
        if (puzzle != null) puzzle.OnSolved += OnSolved;
        if (zone != null) zone.OnZoneUnlocked += OnUnlocked;
    }
    private void Unbind()
    {
        if (gauntlet != null)
        {
            gauntlet.OnGauntletUsed -= OnUsed;
            gauntlet.OnUseRejected -= OnRejected;
        }
        if (energy != null) energy.OnInsufficientEnergy -= OnInsufficient;
        if (puzzle != null) puzzle.OnSolved -= OnSolved;
        if (zone != null) zone.OnZoneUnlocked -= OnUnlocked;
    }
    private void Play(AudioClip clip)
    {
        if (source != null && clip != null && source.isActiveAndEnabled) source.PlayOneShot(clip);
    }
    private void OnUsed(TimeTarget target, int direction)
    {
        insufficientThisUse = false;
        Play(direction < 0 ? rewind : accelerate);
    }
    private void OnInsufficient() { insufficientThisUse = true; Play(insufficientEnergy); }
    private void OnRejected(string reason)
    {
        if (!insufficientThisUse || !reason.StartsWith("Not enough energy.", System.StringComparison.Ordinal)) Play(rejected);
        insufficientThisUse = false;
    }
    private void OnSolved(PuzzleCondition value) => Play(puzzleSolved);
    private void OnUnlocked(int value) => Play(zoneUnlocked);
}
