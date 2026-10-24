using UnityEngine;

public class TemporalWaterPuzzle : MonoBehaviour
{
    public TimeTarget floodgate;
    public Transform water;
    public Transform floatingLog;
    public GameObject floodedBarrier;
    public float drySurface = -1.3f;
    public float fullSurface = .35f;
    public float FillAmount { get; private set; }
    private float logOffset;
    private bool cached;

    private void OnEnable()
    {
        if (!cached)
        {
            logOffset = floatingLog != null ? floatingLog.position.y - fullSurface : 0f;
            cached = true;
        }
        if (floodgate != null) floodgate.OnAgeChanged += Apply;
        Apply(floodgate);
    }
    private void Start() => Apply(floodgate);
    private void OnDisable() { if (floodgate != null) floodgate.OnAgeChanged -= Apply; }
    private void Apply(TimeTarget changed)
    {
        if (floodgate == null || floodgate.StateCount < 2) return;
        FillAmount = 1f - Mathf.Clamp01(floodgate.AgePosition / (floodgate.StateCount - 1));
        float surface = Mathf.Lerp(drySurface, fullSurface, FillAmount);
        if (water != null) { var p = water.position; p.y = surface; water.position = p; water.gameObject.SetActive(FillAmount > .01f); }
        if (floatingLog != null) { var p = floatingLog.position; p.y = surface + logOffset; floatingLog.position = p; }
        if (floodedBarrier != null) floodedBarrier.SetActive(FillAmount > .85f);
    }
}
