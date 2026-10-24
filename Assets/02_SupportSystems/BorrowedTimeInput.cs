using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AGENT 02 — Input layer for the gauntlet. Reads the new Input System
/// directly off the keyboard so no generated action class is required yet;
/// the action-asset wiring can replace the body of ReadInput() later without
/// touching anything else.
///
/// Hold Q = spend time/rejuvenate · Hold E = collect time/age · F = Scan · R = Undo (stub).
/// </summary>
public class BorrowedTimeInput : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("Leave empty to use TimeGauntletController.Instance.")]
    [SerializeField] private TimeGauntletController gauntlet;

    private void Awake()
    {
        if (gauntlet == null) gauntlet = TimeGauntletController.Instance;
    }

    private void Update()
    {
        if (gauntlet == null)
        {
            gauntlet = TimeGauntletController.Instance;
            if (gauntlet == null) return;
        }

        ReadInput();
    }

    private void ReadInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Conflicting directions cancel. Unlocked cursor/focus loss stops channeling.
        if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked)
        {
            bool rewind = keyboard.qKey.isPressed;
            bool age = keyboard.eKey.isPressed;
            if (rewind && !age) gauntlet.RequestRewind();
            if (age && !rewind) gauntlet.RequestAccelerate();
        }

        // F = Scan and R = Undo are declared in the brief as optional.
        // Left as explicit no-ops so the binding exists but nothing lies about working.
        if (keyboard.fKey.wasPressedThisFrame) OnScanPressed();
        if (keyboard.rKey.wasPressedThisFrame) OnUndoPressed();
    }

    private void OnScanPressed()
    {
        var target = gauntlet.CurrentTarget;
        if (target == null) return;
        HintTextManager.Instance?.ShowHint($"{target.displayName} — {target.CurrentStateName}", 2f);
    }

    private void OnUndoPressed()
    {
        // Not in MVP scope. Intentionally does nothing rather than half-working.
    }
}
