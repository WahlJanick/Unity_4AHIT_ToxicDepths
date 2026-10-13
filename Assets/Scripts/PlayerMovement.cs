using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour
{
    [Header("Bewegung")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Mausblick")]
    [Tooltip("Die Kamera muss ein Kindobjekt des Spielers sein.")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Drehung in Grad pro Pixel Mausbewegung.")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField, Range(0f, 89f)] private float maxLookAngle = 85f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera == null || playerCamera.transform == transform ||
            !playerCamera.transform.IsChildOf(transform))
        {
            Debug.LogError("PlayerMovement: Weise eine Kamera auf einem Kindobjekt des Spielers zu.", this);
            enabled = false;
            return;
        }

        cameraPitch = Mathf.DeltaAngle(0f, playerCamera.transform.localEulerAngles.x);

        // Bindings direkt im Code: kein zusaetzliches Input-Actions-Asset erforderlich.
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        SetCursorLocked(true);
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        verticalVelocity = 0f;
        SetCursorLocked(false);
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        lookAction?.Dispose();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SetCursorLocked(false);
        }
    }

    private void Update()
    {
        bool cursorChanged = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
            cursorChanged = true;
        }
        else if (Application.isFocused && Cursor.lockState != CursorLockMode.Locked &&
                 Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
            cursorChanged = true;
        }

        bool controlsActive = Application.isFocused &&
                              Cursor.lockState == CursorLockMode.Locked && !cursorChanged;

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        if (controlsActive)
        {
            Look();
        }

        Move(controlsActive);
    }

    private void Look()
    {
        // Mausdelta ist bereits die Bewegung dieses Frames: kein Time.deltaTime.
        Vector2 look = lookAction.ReadValue<Vector2>() * mouseSensitivity;
        transform.Rotate(Vector3.up, look.x, Space.World);

        cameraPitch = Mathf.Clamp(cameraPitch - look.y, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void Move(bool controlsActive)
    {
        Vector2 input = controlsActive ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
