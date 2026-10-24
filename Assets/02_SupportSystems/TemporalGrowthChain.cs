using UnityEngine;

public class TemporalGrowthChain : MonoBehaviour
{
    public TimeTarget shadeTree;
    public int exposedState = 2;
    public TimeTarget sunPlant;
    public int grownState = 1;
    public Transform pushedRock;
    public Vector3 rockOffset = new Vector3(3f, 0f, 0f);
    public Light sunlight;
    public float growthSeconds = 3f;
    public bool HasTriggered { get; private set; }
    public bool IsComplete => sunPlant != null && sunPlant.IsAtState(grownState);
    private Vector3 rockStart;
    private float fullIntensity;
    private float remainingGrowth;
    private float growthRate;
    private bool cached;

    private void OnEnable()
    {
        if (!cached)
        {
            rockStart = pushedRock != null ? pushedRock.localPosition : Vector3.zero;
            fullIntensity = sunlight != null ? sunlight.intensity : 1f;
            cached = true;
        }
        if (shadeTree != null) shadeTree.OnAgeChanged += ApplyShade;
        if (sunPlant != null) sunPlant.OnAgeChanged += ApplyPlant;
        ApplyShade(shadeTree);
        ApplyPlant(sunPlant);
    }
    private void Start()
    {
        if (!HasTriggered && sunPlant != null) sunPlant.SetInteractionLocked(true, "This plant needs sunlight. Examine what shades it.");
        ApplyShade(shadeTree); ApplyPlant(sunPlant);
    }
    private void OnDisable()
    {
        if (shadeTree != null) shadeTree.OnAgeChanged -= ApplyShade;
        if (sunPlant != null) sunPlant.OnAgeChanged -= ApplyPlant;
    }
    private void ApplyShade(TimeTarget changed)
    {
        bool exposed = shadeTree != null && shadeTree.IsAtState(exposedState);
        if (sunlight != null) sunlight.intensity = exposed ? fullIntensity : 0f;
        if (!exposed || HasTriggered || sunPlant == null || grownState >= sunPlant.StateCount) return;
        HasTriggered = true;
        sunPlant.SetInteractionLocked(false);
        // A finite allowance prevents rewinding the plant from farming repeated natural growth.
        remainingGrowth = Mathf.Max(0f, grownState - sunPlant.AgePosition);
        growthRate = remainingGrowth / Mathf.Max(.01f, growthSeconds);
    }
    private void Update()
    {
        if (!HasTriggered || sunPlant == null || remainingGrowth <= 0f) return;
        float distance = Mathf.Max(0f, grownState - sunPlant.AgePosition);
        if (distance == 0f) { remainingGrowth = 0f; return; }
        float step = Mathf.Min(distance, Mathf.Min(remainingGrowth, growthRate * Time.deltaTime));
        remainingGrowth = Mathf.Max(0f, remainingGrowth - sunPlant.ShiftAge(step));
    }
    private void ApplyPlant(TimeTarget changed)
    {
        if (pushedRock != null && sunPlant != null)
            pushedRock.localPosition = rockStart + rockOffset * Mathf.Clamp01(sunPlant.AgePosition / Mathf.Max(1, grownState));
    }
}
