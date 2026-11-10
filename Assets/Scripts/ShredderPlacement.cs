using System;
using ToxicDepths.Mining;
using ToxicDepths.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace ToxicDepths
{
    [RequireComponent(typeof(PlayerMovement))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(110)]
    public sealed class ShredderPlacement : MonoBehaviour
    {
        private const float FloorClearance = 0.015f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color ValidColor = new Color(0.30f, 0.95f, 0.62f, 0.44f);
        private static readonly Color BlockedColor = new Color(1f, 0.27f, 0.22f, 0.44f);

        [SerializeField] private Camera playerCamera;
        [SerializeField] private HotbarController hotbar;
        [SerializeField] private GameObject shredderPrefab;
        [SerializeField] private Sprite shredderIcon;
        [SerializeField] private Material previewMaterial;
        [SerializeField, Min(0.1f)] private float placementReach = 4f;
        [SerializeField] private LayerMask placementLayers = Physics.DefaultRaycastLayers;

        private PlayerMovement movement;
        private PlayerMining mining;
        private BoxCollider footprint;
        private GameObject preview;
        private Renderer[] previewRenderers;
        private MaterialPropertyBlock previewColor;
        private bool isAiming;
        private readonly int[] shredderCounts = new int[Shredder.Models.Count];

        internal HotbarController Hotbar => hotbar;
        internal Shredder ShredderPrefab => shredderPrefab != null ? shredderPrefab.GetComponentInChildren<Shredder>() : null;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            TryGetComponent(out mining);
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (shredderPrefab != null) shredderPrefab.TryGetComponent(out footprint);
        }

        private void Start()
        {
            if (hotbar == null || footprint == null) return;
            Array.Clear(shredderCounts, 0, shredderCounts.Length);
            for (int i = 1; i < shredderCounts.Length; i++) RefreshSlot(i);
            GiveShredder();
        }

        public bool GiveShredder(int model = 0)
        {
            if (hotbar == null || footprint == null || model < 0 || model >= shredderCounts.Length)
                return false;

            shredderCounts[model]++;
            RefreshSlot(model);
            return true;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !CanPlace())
            {
                CancelPlacement();
                return;
            }

            var placeButton = mouse.rightButton;
            if (placeButton.wasPressedThisFrame) isAiming = true;
            if (!isAiming) return;

            bool hasFloor = TryGetFloorPose(out Pose pose);
            bool canPlace = hasFloor && CanPlaceAt(pose);
            if (placeButton.isPressed)
            {
                if (hasFloor) ShowPreview(pose, canPlace);
                else HidePreview();
                return;
            }

            CancelPlacement();
            if (!placeButton.wasReleasedThisFrame || !canPlace) return;

            int slot = hotbar.SelectedIndex;
            var machine = Instantiate(shredderPrefab, pose.position, pose.rotation);
            var processor = machine.GetComponentInChildren<Shredder>();
            if (processor != null) processor.ConfigureModel(slot);
            shredderCounts[slot]--;
            RefreshSlot(slot);
        }

        private bool CanPlace()
        {
            if (playerCamera == null || hotbar == null || !hotbar.isActiveAndEnabled || footprint == null ||
                !movement.CanUseTools || (mining != null && mining.IsCarryingOre))
                return false;

            int slot = hotbar.SelectedIndex;
            return slot < shredderCounts.Length && shredderCounts[slot] > 0;
        }

        private bool TryGetFloorPose(out Pose pose)
        {
            pose = default;
            Transform eye = playerCamera.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, placementReach,
                    placementLayers, QueryTriggerInteraction.Ignore) || !IsFloor(hit))
                return false;

            Vector3 facingPlayer = -Vector3.ProjectOnPlane(eye.forward, Vector3.up);
            float bottom = footprint.center.y - footprint.size.y * 0.5f;
            pose = new Pose(hit.point + Vector3.up * (FloorClearance - bottom),
                Quaternion.LookRotation(facingPlayer));
            return true;
        }

        private bool CanPlaceAt(Pose pose)
        {
            Vector3 halfSize = footprint.size * 0.5f;
            Vector3 center = pose.position + pose.rotation * footprint.center;
            if (Physics.CheckBox(center, halfSize, pose.rotation, placementLayers, QueryTriggerInteraction.Ignore))
                return false;

            // Check the base corners so the machine cannot balance over a ledge.
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = new Vector3(x * halfSize.x * 0.9f, -halfSize.y + 0.12f, z * halfSize.z * 0.9f);
                if (!Physics.Raycast(center + pose.rotation * corner, Vector3.down, out RaycastHit support,
                        0.28f, placementLayers, QueryTriggerInteraction.Ignore) || !IsFloor(support))
                    return false;
            }

            return true;
        }

        private static bool IsFloor(RaycastHit hit) => hit.normal.y >= 0.9f && hit.rigidbody == null;

        private void ShowPreview(Pose pose, bool canPlace)
        {
            if (previewMaterial == null) return;
            if (preview == null)
            {
                preview = Instantiate(shredderPrefab);
                preview.name = "Zerschredderer (Vorschau)";
                foreach (var collider in preview.GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                previewRenderers = preview.GetComponentsInChildren<Renderer>();
                foreach (var renderer in previewRenderers)
                {
                    var materials = renderer.sharedMaterials;
                    Array.Fill(materials, previewMaterial);
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                previewColor = new MaterialPropertyBlock();
            }

            preview.transform.SetPositionAndRotation(pose.position, pose.rotation);
            previewColor.SetColor(BaseColor, canPlace ? ValidColor : BlockedColor);
            foreach (var renderer in previewRenderers) renderer.SetPropertyBlock(previewColor);
            preview.SetActive(true);
        }

        private void HidePreview()
        {
            if (preview != null) preview.SetActive(false);
        }

        private void RefreshSlot(int slot) => hotbar.SetSlot(slot, shredderIcon, shredderCounts[slot]);

        private void CancelPlacement()
        {
            isAiming = false;
            HidePreview();
        }

        private void OnDisable() => CancelPlacement();

        private void OnDestroy()
        {
            if (preview != null) Destroy(preview);
        }
    }
}