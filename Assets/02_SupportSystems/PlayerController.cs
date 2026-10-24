using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Camera-relative locomotion. World collisions alone determine traversal.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("References (camera falls back to Camera.main)")]
    [SerializeField] private Camera movementCamera;
    [SerializeField] private PlayerCameraRig cameraRig;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 4f;
    [SerializeField, Min(0f)] private float sprintSpeed = 6.5f;
    [SerializeField, Min(0f)] private float acceleration = 25f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -22f;
    [SerializeField, Min(0.1f)] private float terminalSpeed = 35f;

    [Header("Ground probe (exclude the player's layer)")]
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.12f;

    [Header("Fall recovery")]
    [Tooltip("Below this world Y, return to the position captured in Start. No puzzle/energy reset.")]
    [SerializeField] private float fallResetY = -20f;

    private CharacterController controller;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float climbReleaseUntil;

    private void Awake() => controller = GetComponent<CharacterController>();

    private void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        if (movementCamera == null) movementCamera = Camera.main;
    }

    private void Update()
    {
        if (controller == null || !controller.enabled) return;
        if (transform.position.y < fallResetY)
        {
            ReturnToSpawn();
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (movementCamera == null) movementCamera = Camera.main;
        if (TryClimb(dt)) return;
        bool grounded = verticalVelocity <= 0f && HasGround();
        bool acceptsInput = Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
        Keyboard keyboard = acceptsInput ? Keyboard.current : null;

        // Keep takeoff momentum in air; no repeated sprint boosts or air steering.
        if (grounded)
        {
            verticalVelocity = -2f;
            Vector2 input = Vector2.zero;
            if (keyboard != null)
                input = Vector2.ClampMagnitude(new Vector2(
                    (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)), 1f);

            Vector3 desired = Vector3.zero;
            if (movementCamera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(movementCamera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float speed = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                    ? sprintSpeed : walkSpeed;
                desired = (forward * input.y + right * input.x) * speed;
            }
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, acceleration * dt);
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        // Unlocking the cursor stops horizontal motion, but gravity keeps running.
        if (!acceptsInput) horizontalVelocity = Vector3.zero;

        if (horizontalVelocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(horizontalVelocity, Vector3.up), turnSpeed * dt);

        verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -terminalSpeed);
        CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f) verticalVelocity = -2f;
    }

    private bool TryClimb(float dt)
    {
        if (Time.time < climbReleaseUntil || !Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return false;
        var keyboard = Keyboard.current;
        TemporalClimbVolume volume;
        if (keyboard == null || !TemporalClimbVolume.TryFind(controller, out volume)) return false;
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            horizontalVelocity = -volume.transform.forward * walkSpeed;
            climbReleaseUntil = Time.time + .5f; return false;
        }
        if (volume.topExit != null && transform.position.y >= volume.topExit.position.y)
        {
            horizontalVelocity = Vector3.ProjectOnPlane(volume.topExit.position - transform.position, Vector3.up).normalized * 3f;
            verticalVelocity = 2f; climbReleaseUntil = Time.time + .6f; return false;
        }
        float input = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        verticalVelocity = 0f; horizontalVelocity = Vector3.zero;
        controller.Move(Vector3.up * (input * volume.climbSpeed * dt));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(volume.transform.forward), turnSpeed * dt);
        return true;
    }

    private bool HasGround()
    {
        // Expected wiring: upright Player, unit scale, height 2 and radius 0.35.
        float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
        Vector3 centre = transform.TransformPoint(controller.center);
        Vector3 origin = centre + Vector3.down * (controller.height * 0.5f - controller.radius)
            + Vector3.up * 0.05f;
        float distance = 0.05f + controller.skinWidth + groundProbeDistance;
        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
            distance, groundMask, QueryTriggerInteraction.Ignore);
        // Saturated non-alloc results are unordered: don't silently lose a valid support.
        RaycastHit[] hits = count == groundHits.Length
            ? Physics.SphereCastAll(origin, radius, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore)
            : groundHits;
        if (hits != groundHits) count = hits.Length;
        float minimumNormalY = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
            if (hits[i].collider != null && !hits[i].collider.transform.IsChildOf(transform)
                && hits[i].normal.y >= minimumNormalY) return true;
        return false;
    }

    private void ReturnToSpawn()
    {
        Vector3 delta = spawnPosition - transform.position;
        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        controller.enabled = true;
        if (cameraRig != null) cameraRig.NotifyTargetWarp(delta);
    }

    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        acceleration = Mathf.Max(0.01f, acceleration);
        turnSpeed = Mathf.Max(0f, turnSpeed);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        gravity = Mathf.Min(-0.01f, gravity);
        terminalSpeed = Mathf.Max(0.1f, terminalSpeed);
        groundProbeDistance = Mathf.Max(0.01f, groundProbeDistance);
    }
}
