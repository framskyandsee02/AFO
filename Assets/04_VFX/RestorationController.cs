using System.Collections;
using UnityEngine;

/// <summary>
/// AGENT 04 — The payoff. When a puzzle resolves, swaps the zone from its
/// polluted dressing to its restored one and blends the atmosphere across.
/// </summary>
public class RestorationController : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PuzzleCondition triggerPuzzle;

    [Header("Visual Swap")]
    [Tooltip("Dressing shown before restoration (dead props, haze, debris).")]
    [SerializeField] private GameObject pollutedDressing;

    [Tooltip("Dressing shown after restoration (greenery, clear water).")]
    [SerializeField] private GameObject restoredDressing;

    [Header("Atmosphere Blend")]
    [SerializeField] private bool blendAtmosphere = true;
    [SerializeField] private float blendDuration = 2.5f;
    [SerializeField] private Color pollutedFogColor = new Color(0.45f, 0.42f, 0.35f);
    [SerializeField] private Color restoredFogColor = new Color(0.62f, 0.76f, 0.82f);
    [SerializeField] private float pollutedFogDensity = 0.035f;
    [SerializeField] private float restoredFogDensity = 0.012f;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem restorationVFX;
    [SerializeField, TextArea] private string restorationMessage = "The land breathes again.";

    private bool isRestored;
    private Coroutine blendRoutine;
    private bool previousFogEnabled;
    private Color previousFogColor;
    private float previousFogDensity;
    private bool fogCaptured;
    [SerializeField] private int zoneId = 1;
    [SerializeField] private Light atmosphereLight;
    [SerializeField] private Color pollutedLightColor = new Color(0.68f, 0.74f, 0.82f);
    [SerializeField] private Color restoredLightColor = new Color(1f, 0.91f, 0.72f);
    private Color previousLightColor;
    private float previousLightIntensity;

    public bool IsRestored => isRestored;

    private void OnEnable()
    {
        previousFogEnabled = RenderSettings.fog;
        previousFogColor = RenderSettings.fogColor;
        previousFogDensity = RenderSettings.fogDensity;
        fogCaptured = true;
        if (atmosphereLight != null)
        {
            previousLightColor = atmosphereLight.color;
            previousLightIntensity = atmosphereLight.intensity;
        }
        if (triggerPuzzle != null) triggerPuzzle.OnSolved += HandlePuzzleSolved;
    }

    private void OnDisable()
    {
        if (triggerPuzzle != null) triggerPuzzle.OnSolved -= HandlePuzzleSolved;
        if (blendRoutine != null) StopCoroutine(blendRoutine);
        blendRoutine = null;
        if (fogCaptured)
        {
            RenderSettings.fog = previousFogEnabled;
            RenderSettings.fogColor = previousFogColor;
            RenderSettings.fogDensity = previousFogDensity;
            fogCaptured = false;
            if (atmosphereLight != null)
            {
                atmosphereLight.color = previousLightColor;
                atmosphereLight.intensity = previousLightIntensity;
            }
        }
    }

    private void Start()
    {
        ApplyPollutedImmediate();
        if (triggerPuzzle != null && triggerPuzzle.IsSolved) Restore();
    }

    private void HandlePuzzleSolved(PuzzleCondition puzzle) => Restore();

    private void ApplyPollutedImmediate()
    {
        if (pollutedDressing != null) pollutedDressing.SetActive(true);
        if (restoredDressing != null) restoredDressing.SetActive(false);

        if (blendAtmosphere)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = pollutedFogColor;
            RenderSettings.fogDensity = pollutedFogDensity;
            if (atmosphereLight != null) atmosphereLight.color = pollutedLightColor;
        }
    }

    public void Restore()
    {
        if (isRestored) return;
        isRestored = true;

        if (pollutedDressing != null) pollutedDressing.SetActive(false);
        if (restoredDressing != null) restoredDressing.SetActive(true);

        if (restorationVFX != null) restorationVFX.Play();
        HintTextManager.Instance?.ShowHint(restorationMessage, 4f);

        if (blendAtmosphere)
        {
            if (blendRoutine != null) StopCoroutine(blendRoutine);
            blendRoutine = StartCoroutine(BlendAtmosphere());
        }
        else GameState.Instance?.RestoreZone(zoneId);
    }

    private IEnumerator BlendAtmosphere()
    {
        Color startColor = RenderSettings.fogColor;
        float startDensity = RenderSettings.fogDensity;
        Color startLightColor = atmosphereLight != null ? atmosphereLight.color : Color.white;
        float elapsed = 0f;

        while (elapsed < blendDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / blendDuration);
            RenderSettings.fogColor = Color.Lerp(startColor, restoredFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, restoredFogDensity, t);
            if (atmosphereLight != null) atmosphereLight.color = Color.Lerp(startLightColor, restoredLightColor, t);
            yield return null;
        }

        RenderSettings.fogColor = restoredFogColor;
        RenderSettings.fogDensity = restoredFogDensity;
        GameState.Instance?.RestoreZone(zoneId);
        blendRoutine = null;
    }
}
