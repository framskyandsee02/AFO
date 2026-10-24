using System;
using UnityEngine;

/// <summary>Continuous age between authored endpoints. Greybox transforms/colours interpolate without new models.</summary>
public class TimeTarget : MonoBehaviour
{
    [Header("States (index 0 = earliest)")]
    [SerializeField] private TimeStateDefinition[] states = new TimeStateDefinition[0];
    [SerializeField] private int startingStateIndex;
    public string displayName = "Object";
    [Header("Root-cause gate")]
    [SerializeField] private TimeTarget prerequisite;
    [SerializeField] private int prerequisiteRequiredState;
    [SerializeField] private string gatedHint = "Something else must change first.";
    [Header("Time transferred per complete interval")]
    [SerializeField, Min(0.01f)] private float energyCostPerChange = 10f;
    [SerializeField] private ParticleSystem changeVFX;
    [Tooltip("Disable for authored multi-material models; their original material palette is preserved.")]
    [SerializeField] private bool interpolateRootColour = true;

    private int currentStateIndex;
    private float agePosition;
    private bool interactionLocked;
    private string interactionLockReason;
    private Vector3[] positions;
    private Vector3[] scales;
    private Quaternion[] rotations;
    private Color[] colours;
    private Renderer[][] stateRenderers;
    private MaterialPropertyBlock propertyBlock;
    public event Action<TimeTarget, int, int> OnStateChanged;
    public event Action<TimeTarget> OnAgeChanged;
    public int CurrentStateIndex => currentStateIndex;
    public int StateCount => states == null ? 0 : states.Length;
    public float AgePosition => agePosition;
    public float EnergyCostPerChange => energyCostPerChange;
    public bool IsHealthy => IsAtState(currentStateIndex) && Valid(currentStateIndex) && states[currentStateIndex].isHealthy;
    public bool IsTraversable => Valid(currentStateIndex) && states[currentStateIndex].isTraversable;
    public string CurrentStateName
    {
        get
        {
            if (StateCount == 0) return "—";
            if (IsAtState(Mathf.RoundToInt(agePosition))) return Name(Mathf.RoundToInt(agePosition));
            int lower = Mathf.FloorToInt(agePosition);
            return $"{Name(lower)} → {Name(lower + 1)} ({Mathf.RoundToInt((agePosition - lower) * 100f)}%)";
        }
    }

    private void Awake()
    {
        if (states == null) states = new TimeStateDefinition[0];
        currentStateIndex = Mathf.Clamp(startingStateIndex, 0, Mathf.Max(0, StateCount - 1));
        agePosition = currentStateIndex;
        CacheVisuals();
        ApplyAgeVisuals();
    }
    private bool Valid(int i) => i >= 0 && i < StateCount && states[i] != null;
    private string Name(int i) => Valid(i) ? states[i].stateName : "—";
    public bool IsAtState(int index) => Valid(index) && agePosition == index;
    public bool IsUnlocked() => !interactionLocked && (prerequisite == null || prerequisite.IsAtState(prerequisiteRequiredState));
    public void SetInteractionLocked(bool value, string reason = null) { interactionLocked = value; interactionLockReason = reason; }
    public string GetGatedHint() => interactionLocked ? interactionLockReason ?? "This optional choice is committed. Explore another route." : gatedHint;
    public bool CanRewind() => IsUnlocked() && agePosition > 0f;
    public bool CanAccelerate() => IsUnlocked() && agePosition < StateCount - 1;

