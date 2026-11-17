using System;
using ToxicDepths.Mining;
using ToxicDepths.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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
        [SerializeField, Range(0f, 60f)] private float maximumSlope = 35f;
        [SerializeField] private LayerMask placementLayers = Physics.DefaultRaycastLayers;

        private PlayerMovement movement;
        private PlayerMining mining;
        private BoxCollider footprint;
        private GameObject preview;
        private Renderer[] previewRenderers;
        private MaterialPropertyBlock previewColor;
        private float rotationOffset;
        private int lastSelectedSlot = -1;
        private readonly int[] shredderCounts = new int[Shredder.Models.Count];

        internal HotbarController Hotbar => hotbar;
        internal Shredder ShredderPrefab => shredderPrefab != null ? shredderPrefab.GetComponentInChildren<Shredder>() : null;
        internal bool CanStoreShredder => hotbar != null && footprint != null;
        internal bool HasSelectedShredder => CanStoreShredder && hotbar.SelectedIndex >= 0 &&
            hotbar.SelectedIndex < shredderCounts.Length && shredderCounts[hotbar.SelectedIndex] > 0;

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
            if (!movement.CanUseTools || (mining != null && mining.IsCarryingOre))
            {
                CancelPlacement();
                return;
            }

            if (hotbar != null && hotbar.SelectedIndex != lastSelectedSlot)
            {
                lastSelectedSlot = hotbar.SelectedIndex;
                rotationOffset = 0f;
            }

            var keyboard = Keyboard.current;
            bool interact = keyboard != null && keyboard.eKey.wasPressedThisFrame &&
                (mining == null || !mining.InteractionUsedThisFrame);
            bool aimedPickup = mining != null && mining.TryAim(out RaycastHit hit) && mining.CanPickUpTarget(hit.collider);
            if (interact && mining != null && mining.TryAim(out RaycastHit machineHit) &&
                TryPickUpMachine(Shredder.Find(machineHit.collider))) return;
            if (!CanPlace()) { CancelPlacement(); return; }
            if (interact && !aimedPickup) RotateSelected(45f);
            UpdatePreview();
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) TryPlaceSelected();
        }

        private void UpdatePreview()
        {
            bool hasFloor = TryGetFloorPose(out Pose pose);
            if (!hasFloor)
            {
                Vector3 facing = Vector3.ProjectOnPlane(-playerCamera.transform.forward, Vector3.up);
                if (facing.sqrMagnitude < 0.001f) facing = transform.forward;
                pose = new Pose(playerCamera.transform.position + playerCamera.transform.forward * 2.2f -
                    Vector3.up * footprint.center.y,
                    Quaternion.AngleAxis(rotationOffset, Vector3.up) * Quaternion.LookRotation(facing));
            }
            ShowPreview(pose, hasFloor && CanPlaceAt(pose));
        }

        internal bool TryPlaceSelected()
        {
            if (!HasSelectedShredder || !TryGetFloorPose(out Pose pose) || !CanPlaceAt(pose)) return false;
            int slot = hotbar.SelectedIndex;
            var machine = Instantiate(shredderPrefab, pose.position, pose.rotation);
            SceneManager.MoveGameObjectToScene(machine, gameObject.scene);
            var processor = machine.GetComponentInChildren<Shredder>();
            if (processor != null) processor.ConfigureModel(slot);
            shredderCounts[slot]--;
            RefreshSlot(slot);
            if (!HasSelectedShredder) HidePreview();
            return true;
        }

        internal void RotateSelected(float degrees)
        {
            if (!HasSelectedShredder) return;
            rotationOffset = Mathf.Repeat(rotationOffset + degrees, 360f);
        }

        internal bool TryPickUpMachine(Shredder machine)
        {
            if (machine == null || !machine.CanPickUp || (mining != null && mining.IsCarryingOre) ||
                !GiveShredder(machine.ModelIndex)) return false;
            hotbar.SelectSlot(machine.ModelIndex);
            if (mining != null) mining.MarkInteractionUsed();
            machine.Root.SetActive(false);
            Destroy(machine.Root);
            return true;
        }

        internal bool TryThrowSelected(float speed, float charge)
        {
            if (!HasSelectedShredder || !ItemDropPhysics.TryGetPose(playerCamera,
                    new Bounds(footprint.center, footprint.size), out Pose pose)) return false;
            int slot = hotbar.SelectedIndex;
            var machine = Instantiate(shredderPrefab, pose.position, pose.rotation);
            SceneManager.MoveGameObjectToScene(machine, gameObject.scene);
            var processor = machine.GetComponentInChildren<Shredder>();
            processor.ConfigureModel(slot);
            processor.PackForPickup();
            var body = machine.GetComponent<Rigidbody>();
            ItemDropPhysics.Launch(body, playerCamera, speed, charge);
            shredderCounts[slot]--;
            RefreshSlot(slot);
            if (!HasSelectedShredder) HidePreview();
            return true;
        }

        private bool CanPlace()
        {
            if (playerCamera == null || hotbar == null || !hotbar.isActiveAndEnabled || footprint == null ||
                !movement.CanUseTools || (mining != null && mining.IsCarryingOre))
                return false;

            return HasSelectedShredder;
        }

        private bool TryGetFloorPose(out Pose pose)
        {
            pose = default;
            Transform eye = playerCamera.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, placementReach,
                    placementLayers, QueryTriggerInteraction.Ignore) || !IsFloor(hit))
                return false;

            Vector3 normal = hit.normal;
            Vector3 facingPlayer = Vector3.ProjectOnPlane(-eye.forward, normal);
            if (facingPlayer.sqrMagnitude < 0.001f) facingPlayer = Vector3.ProjectOnPlane(eye.up, normal);
            Quaternion rotation = Quaternion.AngleAxis(rotationOffset, normal) * Quaternion.LookRotation(facingPlayer, normal);
            float bottom = footprint.center.y - footprint.size.y * 0.5f;
            Vector3 baseCenter = new Vector3(footprint.center.x, bottom, footprint.center.z);
            pose = new Pose(hit.point + normal * FloorClearance - rotation * baseCenter, rotation);
            return true;
        }

        private bool CanPlaceAt(Pose pose)
        {
            Vector3 halfSize = footprint.size * 0.5f;
            Vector3 center = pose.position + pose.rotation * footprint.center;
            Vector3 normal = pose.rotation * Vector3.up;
            if (Physics.CheckBox(center, halfSize, pose.rotation, placementLayers, QueryTriggerInteraction.Ignore))
                return false;

            // Check the base corners so the machine cannot balance over a ledge.
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + pose.rotation * new Vector3(x * halfSize.x * 0.9f, -halfSize.y, z * halfSize.z * 0.9f);
                if (!Physics.Raycast(corner + normal * 0.12f, -normal, out RaycastHit support,
                        0.28f, placementLayers, QueryTriggerInteraction.Ignore) || !IsFloor(support) ||
                    Vector3.Dot(support.normal, normal) < 0.99f ||
                    Mathf.Abs(Vector3.Dot(corner - support.point, normal) - FloorClearance) > 0.035f)
                    return false;
            }

            return true;
        }

        private bool IsFloor(RaycastHit hit) => hit.rigidbody == null &&
            hit.normal.y >= Mathf.Cos(maximumSlope * Mathf.Deg2Rad);

        private void ShowPreview(Pose pose, bool canPlace)
        {
            if (previewMaterial == null) return;
            if (preview == null)
            {
                preview = Instantiate(shredderPrefab);
                preview.name = "Zerschredderer (Vorschau)";
                foreach (var processor in preview.GetComponentsInChildren<Shredder>()) processor.enabled = false;
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
            HidePreview();
        }

        private void OnDisable() => CancelPlacement();

        private void OnDestroy()
        {
            if (preview != null) Destroy(preview);
        }
    }
}
