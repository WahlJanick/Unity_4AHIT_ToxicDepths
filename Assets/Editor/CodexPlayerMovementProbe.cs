using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class CodexPlayerMovementProbe
{
    private const string Root = "C:/Users/Michael/Documents/Codex/2026-10-13/kanns/work/";
    private static double nextSnapshot;
    private static double releaseAt;
    private static Key[] heldKeys;
    private static Vector3 startPosition;
    private static string testName;
    private static int samples;
    private static float maxInput;
    private static bool allActionsEnabled;

    static CodexPlayerMovementProbe()
    {
        EditorApplication.update += Tick;
        InputSystem.onBeforeUpdate += QueueKeys;
        InputSystem.onAfterUpdate += SampleInput;
    }

    private static object Field(PlayerMovement player, string name)
    {
        return typeof(PlayerMovement).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(player);
    }

    private static void QueueKeys()
    {
        if (EditorApplication.isPlaying && heldKeys != null && Keyboard.current != null &&
            InputState.currentUpdateType == InputUpdateType.Dynamic)
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(heldKeys));
    }

    private static void SampleInput()
    {
        if (heldKeys == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        var move = player != null ? Field(player, "moveAction") as InputAction : null;
        if (move != null) maxInput = Mathf.Max(maxInput, move.ReadValue<Vector2>().magnitude);
    }

    private static void Tick()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        var request = Root + "movement-request.txt";
        if (File.Exists(request))
        {
            var command = File.ReadAllText(request).Trim();
            File.Delete(request);
            if (command.StartsWith("move-") && player != null && EditorApplication.isPlaying)
            {
                testName = command;
                heldKeys = command.Substring(5) switch
                {
                    "w" => new[] { Key.W },
                    "s" => new[] { Key.S },
                    "a" => new[] { Key.A },
                    "d" => new[] { Key.D },
                    "wd" => new[] { Key.W, Key.D },
                    _ => Array.Empty<Key>()
                };
                startPosition = player.transform.position;
                releaseAt = EditorApplication.timeSinceStartup + 1;
                samples = 0;
                maxInput = 0;
                allActionsEnabled = true;
            }
            else if (command == "reenable" && player != null)
            {
                player.enabled = false;
                player.enabled = true;
            }
            else if (command == "unlock")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (command == "play") EditorApplication.isPlaying = true;
            else if (command == "stop") EditorApplication.isPlaying = false;
        }

        if (heldKeys != null && player != null)
        {
            samples++;
            var move = Field(player, "moveAction") as InputAction;
            allActionsEnabled &= move != null && move.enabled;
            if (move != null) maxInput = Mathf.Max(maxInput, move.ReadValue<Vector2>().magnitude);
            if (EditorApplication.timeSinceStartup >= releaseAt)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                heldKeys = null;
                var delta = player.transform.position - startPosition;
                File.AppendAllText(Root + "movement-tests.jsonl", JsonUtility.ToJson(new TestResult
                {
                    name = testName, start = startPosition, end = player.transform.position,
                    delta = delta, distance = new Vector2(delta.x, delta.z).magnitude,
                    samples = samples, maxInput = maxInput, actionsEnabled = allActionsEnabled,
                    focus = Application.isFocused, cursor = Cursor.lockState.ToString()
                }) + "\n");
            }
        }

        if (EditorApplication.timeSinceStartup < nextSnapshot) return;
        nextSnapshot = EditorApplication.timeSinceStartup + 0.2;
        var action = player != null ? Field(player, "moveAction") as InputAction : null;
        var look = player != null ? Field(player, "lookAction") as InputAction : null;
        var controller = player != null ? player.GetComponent<CharacterController>() : null;
        var snapshot = new Snapshot
        {
            playing = EditorApplication.isPlaying, paused = EditorApplication.isPaused,
            focus = Application.isFocused, cursor = Cursor.lockState.ToString(),
            playerEnabled = player != null && player.enabled,
            position = player != null ? player.transform.position : Vector3.zero,
            cameraRotation = player != null ? player.GetComponentInChildren<Camera>().transform.localEulerAngles : Vector3.zero,
            moveExists = action != null, moveEnabled = action != null && action.enabled,
            moveInput = action != null ? action.ReadValue<Vector2>() : Vector2.zero,
            lookEnabled = look != null && look.enabled,
            keyboardEnabled = Keyboard.current != null && Keyboard.current.enabled,
            wPressed = Keyboard.current != null && Keyboard.current.wKey.isPressed,
            controls = action != null ? string.Join(", ", action.controls) : "",
            grounded = controller != null && controller.isGrounded, timeScale = Time.timeScale,
            inputUpdateMode = InputSystem.settings.updateMode.ToString()
        };
        File.WriteAllText(Root + "movement-status.json", JsonUtility.ToJson(snapshot, true));
    }

    [Serializable] private class Snapshot
    {
        public bool playing, paused, focus, playerEnabled, moveExists, moveEnabled, lookEnabled, keyboardEnabled, wPressed, grounded;
        public string cursor, controls, inputUpdateMode;
        public Vector3 position, cameraRotation;
        public Vector2 moveInput;
        public float timeScale;
    }

    [Serializable] private class TestResult
    {
        public string name, cursor;
        public Vector3 start, end, delta;
        public float distance, maxInput;
        public int samples;
        public bool actionsEnabled, focus;
    }
}
