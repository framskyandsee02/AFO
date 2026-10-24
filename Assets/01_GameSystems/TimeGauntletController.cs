using System;
using UnityEngine;

/// <summary>Held E collects time by ageing; held Q spends it to rejuvenate. Aim stays camera-centred.</summary>
public class TimeGauntletController : MonoBehaviour
{
    public static TimeGauntletController Instance { get; private set; }
    [SerializeField] private Camera aimCamera;
    [SerializeField] private float aimRange = 12f;
    [SerializeField] private LayerMask targetMask = ~0;
    [Header("Channel timing")]
    [SerializeField, Min(0.1f)] private float secondsPerState = 3f;
    [Tooltip("Small pause at an exact endpoint, allowing release on Mature.")]
    [SerializeField] private float useCooldown = 0.25f;
    [SerializeField] private ParticleSystem rewindVFX;
    [SerializeField] private ParticleSystem accelerateVFX;
    private TimeTarget currentTarget;
    private float nextUseTime;
    private float nextFeedbackTime;
    private float nextRejectTime;
    private string lastRejection;
    public event Action<TimeTarget> OnTargetChanged;
    public event Action<TimeTarget, int> OnGauntletUsed;
    public event Action<string> OnUseRejected;
    public TimeTarget CurrentTarget => currentTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (aimCamera == null) aimCamera = Camera.main;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void Update() => UpdateAim();
    private void UpdateAim()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        TimeTarget found = null;
        if (aimCamera != null)
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, aimRange, targetMask, QueryTriggerInteraction.Ignore))
                found = hit.collider.GetComponentInParent<TimeTarget>();
        }
        if (found != currentTarget)
        {
            currentTarget = found;
            nextUseTime = 0f;
            OnTargetChanged?.Invoke(currentTarget);
        }
    }
    public void RequestRewind() => RequestUse(-1);
    public void RequestAccelerate() => RequestUse(1);
    private void Reject(string reason, bool insufficient = false)
    {
        if (reason == lastRejection && Time.unscaledTime < nextRejectTime) return;
        lastRejection = reason;
        nextRejectTime = Time.unscaledTime + 1f;
        if (insufficient && EnergyManager.Instance != null)
            EnergyManager.Instance.TrySpend(0.01f);
        OnUseRejected?.Invoke(reason);
    }
    private void RequestUse(int direction)
    {
        // Re-read the camera ray here: input and aim Update order is unspecified.
        UpdateAim();
        if (Time.unscaledTime < nextUseTime || Time.deltaTime <= 0f) return;
        if (currentTarget == null) { Reject("Aim at an object to transfer time."); return; }
        float applied = ChannelTarget(currentTarget, direction, Time.deltaTime);
        if (applied == 0f) return;
        if (currentTarget.IsAtState(Mathf.RoundToInt(currentTarget.AgePosition)))
            nextUseTime = Time.unscaledTime + useCooldown;
        if (Time.unscaledTime >= nextFeedbackTime)
        {
            var vfx = direction < 0 ? rewindVFX : accelerateVFX;
            if (vfx != null) { vfx.transform.position = currentTarget.transform.position; vfx.Play(); }
            nextFeedbackTime = Time.unscaledTime + Mathf.Max(0.25f, useCooldown);
            OnGauntletUsed?.Invoke(currentTarget, direction);
        }
    }
    // Single atomic step shared by normal hold input and deterministic runtime checks.
    private float ChannelTarget(TimeTarget target, int direction, float deltaTime)
    {
        if (target == null || deltaTime <= 0f) return 0f;
        if (!target.IsUnlocked()) { Reject(target.GetGatedHint()); return 0f; }
        if (direction < 0 ? !target.CanRewind() : !target.CanAccelerate())
        {
            Reject(direction < 0 ? "This object is already at its youngest age." : "No more time can be extracted from this object.");
            return 0f;
        }
        var energy = EnergyManager.Instance;
        if (energy == null) { Reject("Time storage is unavailable."); return 0f; }
        float rate = Mathf.Max(0.01f, target.EnergyCostPerChange);
        float available = direction < 0 ? energy.CurrentEnergy : energy.MaxEnergy - energy.CurrentEnergy;
        if (available <= 0.00001f)
        {
            Reject(direction < 0 ? "Not enough energy. Hold E on a younger object first." : "Time storage is full. Hold Q to restore something.", direction < 0);
            return 0f;
        }
        float step = Mathf.Min(deltaTime / Mathf.Max(0.1f, secondsPerState),
            target.DistanceToNextState(direction), available / rate);
        float applied = target.ShiftAge(direction * step, energy);
        if (applied == 0f) return 0f;
        lastRejection = null;
        return applied;
    }
}