    // These endpoint/debug APIs are retained for existing setup tools.
    // Player input uses ShiftAge with a delta proportional to held time.
    public bool Rewind() => SetState(Mathf.Max(0, Mathf.CeilToInt(agePosition) - 1));
    public bool Accelerate() => SetState(Mathf.Min(StateCount - 1, Mathf.FloorToInt(agePosition) + 1));
    public bool SetState(int index)
    {
        if (!IsUnlocked() || !Valid(index) || IsAtState(index)) return false;
        return ShiftAge(index - agePosition) != 0f;
    }
    public float DistanceToNextState(int direction)
    {
        if (direction < 0)
            return agePosition - Mathf.Max(0, Mathf.CeilToInt(agePosition) - 1);
        return Mathf.Min(StateCount - 1, Mathf.FloorToInt(agePosition) + 1) - agePosition;
    }
    public float ShiftAge(float delta, EnergyManager storage = null)
    {
        if (!IsUnlocked() || StateCount == 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return 0f;
        float next = Mathf.Clamp(agePosition + delta, 0f, StateCount - 1);
        float applied = next - agePosition;
        if (applied == 0f) return 0f;
        if (storage != null)
        {
            float amount = Mathf.Abs(applied) * Mathf.Max(0.01f, energyCostPerChange);
            float available = applied < 0f ? storage.CurrentEnergy : storage.MaxEnergy - storage.CurrentEnergy;
            // Floating-point addition must never round a resource-limited step beyond its budget.
            if (amount > available) return 0f;
            if (applied < 0f) { if (!storage.TrySpend(amount)) return 0f; }
            else storage.Refill(amount);
        }
        int previousIndex = currentStateIndex;
        agePosition = next;
        bool endpoint = IsAtState(Mathf.RoundToInt(agePosition));
        if (endpoint) currentStateIndex = Mathf.RoundToInt(agePosition);
        ApplyAgeVisuals();
        OnAgeChanged?.Invoke(this);
        if (endpoint)
        {
            if (changeVFX != null) changeVFX.Play();
            OnStateChanged?.Invoke(this, previousIndex, currentStateIndex);
        }
        return applied;
    }

    private void CacheVisuals()
    {
        positions = new Vector3[StateCount];
        scales = new Vector3[StateCount];
        rotations = new Quaternion[StateCount];
        colours = new Color[StateCount];
        stateRenderers = new Renderer[StateCount][];
        propertyBlock = new MaterialPropertyBlock();
        for (int i = 0; i < StateCount; i++)
        {
            if (!Valid(i) || states[i].visualRoot == null) continue;
            Transform root = states[i].visualRoot.transform;
            positions[i] = root.localPosition;
            scales[i] = root.localScale;
            rotations[i] = root.localRotation;
            stateRenderers[i] = root.GetComponentsInChildren<Renderer>(true);
            var material = stateRenderers[i].Length > 0 ? stateRenderers[i][0].sharedMaterial : null;
            colours[i] = material == null ? Color.white : material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor") : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        }
    }
    private void ApplyAgeVisuals()
    {
        if (positions == null) return;
        int lower = Mathf.Clamp(Mathf.FloorToInt(agePosition), 0, Mathf.Max(0, StateCount - 1));
        int upper = Mathf.Min(lower + 1, StateCount - 1);
        float t = agePosition - lower;
        for (int i = 0; i < StateCount; i++)
        {
            if (!Valid(i)) continue;
            bool active = i == currentStateIndex;
            var root = states[i].visualRoot;
            if (root != null)
            {
                root.transform.localPosition = positions[i];
                root.transform.localRotation = rotations[i];
                root.transform.localScale = scales[i];
                root.SetActive(active);
                if (active)
                {
                    root.transform.localPosition = Vector3.Lerp(positions[lower], positions[upper], t);
                    root.transform.localRotation = Quaternion.Slerp(rotations[lower], rotations[upper], t);
                    root.transform.localScale = Vector3.Lerp(scales[lower], scales[upper], t);
                }
                if (interpolateRootColour && stateRenderers[i] != null)
                    foreach (var renderer in stateRenderers[i])
                    {
                        if (renderer == null) continue;
                        renderer.GetPropertyBlock(propertyBlock);
                        Color colour = active ? Color.Lerp(colours[lower], colours[upper], t) : colours[i];
                        propertyBlock.SetColor("_BaseColor", colour);
                        propertyBlock.SetColor("_Color", colour);
                        renderer.SetPropertyBlock(propertyBlock);
                    }
            }
            if (states[i].stateCollider != null) states[i].stateCollider.enabled = active;
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (states == null) states = new TimeStateDefinition[0];
        startingStateIndex = Mathf.Clamp(startingStateIndex, 0, Mathf.Max(0, StateCount - 1));
        prerequisiteRequiredState = Mathf.Max(0, prerequisiteRequiredState);
        energyCostPerChange = Mathf.Max(0.01f, energyCostPerChange);
    }
#endif
}
