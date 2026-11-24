using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToxicDepths.UI
{
    [DisallowMultipleComponent]
    internal sealed class PlayerDefeat : MonoBehaviour
    {
        private const float FallDuration = 0.75f;
        private const float FadeStart = 0.42f;
        private const float FadeEnd = 1f;
        private static readonly Color Background = new Color(0.045f, 0.06f, 0.065f);
        private static readonly Color Accent = new Color(0.40f, 0.94f, 0.66f);

        private CharacterController controller;
        private Transform view;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private Quaternion startRotation;
        private Quaternion endRotation;
        private float elapsed;
        private float fallSpeed;
        private float previousTimeScale;
        private bool paused;
        private bool loading;
        private GUIStyle title;
        private GUIStyle action;

        internal void Begin(Camera camera, CharacterController playerController)
        {
            controller = playerController;
            if (camera == null) return;

            view = camera.transform;
            startPosition = view.localPosition;
            endPosition = startPosition + new Vector3(0.25f, -1.08f, 0f);
            startRotation = view.localRotation;
            endRotation = startRotation * Quaternion.Euler(10f, 0f, 76f);
        }

        private void Update()
        {
            if (paused)
            {
                elapsed = Mathf.Min(FadeEnd, elapsed + Time.unscaledDeltaTime);
                return;
            }

            float step = Mathf.Min(Time.unscaledDeltaTime, FallDuration - elapsed);
            elapsed += step;
            float fall = Mathf.SmoothStep(0f, 1f, elapsed / FallDuration);
            if (controller != null && controller.enabled && !controller.isGrounded)
            {
                fallSpeed += 20f * step;
                controller.Move(Vector3.down * (fallSpeed * step));
            }

            if (view != null)
                view.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, endPosition, fall),
                    Quaternion.Slerp(startRotation, endRotation, fall));

            if (elapsed < FallDuration) return;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            paused = true;
        }

        private void OnGUI()
        {
            if (elapsed < FadeStart) return;

            GUI.depth = -100;
            float fade = Mathf.InverseLerp(FadeStart, FadeEnd, elapsed);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture,
                ScaleMode.StretchToFill, true, 0f, new Color(Background.r, Background.g, Background.b, fade), 0f, 0f);
            if (elapsed < FadeEnd || loading) return;

            title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            action ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 17 };
            title.fontSize = Mathf.Clamp(Screen.height / 12, 40, 84);
            title.normal.textColor = Color.white;

            float center = Screen.height * 0.48f;
            float width = Mathf.Min(240f, Screen.width * 0.8f);
            float left = (Screen.width - width) * 0.5f;
            GUI.Label(new Rect(0, center - 100f, Screen.width, 100f), "YOU LOSE", title);

            if (DrawAction(new Rect(left, center + 20f, width, 44f), "Neustart", Accent, Background))
                LoadScene(gameObject.scene.path);
            else if (DrawAction(new Rect(left, center + 74f, width, 38f), "Hauptmenü", Background, Color.white))
                LoadScene("MainMenu");
        }

        private bool DrawAction(Rect rect, string text, Color fill, Color foreground)
        {
            if (rect.Contains(Event.current.mousePosition)) fill = Color.Lerp(fill, Color.white, 0.16f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, 0f, 0f);
            action.normal.textColor = foreground;
            GUI.Label(rect, text, action);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void LoadScene(string scene)
        {
            loading = true;
            SceneManager.LoadScene(scene);
            ReleasePause();
        }

        private void OnDestroy() => ReleasePause();

        private void ReleasePause()
        {
            if (!paused) return;
            Time.timeScale = previousTimeScale;
            paused = false;
        }
    }
}
