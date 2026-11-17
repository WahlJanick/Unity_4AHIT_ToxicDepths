using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ToxicDepths
{
    [AddComponentMenu("Toxic Depths/Frog Greeting")]
    [DisallowMultipleComponent]
    public sealed class FrogGreeting : MonoBehaviour
    {
        [Serializable]
        private sealed class Choice
        {
            public string label;
            [Tooltip("Leer lassen, um das Gespräch zu beenden.")]
            public string nextNode;
        }

        [Serializable]
        private sealed class Node
        {
            public string id;
            [TextArea(2, 4)] public string line;
            public Choice first;
            public Choice second;
        }

        [SerializeField] private Canvas speechBubble;
        [SerializeField] private TMP_Text speechText;
        [SerializeField, FormerlySerializedAs("tipChoice")] private Button firstChoice;
        [SerializeField, FormerlySerializedAs("goodbyeChoice")] private Button secondChoice;
        [SerializeField, Tooltip("Der erste Eintrag ist der Gesprächseinstieg.")]
        private Node[] nodes = Array.Empty<Node>();

        private RectTransform dialogue;
        private TMP_Text firstLabel;
        private TMP_Text secondLabel;
        private Node currentNode;
        private Vector3 headOffset;
        private float dialogueScale;
        private Camera viewer;
        private PlayerMovement player;

        private void Awake()
        {
            if (speechBubble == null || speechText == null || firstChoice == null || secondChoice == null)
                return;

            firstLabel = firstChoice.GetComponentInChildren<TMP_Text>(true);
            secondLabel = secondChoice.GetComponentInChildren<TMP_Text>(true);
            if (firstLabel == null || secondLabel == null) return;

            dialogue = (RectTransform)speechBubble.transform;
            headOffset = dialogue.localPosition;
            dialogueScale = dialogue.localScale.x;
            firstChoice.onClick.AddListener(() => Choose(currentNode?.first));
            secondChoice.onClick.AddListener(() => Choose(currentNode?.second));
        }

        internal bool Show(Camera playerCamera, PlayerMovement listener)
        {
            if (dialogue == null || nodes.Length == 0 || playerCamera == null || listener == null ||
                !listener.TryBeginConversation(this))
                return false;

            player = listener;
            viewer = playerCamera;
            speechBubble.worldCamera = viewer;
            ShowNode(nodes[0]);

            if (EventSystem.current == null)
                new GameObject("Gameplay EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            PositionDialogue();
            dialogue.gameObject.SetActive(true);
            return true;
        }

        private void LateUpdate()
        {
            if (dialogue == null || !dialogue.gameObject.activeSelf) return;
            if (viewer == null || !viewer.isActiveAndEnabled || player == null)
            {
                Close();
                return;
            }

            PositionDialogue();
        }

        private void PositionDialogue()
        {
            const float margin = 0.025f;
            Vector3 anchor = viewer.WorldToViewportPoint(transform.TransformPoint(headOffset));
            float minDepth = viewer.nearClipPlane + 0.05f;
            if (anchor.z < minDepth)
                anchor = new Vector3(0.5f, 1f, minDepth);

            // Fit both columns inside the view even when the player is close to the frog.
            Vector3 viewSize = viewer.ViewportToWorldPoint(new Vector3(1f, 1f, anchor.z)) -
                viewer.ViewportToWorldPoint(new Vector3(0f, 0f, anchor.z));
            float viewWidth = Vector3.Dot(viewSize, viewer.transform.right);
            float viewHeight = Vector3.Dot(viewSize, viewer.transform.up);
            Rect bounds = dialogue.rect;
            float scale = Mathf.Min(dialogueScale, Mathf.Min(viewWidth * 0.67f / bounds.width, viewHeight * 0.27f / bounds.height));

            float horizontal = scale / viewWidth;
            float vertical = scale / viewHeight;
            anchor.x = Mathf.Clamp(anchor.x, margin - bounds.xMin * horizontal, 1f - margin - bounds.xMax * horizontal);
            anchor.y = Mathf.Clamp(anchor.y, margin - bounds.yMin * vertical, 1f - margin - bounds.yMax * vertical);
            // Project the bubble a little closer so the frog's head cannot cover the text.
            float planeDepth = Mathf.Max(minDepth, anchor.z * 0.6f);
            float drawingScale = viewer.orthographic ? scale : scale * (planeDepth / anchor.z);
            dialogue.localScale = Vector3.one * drawingScale;
            anchor.z = planeDepth;
            dialogue.SetPositionAndRotation(viewer.ViewportToWorldPoint(anchor), viewer.transform.rotation);
        }

        private void ShowNode(Node node)
        {
            currentNode = node;
            speechText.text = node.line;
            firstLabel.text = node.first.label;
            secondLabel.text = node.second.label;
        }

        private void Choose(Choice choice)
        {
            if (choice == null) return;
            if (!string.IsNullOrEmpty(choice.nextNode))
            {
                var next = Array.Find(nodes, node => node.id == choice.nextNode);
                if (next != null)
                {
                    ShowNode(next);
                    return;
                }

                Debug.LogWarning($"Unbekannter Dialogknoten am Frosch: {choice.nextNode}", this);
            }

            Close();
        }

        internal void Close()
        {
            currentNode = null;
            if (dialogue != null) dialogue.gameObject.SetActive(false);
            if (speechBubble != null) speechBubble.worldCamera = null;
            viewer = null;
            if (player != null) player.EndConversation(this);
            player = null;
        }

        private void OnDisable() => Close();
    }
}
