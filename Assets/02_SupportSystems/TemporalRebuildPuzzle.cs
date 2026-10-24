using UnityEngine;

/// <summary>Repairing a bad alignment directly leaves a short span. Collapse first to realign.</summary>
public sealed class TemporalRebuildPuzzle : MonoBehaviour
{
    public TimeTarget target;
    public Transform deck;
    public GameObject reward;
    public float shortLength = 2.5f, repairedLength = 10f;
    public bool HasCollapsed { get; private set; }
    public bool IsSolved { get; private set; }
    private void OnEnable() { if (target != null) target.OnAgeChanged += Refresh; }
    private void OnDisable() { if (target != null) target.OnAgeChanged -= Refresh; }
    private void Start() { if (reward != null) reward.SetActive(false); if (target != null) Refresh(target); }
    private void Refresh(TimeTarget value)
    {
        if (value.IsAtState(value.StateCount - 1)) HasCollapsed = true;
        float alignment = HasCollapsed ? 1f - Mathf.Clamp01(value.AgePosition) : 0f;
        if (deck != null)
        {
            deck.gameObject.SetActive(value.AgePosition < value.StateCount - 1);
            deck.localScale = new Vector3(2.5f, .25f, Mathf.Lerp(shortLength, repairedLength, alignment));
            deck.localRotation = Quaternion.Euler(0, HasCollapsed ? Mathf.Lerp(35f, 0f, alignment) : 0f, 0);
        }
        if (!IsSolved && HasCollapsed && value.IsAtState(0))
        { IsSolved = true; if (reward != null) reward.SetActive(true); }
    }
}
