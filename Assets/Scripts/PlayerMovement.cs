using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour
{
    [Header("Bewegung")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [Tooltip("Beschleunigung nach unten in Metern pro Sekunde zum Quadrat.")]
    [SerializeField] private float gravity = -20f;

    [Header("Springen")]
    [Tooltip("Sprunghoehe in Metern. 0 deaktiviert das Springen.")]
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;

    [Header("Mausblick")]
    [Tooltip("Die Kamera muss ein Kindobjekt des Spielers sein.")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Optional: das Pilzling-Modell. Es wird in der Ego-Sicht inklusive Schatten ausgeblendet.")]
    [SerializeField] private Transform playerModel;
    [Tooltip("Statisch gehaltenes Werkzeug mit Hand fuer die Ego-Sicht. Kindobjekt der Kamera.")]
    [SerializeField] private GameObject firstPersonHeldItem;
    [Tooltip("Drehung in Grad pro Pixel Mausbewegung.")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField, Range(0f, 89f)] private float maxLookAngle = 85f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private float verticalVelocity;
    private float cameraPitch;
    private float cameraYaw;
    private Renderer[] modelRenderers;
    private bool[] originalRendererStates;
    private bool controlsPaused;

    private void OnValidate()
    {
        // Die Schwerkraft muss nach unten wirken, damit die Sprungberechnung gueltig bleibt.
        gravity = -Mathf.Max(Mathf.Abs(gravity), 0.01f);
        jumpHeight = Mathf.Max(0f, jumpHeight);
    }

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
            originalRendererStates = new bool[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                originalRendererStates[i] = modelRenderers[i].enabled;
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
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
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
        jumpAction.Enable();
        controlsPaused = false;
        SetModelVisible(false);
        if (firstPersonHeldItem != null) firstPersonHeldItem.SetActive(true);
        SetCursorLocked(Application.isFocused);
    }

    private void OnDisable()
    {
        DisposeInputActions();
        controlsPaused = true;
        verticalVelocity = 0f;
        SetModelVisible(true);
        if (firstPersonHeldItem != null) firstPersonHeldItem.SetActive(false);
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
        jumpAction?.Dispose();
        moveAction = null;
        lookAction = null;
        jumpAction = null;
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

        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        float downwardGravity = -Mathf.Max(Mathf.Abs(gravity), 0.01f);
        if (controlsActive && grounded && verticalVelocity <= 0f &&
            jumpHeight > 0f && jumpAction.WasPressedThisFrame())
        {
            // v = sqrt(2 * g * h): die Inspector-Einstellung bestimmt die Sprunghoehe.
            verticalVelocity = Mathf.Sqrt(2f * -downwardGravity * jumpHeight);
        }

        verticalVelocity += downwardGravity * Time.deltaTime;
        velocity.y = verticalVelocity;
        CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);
        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
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
                modelRenderers[i].enabled = visible && originalRendererStates[i];
            }
        }
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
