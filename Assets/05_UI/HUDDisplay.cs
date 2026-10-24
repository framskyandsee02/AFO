using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AGENT 05 — Reads the systems and shows energy, the aimed-at target and
/// puzzle progress. Event-driven where the systems expose events; every field
/// is optional so a partially dressed Canvas still runs.
/// </summary>
public class HUDDisplay : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField] private Image energyFill;
    [SerializeField] private TextMeshProUGUI energyLabel;

    [Header("Target Readout")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private TextMeshProUGUI targetNameLabel;
    [SerializeField] private TextMeshProUGUI targetStateLabel;

    [Header("Puzzle Progress")]
    [SerializeField] private PuzzleCondition trackedPuzzle;
    [SerializeField] private TextMeshProUGUI puzzleProgressLabel;

    [Header("Crosshair")]
    [SerializeField] private Graphic crosshair;
    [SerializeField] private Color crosshairIdle = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color crosshairActive = new Color(0.45f, 0.9f, 1f, 1f);
    [SerializeField] private Color crosshairGated = new Color(1f, 0.55f, 0.35f, 1f);
    private EnergyManager observedEnergy;
    private TimeGauntletController observedGauntlet;
    private TimeTarget observedTarget;
    private int observedState = -1;
    private bool observedUnlocked;
    private float observedAge = -1f;

    private void OnEnable()
    {
        BindSystems();

        if (trackedPuzzle != null)
            trackedPuzzle.OnProgressChanged += HandlePuzzleProgress;

        HandleTargetChanged(null);
    }

    private void OnDisable()
    {
        if (observedEnergy != null) observedEnergy.OnEnergyChanged -= HandleEnergyChanged;
        if (observedGauntlet != null) observedGauntlet.OnTargetChanged -= HandleTargetChanged;
        observedEnergy = null;
        observedGauntlet = null;

        if (trackedPuzzle != null)
            trackedPuzzle.OnProgressChanged -= HandlePuzzleProgress;
    }

    private void Start()
    {
        BindSystems();
        if (trackedPuzzle != null)
            HandlePuzzleProgress(Mathf.RoundToInt(trackedPuzzle.GetProgressNormalized() * trackedPuzzle.RequirementCount), trackedPuzzle.RequirementCount);
    }

    private void BindSystems()
    {
        if (observedEnergy != EnergyManager.Instance)
        {
            if (observedEnergy != null) observedEnergy.OnEnergyChanged -= HandleEnergyChanged;
            observedEnergy = EnergyManager.Instance;
            if (observedEnergy != null)
            {
                observedEnergy.OnEnergyChanged += HandleEnergyChanged;
                HandleEnergyChanged(observedEnergy.CurrentEnergy, observedEnergy.MaxEnergy);
            }
        }
        if (observedGauntlet != TimeGauntletController.Instance)
        {
            if (observedGauntlet != null) observedGauntlet.OnTargetChanged -= HandleTargetChanged;
            observedGauntlet = TimeGauntletController.Instance;
            if (observedGauntlet != null)
            {
                observedGauntlet.OnTargetChanged += HandleTargetChanged;
                HandleTargetChanged(observedGauntlet.CurrentTarget);
            }
        }
    }

    private void Update()
    {
        BindSystems();
        // A state/prerequisite can change while the aim remains on the same target.
        if (observedTarget != null && (observedAge != observedTarget.AgePosition || observedState != observedTarget.CurrentStateIndex
            || observedUnlocked != observedTarget.IsUnlocked())) HandleTargetChanged(observedTarget);
    }

    private void HandleEnergyChanged(float current, float max)
    {
        if (energyFill != null) energyFill.fillAmount = max > 0f ? current / max : 0f;
        if (energyLabel != null) energyLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void HandleTargetChanged(TimeTarget target)
    {
        observedTarget = target;
        observedAge = target != null ? target.AgePosition : -1f;
        observedState = target != null ? target.CurrentStateIndex : -1;
        observedUnlocked = target != null && target.IsUnlocked();
        bool hasTarget = target != null;

        if (targetPanel != null) targetPanel.SetActive(hasTarget);

        if (hasTarget)
        {
            if (targetNameLabel != null) targetNameLabel.text = target.displayName;
            if (targetStateLabel != null) targetStateLabel.text = target.IsUnlocked()
                ? target.CurrentStateName
                : "Locked";
        }

        if (crosshair != null)
            crosshair.color = !hasTarget ? crosshairIdle
                            : target.IsUnlocked() ? crosshairActive
                            : crosshairGated;
    }

    private void HandlePuzzleProgress(int matched, int total)
    {
        if (puzzleProgressLabel == null) return;
        puzzleProgressLabel.text = total > 0 ? $"{matched} / {total}" : string.Empty;
    }
}
