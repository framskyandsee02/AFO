using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>One-shot forest payoff. The bridge is latched independently of later tree age.</summary>
public sealed class ForestKeystoneRestoration : MonoBehaviour
{
    public PuzzleCondition puzzle;
    public GameObject rootBridge;
    public GameObject city;
    public GameObject[] sickTrees = new GameObject[0];
    public GameObject[] healthyTrees = new GameObject[0];
    public Volume volume;
    public Light sun;
    public float duration = 5f;
    public bool IsRestored { get; private set; }
    private VolumeProfile runtimeProfile, originalProfile;
    private ColorAdjustments color;
    private Color fogBefore, lightBefore;
    private float densityBefore, saturationBefore;
    private bool fogEnabledBefore;
    private bool atmosphereCaptured;
    private Coroutine routine;
    private Vector3 finalBridgeScale;
    private float restoreElapsed;
    private bool completed;
    private void Awake()
    {
        fogBefore = RenderSettings.fogColor; densityBefore = RenderSettings.fogDensity;
        fogEnabledBefore = RenderSettings.fog; if (sun != null) lightBefore = sun.color;
        atmosphereCaptured = true;
        finalBridgeScale = rootBridge != null ? rootBridge.transform.localScale : Vector3.one;
        if (volume != null && volume.sharedProfile != null)
        {
            originalProfile = volume.sharedProfile; runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.hideFlags = HideFlags.DontSave;
            foreach (var component in originalProfile.components)
                if (component != null) runtimeProfile.components.Add(Instantiate(component));
            volume.profile = runtimeProfile;
            if (runtimeProfile.TryGet(out color)) saturationBefore = color.saturation.value;
        }
    }
    private void OnEnable()
    {
        if (puzzle != null) puzzle.OnSolved += OnSolved;
        if (IsRestored)
        {
            atmosphereCaptured = true;
            if (completed) ApplyProgress(1f);
            else routine = StartCoroutine(Restore());
        }
    }
    private void Start()
    {
        if (!IsRestored)
        {
            if (rootBridge != null) rootBridge.SetActive(false);
            foreach (var tree in healthyTrees) if (tree != null) tree.SetActive(false);
        }
        if (puzzle != null && puzzle.IsSolved) OnSolved(puzzle);
    }
    private void OnSolved(PuzzleCondition value)
    {
        if (IsRestored) return; IsRestored = true;
        if (isActiveAndEnabled) routine = StartCoroutine(Restore());
    }
    private IEnumerator Restore()
    {
        if (city != null) city.SetActive(true);
        if (rootBridge != null) rootBridge.SetActive(true);
        while (restoreElapsed < Mathf.Max(0.1f, duration))
        {
            restoreElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(restoreElapsed / Mathf.Max(0.1f, duration)));
            ApplyProgress(t);
            yield return null;
        }
        ApplyProgress(1f); completed = true;
        GameState.Instance?.UnlockZone(2); GameState.Instance?.RestoreZone(1);
        HintTextManager.Instance?.ShowHint("The forest breathes again. The roots remember the way upstream.", 6f);
        routine = null;
    }
    private void ApplyProgress(float t)
    {
        if (rootBridge != null) rootBridge.transform.localScale = new Vector3(finalBridgeScale.x, finalBridgeScale.y, Mathf.Lerp(.01f, finalBridgeScale.z, t));
        RenderSettings.fogColor = Color.Lerp(fogBefore, new Color(.61f,.70f,.65f), t);
        RenderSettings.fogDensity = Mathf.Lerp(densityBefore, densityBefore * .55f, t);
        if (sun != null) sun.color = Color.Lerp(lightBefore, new Color(1f,.91f,.74f), t);
        if (color != null) color.saturation.Override(Mathf.Lerp(saturationBefore, 4f, t));
        int count = Mathf.Min(sickTrees.Length, healthyTrees.Length);
        int restoredCount = Mathf.FloorToInt(t * count);
        for (int i = 0; i < count; i++)
        {
            if (sickTrees[i] != null) sickTrees[i].SetActive(i >= restoredCount);
            if (healthyTrees[i] != null) healthyTrees[i].SetActive(i < restoredCount);
        }
    }
    private void OnDisable()
    {
        if (puzzle != null) puzzle.OnSolved -= OnSolved;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (atmosphereCaptured)
        {
            RenderSettings.fog = fogEnabledBefore; RenderSettings.fogColor = fogBefore; RenderSettings.fogDensity = densityBefore;
            if (sun != null) sun.color = lightBefore;
            atmosphereCaptured = false;
        }
    }
    private void OnDestroy()
    {
        if (volume != null && runtimeProfile != null) { volume.profile = null; volume.sharedProfile = originalProfile; }
        if (runtimeProfile != null)
        {
            foreach (var component in runtimeProfile.components) if (component != null) Destroy(component);
            Destroy(runtimeProfile);
        }
    }
}
