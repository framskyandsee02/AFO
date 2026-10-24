using System;
using UnityEngine;

public class TemporalOptionalChoice : MonoBehaviour
{
    public TimeTarget[] routes = new TimeTarget[0];
    public int requiredState;
    public GameObject[] rewards = new GameObject[0];
    public GameObject remainingBrace;
    public int SelectedIndex { get; private set; } = -1;
    public bool IsCommitted => SelectedIndex >= 0;
    public event Action<int> OnSelected;
    private bool started;

    private void OnEnable()
    {
        foreach (var route in routes) if (route != null) route.OnAgeChanged += Apply;
        if (started) Apply(null);
    }
    private void Start() { started = true; Apply(null); }
    private void OnDisable()
    {
        foreach (var route in routes) if (route != null) route.OnAgeChanged -= Apply;
    }
    private void Apply(TimeTarget changed)
    {
        if (!started) return;
        if (!IsCommitted)
        {
            for (int i = 0; i < routes.Length; i++)
            {
                if (routes[i] == null || !routes[i].IsAtState(requiredState)) continue;
                SelectedIndex = i;
                break;
            }
            if (IsCommitted)
            {
                foreach (var route in routes) if (route != null) route.SetInteractionLocked(true);
                if (remainingBrace != null) remainingBrace.SetActive(false);
                ApplyRewards();
                OnSelected?.Invoke(SelectedIndex);
                return;
            }
        }
        ApplyRewards();
    }
    private void ApplyRewards()
    {
        for (int i = 0; i < rewards.Length; i++)
            if (rewards[i] != null) rewards[i].SetActive(i == SelectedIndex);
    }
}
