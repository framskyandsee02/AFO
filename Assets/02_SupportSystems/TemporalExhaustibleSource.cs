using UnityEngine;

/// <summary>An explicitly optional source that cannot be recovered after complete extraction.</summary>
public sealed class TemporalExhaustibleSource : MonoBehaviour
{
    public TimeTarget target;
    public GameObject optionalSupport;
    public bool IsExhausted { get; private set; }
    private void OnEnable() { if (target != null) target.OnAgeChanged += Refresh; }
    private void OnDisable() { if (target != null) target.OnAgeChanged -= Refresh; }
    private void Start() { if (target != null) Refresh(target); }
    private void Refresh(TimeTarget value)
    {
        if (IsExhausted || !value.IsAtState(value.StateCount - 1)) return;
        IsExhausted = true; value.SetInteractionLocked(true, "This optional source is spent. Find another source.");
        if (optionalSupport != null) optionalSupport.SetActive(false);
        HintTextManager.Instance?.ShowHint("That support is spent. Another source remains elsewhere; the main route is safe.", 5f);
    }
}
