using UnityEngine;

public class TemporalLifeSwitch : MonoBehaviour
{
    public TimeTarget target;
    public int activeState = 1;
    public Light pathLight;
    public GameObject hiddenPassageBarrier;
    public bool revealPassageWhenActive = true;
    public bool IsActive => target != null && target.IsAtState(activeState);
    private float fullIntensity;
    private bool cached;

    private void OnEnable()
    {
        if (!cached) { fullIntensity = pathLight != null ? pathLight.intensity : 1f; cached = true; }
        if (target != null) target.OnAgeChanged += Apply;
        Apply(target);
    }
    private void Start() => Apply(target);
    private void OnDisable() { if (target != null) target.OnAgeChanged -= Apply; }
    private void Apply(TimeTarget changed)
    {
        if (target == null) return;
        if (pathLight != null)
            pathLight.intensity = fullIntensity * Mathf.Clamp01(1f - Mathf.Abs(target.AgePosition - activeState));
        // Passage collisions change only at authored endpoints, never midway through aging.
        if (hiddenPassageBarrier != null && target.IsAtState(Mathf.RoundToInt(target.AgePosition)))
            hiddenPassageBarrier.SetActive(revealPassageWhenActive ? !IsActive : IsActive);
    }
}
