using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>Input/cursor glue for Cinemachine 3.1.7. Cinemachine owns camera placement/collision.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachineOrbitalFollow))]
public sealed class PlayerCameraRig : MonoBehaviour
{
    [Header("Mouse (degrees per pixel, not degrees per second)")]
    [SerializeField, Min(0f)] private float sensitivity = 0.15f;
    [SerializeField] private bool invertY;
    [SerializeField] private Vector2 pitchLimits = new Vector2(-20f, 65f);

    private CinemachineCamera virtualCamera;
    private CinemachineOrbitalFollow orbit;
    private bool skipMouseDelta;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        orbit = GetComponent<CinemachineOrbitalFollow>();
    }

    private void OnEnable()
    {
        if (orbit == null) orbit = GetComponent<CinemachineOrbitalFollow>();
        if (orbit != null)
        {
            orbit.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orbit.HorizontalAxis.Wrap = true;
            orbit.HorizontalAxis.Recentering.Enabled = false;
            orbit.VerticalAxis.Range = pitchLimits;
            orbit.VerticalAxis.Wrap = false;
            orbit.VerticalAxis.Value = orbit.VerticalAxis.ClampValue(orbit.VerticalAxis.Value);
            orbit.VerticalAxis.Recentering.Enabled = false;
        }
        SetCursor(Application.isFocused);
    }

    private void Update()
    {
        if (!Application.isFocused) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursor(false);
            return;
        }
        Mouse mouse = Mouse.current;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) SetCursor(true);
            return;
        }
        if (skipMouseDelta) { skipMouseDelta = false; return; }
        if (mouse == null || orbit == null || Time.deltaTime <= 0f) return;
        Vector2 delta = mouse.delta.ReadValue() * sensitivity;
        orbit.HorizontalAxis.Value = orbit.HorizontalAxis.ClampValue(orbit.HorizontalAxis.Value + delta.x);
        orbit.VerticalAxis.Value = orbit.VerticalAxis.ClampValue(
            orbit.VerticalAxis.Value + (invertY ? delta.y : -delta.y));
    }

    public void NotifyTargetWarp(Vector3 delta)
    {
        if (virtualCamera == null) return;
        Transform follow = virtualCamera.Follow;
        Transform lookAt = virtualCamera.LookAt;
        if (follow != null) virtualCamera.OnTargetObjectWarped(follow, delta);
        if (lookAt != null && lookAt != follow) virtualCamera.OnTargetObjectWarped(lookAt, delta);
        virtualCamera.PreviousStateIsValid = false;
    }

    private void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        skipMouseDelta = true;
    }

    private void OnApplicationFocus(bool focused)
    {
        // Returning to the app requires an explicit click to resume controls.
        if (!focused) SetCursor(false);
    }

    private void OnDisable() => SetCursor(false);

    private void OnValidate()
    {
        sensitivity = Mathf.Max(0f, sensitivity);
        pitchLimits.x = Mathf.Clamp(pitchLimits.x, -85f, 84f);
        pitchLimits.y = Mathf.Clamp(pitchLimits.y, pitchLimits.x + 1f, 85f);
    }
}
