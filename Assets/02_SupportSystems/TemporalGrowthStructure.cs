using UnityEngine;

/// <summary>Keep the climbable region at the actual height of a continuously growing structure.</summary>
public sealed class TemporalGrowthStructure : MonoBehaviour
{
    public TimeTarget target;
    public TemporalClimbVolume climbVolume;
    public float maximumHeight = 6f;
    private void OnEnable() { if (target != null) target.OnAgeChanged += Refresh; }
    private void OnDisable() { if (target != null) target.OnAgeChanged -= Refresh; }
    private void Start() { if (target != null) Refresh(target); }
    private void Refresh(TimeTarget value)
    {
        if (climbVolume == null) return;
        float progress = value.StateCount > 1 ? Mathf.Clamp01(value.AgePosition / (value.StateCount - 1)) : 0f;
        float height = Mathf.Lerp(.15f, maximumHeight, progress);
        var box = climbVolume.GetComponent<BoxCollider>();
        box.center = new Vector3(0, (height + 1.5f) * .5f, 0);
        box.size = new Vector3(1.5f, height + 1.5f, 1.5f);
        box.enabled = height > .75f;
    }
}
