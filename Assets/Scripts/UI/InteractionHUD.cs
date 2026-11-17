using ToxicDepths.Mining;
using UnityEngine;

namespace ToxicDepths.UI
{
    [AddComponentMenu("Toxic Depths/UI/Interaction HUD")]
    [DisallowMultipleComponent]
    public sealed class InteractionHUD : MonoBehaviour
    {
        [SerializeField] private PlayerMining playerMining;
        [SerializeField] private GameObject pickupPrompt;
        [SerializeField] private GameObject shredderProgress;
        [SerializeField] private RectTransform progressFill;

        private PlayerMovement movement;
        private Canvas canvas;
        private RectTransform promptRect;

        private void OnEnable()
        {
            movement = playerMining != null ? playerMining.GetComponent<PlayerMovement>() : null;
            canvas = GetComponent<Canvas>();
            promptRect = pickupPrompt != null ? pickupPrompt.transform as RectTransform : null;
            HideAll();
        }

        private void LateUpdate()
        {
            if (playerMining == null || !playerMining.isActiveAndEnabled ||
                movement == null || !movement.CanUseTools ||
                !playerMining.TryAim(out RaycastHit hit))
            {
                HideAll();
                return;
            }

            bool canPickUp = playerMining.CanPickUpTarget(hit.collider);
            SetVisible(pickupPrompt, canPickUp && PositionPrompt(hit.collider.bounds.center));

            var machine = Shredder.Find(hit.collider);
            bool processing = machine != null && machine.isActiveAndEnabled && machine.IsProcessing;
            SetVisible(shredderProgress, processing);
            if (processing && progressFill != null)
                progressFill.anchorMax = new Vector2(machine.Progress, 1f);
        }

        private bool PositionPrompt(Vector3 worldPosition)
        {
            var viewCamera = playerMining != null ? playerMining.ViewCamera : null;
            if (viewCamera == null || canvas == null || promptRect == null ||
                !(promptRect.parent is RectTransform parent)) return false;

            Vector3 screenPosition = viewCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f || !viewCamera.pixelRect.Contains(screenPosition)) return false;

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition,
                    uiCamera, out Vector2 localPosition)) return false;

            // Anchor to the object, not to the ray's hit point (which follows the crosshair).
            promptRect.position = parent.TransformPoint(localPosition);
            return true;
        }

        private void HideAll()
        {
            SetVisible(pickupPrompt, false);
            SetVisible(shredderProgress, false);
        }

        private static void SetVisible(GameObject view, bool visible)
        {
            if (view != null && view.activeSelf != visible) view.SetActive(visible);
        }

        private void OnDisable() => HideAll();
    }
}
