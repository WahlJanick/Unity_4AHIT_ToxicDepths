using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

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
    [Tooltip("Optional: das Pilzling-Modell. Es wirft Schatten, verdeckt aber die Ego-Sicht nicht.")]
    [SerializeField] private Transform playerModel;
    [Tooltip("Drehung in Grad pro Pixel Mausbewegung.")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField, Range(0f, 89f)] private float maxLookAngle = 85f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private float verticalVelocity;
    private float cameraPitch;
    private float cameraYaw;
    private Renderer[] modelRenderers;
    private ShadowCastingMode[] originalShadowModes;
    private bool controlsPaused;

    private bool InitializePlayer()
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
            return false;
        }

        cameraPitch = Mathf.DeltaAngle(0f, playerCamera.transform.localEulerAngles.x);
        cameraYaw = playerCamera.transform.localEulerAngles.y;

        if (playerModel != null)
        {
            modelRenderers = playerModel.GetComponentsInChildren<Renderer>(true);
            originalShadowModes = new ShadowCastingMode[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                originalShadowModes[i] = modelRenderers[i].shadowCastingMode;
            }
        }

        // Bindings direkt im Code: kein zusaetzliches Input-Actions-Asset erforderlich.
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        return true;
    }

    private void OnEnable()
    {
        // Auch ohne Scene-/Domain-Reload die Eingaben bei jedem Play-Start neu anlegen.
        if (!InitializePlayer())
        {
            enabled = false;
            return;
        }

        moveAction.Enable();
        lookAction.Enable();
        controlsPaused = false;
        SetModelVisible(false);
        SetCursorLocked(Application.isFocused);
    }

    private void OnDisable()
    {
        DisposeInputActions();
        controlsPaused = true;
        verticalVelocity = 0f;
        SetModelVisible(true);
        SetCursorLocked(false);
    }

    private void OnDestroy()
    {
        DisposeInputActions();
    }

    private void DisposeInputActions()
    {
        moveAction?.Dispose();
        lookAction?.Dispose();
        moveAction = null;
        lookAction = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!hasFocus)
        {
            SetCursorLocked(false);
        }
        else if (!controlsPaused)
        {
            SetCursorLocked(true);
        }
    }

    private void Update()
    {
        bool cursorChanged = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            controlsPaused = true;
            SetCursorLocked(false);
            cursorChanged = true;
        }
        else if (Application.isFocused && controlsPaused &&
                 Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            controlsPaused = false;
            SetCursorLocked(true);
            cursorChanged = true;
        }

        // Unity kann den Cursor-Lock beim Fokuswechsel aufheben.
        // WASD darf dadurch nicht dauerhaft blockiert werden.
        bool controlsActive = Application.isFocused && !controlsPaused;
        if (controlsActive && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
            cursorChanged = true;
        }

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        if (controlsActive && !cursorChanged && Cursor.lockState == CursorLockMode.Locked)
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
        // Die im Inspector eingestellte Blickrichtung des Modells beibehalten.
        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }

    private void Move(bool controlsActive)
    {
        Vector2 input = controlsActive ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        input = Vector2.ClampMagnitude(input, 1f);
        // Auch bei einer um 180 Grad gedrehten Kamera bewegt W nach vorne.
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 velocity = (right * input.x + forward * input.y) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void SetModelVisible(bool visible)
    {
        if (modelRenderers == null)
        {
            return;
        }

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
            {
                modelRenderers[i].shadowCastingMode = visible
                    ? originalShadowModes[i]
                    : ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
