using System;
using UnityEngine;

/// <summary>Stored time: collect by ageing (E), spend to rejuvenate (Q). No passive regeneration.</summary>
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }
    [SerializeField, Min(0.01f)] private float maxEnergy = 100f;
    [SerializeField, Min(0f)] private float startingEnergy = 0f;
    private float currentEnergy;
    public event Action<float, float> OnEnergyChanged;
    public event Action OnInsufficientEnergy;
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float Normalized => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
    }
    private void Start() => OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public bool HasEnergy(float amount) => amount <= currentEnergy;
    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (amount > currentEnergy + 0.00001f) { OnInsufficientEnergy?.Invoke(); return false; }
        currentEnergy = Mathf.Max(0f, currentEnergy - amount);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        return true;
    }
    public void Refill(float amount)
    {
        if (amount <= 0f) return;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
    // Debug/setup API only; gameplay collects time from an actual target.
    public void RefillFull()
    {
        currentEnergy = maxEnergy;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
    private void OnValidate()
    {
        maxEnergy = Mathf.Max(0.01f, maxEnergy);
        startingEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
    }
}
