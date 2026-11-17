using ToxicDepths.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace ToxicDepths.Mining
{
    [AddComponentMenu("Toxic Depths/Mining/Player Mining")]
    [RequireComponent(typeof(PlayerVitals), typeof(PlayerMovement))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerMining : MonoBehaviour
    {
        private const int PickaxeSlot = 5;
        private const string ShopCounterLayer = "Shop Counter";

        [Header("Referenzen")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform heldPickaxe;
        [SerializeField] private HotbarController hotbar;
        [SerializeField] private DynamicCrosshair crosshair;
        [SerializeField] private MeshFilter carryHand;
        [SerializeField] private Material oreOutlineMaterial;

        [Header("Abbauen")]
        [SerializeField, Min(0.1f)] private float reach = 3f;
        [SerializeField, Min(1)] private int damagePerHit = 1;
        [Tooltip("Verbraucht zusaetzlich Energie gemaess PlayerVitals.energyPerStamina.")]
        [SerializeField, Min(0f)] private float staminaPerSwing = 12f;
        [SerializeField] private LayerMask hitLayers = Physics.DefaultRaycastLayers;

        [Header("Tragen und Werfen")]
        [SerializeField, Min(0.1f)] private float tapThrowSpeed = 3.2f;
        [FormerlySerializedAs("throwSpeed")]
        [SerializeField, Min(0.1f)] private float chargedThrowSpeed = 9f;
        [SerializeField, Min(0.05f)] private float throwChargeTime = 1.25f;
        [SerializeField] private Vector3 carryOffset = new Vector3(0f, -0.3f, 1.24f);

        [Header("Schlaganimation")]
        [SerializeField, Min(0.1f)] private float swingDuration = 0.7f;
        [SerializeField, Range(0.3f, 0.7f)] private float impactTime = 0.45f;
        [SerializeField] private Vector3 windupEuler = new Vector3(-30f, 10f, -18f);
        [SerializeField] private Vector3 strikeEuler = new Vector3(48f, -12f, 25f);
        [SerializeField] private Vector3 windupOffset = new Vector3(0.06f, 0.08f, -0.08f);
        [SerializeField] private Vector3 strikeOffset = new Vector3(-0.18f, -0.14f, 0.14f);

        private PlayerVitals vitals;
        private PlayerMovement movement;
        private ShredderShop shop;
        private int shopCounterLayer;
        private Rigidbody displayedOutput;
        private Transform outputGrip;
        private Transform outputHand;
        private MeshFilter outputMesh;
        private MeshRenderer outputRenderer;
        private OreChunk heldOre;
        private OreChunk outlinedOre;
        private Transform carryGrip;
        private Camera carryCamera;
        private int carryLayer = -1;
        private bool restoreCarryLayer;
        private Transform leftHand;
        private Transform rightHand;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private bool poseCaptured;
        private bool holdToMine;
        private bool impactApplied;
        private float swingElapsed;
        private float retryAt;
        private float throwChargeStartedAt = -1f;

        public bool IsSwinging { get; private set; }
        public bool IsCarryingOre => heldOre != null;
        internal bool ShouldShowPickaxe => !IsCarryingOre &&
            (hotbar == null || hotbar.SelectedIndex == PickaxeSlot);
        public float SwingProgress => IsSwinging ? Mathf.Clamp01(swingElapsed / swingDuration) : 0f;
        private bool CanInteract => isActiveAndEnabled && movement != null && movement.CanUseTools &&
            playerCamera != null;
        private float ImpactTime => crosshair != null ? crosshair.ImpactTime : impactTime;
        private bool HeldItemsVisible => movement.isActiveAndEnabled && !movement.IsInConversation;

        private void Awake()
        {
            vitals = GetComponent<PlayerVitals>();
            movement = GetComponent<PlayerMovement>();
            TryGetComponent(out shop);
            shopCounterLayer = LayerMask.NameToLayer(ShopCounterLayer);
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            if (heldPickaxe != null)
            {
                restPosition = heldPickaxe.localPosition;
                restRotation = heldPickaxe.localRotation;
                poseCaptured = true;
            }

            if (hotbar != null) hotbar.OnSelectionChanged.AddListener(OnHotbarSelectionChanged);
            UpdatePickaxeVisibility();
        }

        private void Update()
        {
            if (!movement.isActiveAndEnabled || playerCamera == null)
                ReturnHeldOre();
            else if (IsCarryingOre)
                UpdateCarryPose();
            else if (carryGrip != null && carryGrip.gameObject.activeSelf)
                ReturnHeldOre();

            if (!CanInteract)
            {
                SetOutlinedOre(null);
                holdToMine = false;
                throwChargeStartedAt = -1f;
                CancelSwing();
                return;
            }

            var keyboard = Keyboard.current;
            bool interactPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            if (IsCarryingOre)
            {
                if (interactPressed && TryAim(out var carriedAim)) TryGreet(carriedAim.collider);
                UpdateThrowCharge(keyboard);
                return;
            }

            TryAim(out var aim);
            var pickup = GetPickup(aim.collider);
            SetOutlinedOre(pickup);
            if (interactPressed && ((pickup != null && TryPickUp(pickup)) || TryGreet(aim.collider)))
                return;

            if (!CanControlTool()) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!mouse.leftButton.isPressed) holdToMine = false;
            if (mouse.leftButton.wasPressedThisFrame) holdToMine = true;
            if (holdToMine && !IsSwinging && Time.time >= retryAt) TryStartSwing();
        }

        private bool CanControlTool()
        {
            return CanInteract && ShouldShowPickaxe && heldPickaxe != null && heldPickaxe.gameObject.activeInHierarchy;
        }

        public bool TryStartSwing()
        {
            if (IsSwinging || !CanControlTool()) return false;
            if (!vitals.TrySpendStamina(staminaPerSwing))
            {
                retryAt = Time.time + 0.2f;
                return false;
            }

            IsSwinging = true;
            swingElapsed = 0f;
            impactApplied = false;
            return true;
        }

        private void LateUpdate()
        {
            RefreshOutputView();

            if (!IsSwinging) return;
            if (!CanControlTool()) { CancelSwing(); return; }

            swingElapsed += Time.deltaTime;
            float progress = SwingProgress;
            AnimateTool(progress);
            if (crosshair != null) crosshair.SetSwingProgress(progress);

            if (!impactApplied && progress >= ImpactTime)
            {
                impactApplied = true;
                // Resolve the nearest surface now: moving away or aiming behind a wall cannot hit.
                FindTarget()?.ApplyHit(damagePerHit);
            }

            if (progress >= 1f) CancelSwing();
        }

        public OreDeposit FindTarget()
        {
            return TryAim(out RaycastHit hit) ? GetDeposit(hit.collider) : null;
        }

        internal bool TryAim(out RaycastHit hit) => TryAimOnLayers(hitLayers, out hit);

        private bool TryAimOnLayers(int layers, out RaycastHit hit)
        {
            hit = default;
            return playerCamera != null && Physics.Raycast(playerCamera.transform.position,
                playerCamera.transform.forward, out hit, reach, layers, QueryTriggerInteraction.Ignore);
        }

        private static OreDeposit GetDeposit(Collider collider)
        {
            var ore = collider.GetComponentInParent<OreDeposit>();
            return ore != null && ore.isActiveAndEnabled && !ore.IsDepleted ? ore : null;
        }

        private static OreChunk GetPickup(Collider collider)
        {
            var chunk = collider != null ? collider.GetComponentInParent<OreChunk>() : null;
            return chunk != null && chunk.isActiveAndEnabled && !chunk.IsHeld ? chunk : null;
        }

        private bool TryGreet(Collider collider)
        {
            if (collider != null && collider.gameObject.layer == shopCounterLayer)
            {
                int greetingLayers = hitLayers.value & ~(1 << shopCounterLayer);
                if (!TryAimOnLayers(greetingLayers, out var greetingHit)) return false;
                collider = greetingHit.collider;
            }

            var frog = collider != null ? collider.GetComponentInParent<FrogGreeting>() : null;
            return frog != null && frog.isActiveAndEnabled && frog.Show(playerCamera, movement);
        }

        private void SetOutlinedOre(OreChunk chunk)
        {
            if (outlinedOre == chunk) return;
            if (outlinedOre != null) outlinedOre.SetOutline(null);
            outlinedOre = chunk;
            if (outlinedOre != null) outlinedOre.SetOutline(oreOutlineMaterial);
        }

        private bool TryPickUp(OreChunk chunk)
        {
            if (IsCarryingOre || !chunk.TryPickUp()) return false;
            SetOutlinedOre(null);
            heldOre = chunk;
            holdToMine = false;
            CancelSwing();
            vitals.StopSprint();
            SetCarryView(true);
            UpdateCarryPose();
            return true;
        }

        private void UpdateThrowCharge(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                throwChargeStartedAt = -1f;
                return;
            }

            var throwKey = keyboard.qKey;
            if (throwKey.wasPressedThisFrame) throwChargeStartedAt = Time.time;
            if (throwChargeStartedAt < 0f || throwKey.isPressed) return;

            float charge = Mathf.Clamp01((Time.time - throwChargeStartedAt) / throwChargeTime);
            throwChargeStartedAt = -1f;
            if (throwKey.wasReleasedThisFrame) ThrowHeldOre(charge);
        }

        private void ThrowHeldOre(float charge)
        {
            var cameraTransform = playerCamera.transform;
            var shredder = TryAim(out RaycastHit hit) ? hit.collider.GetComponentInChildren<Shredder>() : null;
            bool fed = shredder != null && shredder.CanAcceptOre &&
                heldOre.TryThrowInto(cameraTransform.position, shredder.transform.position);
            if (!fed && !TryThrowNormally(cameraTransform, charge)) return;

            heldOre = null;
            SetCarryView(false);
        }

        private bool TryThrowNormally(Transform cameraTransform, float charge)
        {
            float speed = Mathf.Lerp(tapThrowSpeed, chargedThrowSpeed, charge);
            Vector3 velocity = cameraTransform.forward * speed;
            velocity.y = Mathf.Max(velocity.y + 0.8f, Mathf.Lerp(1f, 2f, charge));
            if (heldOre.TryThrow(cameraTransform.position, carryGrip.position, carryGrip.rotation, velocity))
                return true;

            // A steep view angle can put the held pose inside the floor or ceiling.
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Quaternion upright = Quaternion.LookRotation(forward);
            Vector3 releasePosition = cameraTransform.position + upright * carryGrip.localPosition;
            return heldOre.TryThrow(cameraTransform.position, releasePosition, upright, velocity);
        }

        private void ReturnHeldOre()
        {
            throwChargeStartedAt = -1f;
            if (IsCarryingOre)
            {
                heldOre.ReturnToPickupSpot();
                heldOre = null;
            }

            if (carryGrip != null && carryGrip.gameObject.activeSelf) SetCarryView(false);
        }

        private void UpdateCarryPose()
        {
            var eye = playerCamera.transform;
            Vector3 size = heldOre.Size;
            Vector3 offset = carryOffset;
            offset.z = Mathf.Max(offset.z, Mathf.Max(size.x, size.z) * 0.5f + 0.9f);
            float eyeHeight = eye.position.y - transform.position.y;
            offset.y = Mathf.Max(offset.y, size.y * 0.5f + 0.12f - eyeHeight);
            carryGrip.localPosition = offset;
            heldOre.SetCarryPose(carryGrip);
        }

        private void SetCarryView(bool carrying)
        {
            if (carrying && carryGrip == null)
            {
                carryGrip = new GameObject("Erz-Tragepunkt").transform;
                carryGrip.SetParent(playerCamera.transform, false);
                CreateCarryCamera();
                if (carryHand != null && carryHand.TryGetComponent(out MeshRenderer source))
                {
                    leftHand = CreateHand(carryGrip, "Linke Hand", -1f, source);
                    rightHand = CreateHand(carryGrip, "Rechte Hand", 1f, source);
                }
            }

            if (carrying && leftHand != null && rightHand != null)
            {
                Vector3 size = heldOre.Size;
                rightHand.localPosition = new Vector3(size.x * 0.32f + 0.02f, -size.y * 0.20f, -size.z * 0.3f);
                leftHand.localPosition = Vector3.Scale(rightHand.localPosition, new Vector3(-1f, 1f, 1f));
            }

            if (carrying && carryLayer >= 0) heldOre.SetViewLayer(carryLayer);
            if (carryGrip != null) carryGrip.gameObject.SetActive(carrying);
            RefreshCarryModelVisibility();
            UpdateCarryCamera();
            UpdatePickaxeVisibility();
        }

        internal void RefreshHeldItemVisibility()
        {
            UpdatePickaxeVisibility();
            RefreshCarryModelVisibility();
            RefreshOutputView();
        }

        private void RefreshCarryModelVisibility()
        {
            if (!IsCarryingOre) return;

            bool visible = HeldItemsVisible;
            heldOre.SetHeldModelVisible(visible);
            if (leftHand != null) leftHand.gameObject.SetActive(visible);
            if (rightHand != null) rightHand.gameObject.SetActive(visible);
        }

        private void RefreshOutputView()
        {
            SetOutputView(HeldItemsVisible && !IsCarryingOre && playerCamera != null && shop != null
                ? shop.SelectedOutput : null);
        }

        private void SetOutputView(Rigidbody output)
        {
            if (displayedOutput == output) return;

            displayedOutput = output;
            if (output != null)
            {
                if (outputGrip == null) CreateOutputGrip();

                var source = output.GetComponentInChildren<MeshRenderer>();
                if (source == null || !source.TryGetComponent(out MeshFilter mesh))
                {
                    outputGrip.gameObject.SetActive(false);
                    UpdateCarryCamera();
                    return;
                }

                outputMesh.sharedMesh = mesh.sharedMesh;
                outputRenderer.sharedMaterials = source.sharedMaterials;
                bool emerald = output.TryGetComponent(out ShredderOutputPickup pickup) &&
                    pickup.ItemKind == ShredderOutputPickup.Kind.Emerald;
                Vector3 position = mesh.transform.localPosition;
                Quaternion rotation = mesh.transform.localRotation;
                Vector3 scale = mesh.transform.localScale;
                if (emerald)
                {
                    position.y = 0.02f;
                    rotation = Quaternion.Euler(-89.3f, 197.68f, 0f);
                    scale = Vector3.one * 26.1f;
                }

                outputMesh.transform.SetLocalPositionAndRotation(position, rotation);
                outputMesh.transform.localScale = scale;

                if (outputHand != null)
                {
                    outputHand.SetLocalPositionAndRotation(
                        emerald ? new Vector3(0.079f, -0.112f, -0.052f) : new Vector3(0.035f, -0.105f, 0.02f),
                        emerald ? Quaternion.Euler(24f, -34f, 43f) : Quaternion.Euler(36f, -22f, 32f));
                }
            }

            if (outputGrip != null) outputGrip.gameObject.SetActive(output != null);
            UpdateCarryCamera();
        }

        private void CreateOutputGrip()
        {
            CreateCarryCamera();
            outputGrip = new GameObject("Material in der Hand").transform;
            outputGrip.SetParent(playerCamera.transform, false);
            outputGrip.SetLocalPositionAndRotation(new Vector3(0.27f, -0.17f, 0.74f),
                Quaternion.Euler(-6f, -20f, -12f));

            if (carryHand != null && carryHand.TryGetComponent(out MeshRenderer source))
                outputHand = CreateHand(outputGrip, "Rechte Hand", 1f, source);

            var model = new GameObject("Materialmodell", typeof(MeshFilter), typeof(MeshRenderer));
            model.layer = carryLayer >= 0 ? carryLayer : Physics.IgnoreRaycastLayer;
            model.transform.SetParent(outputGrip, false);
            outputMesh = model.GetComponent<MeshFilter>();
            outputRenderer = model.GetComponent<MeshRenderer>();
            outputRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void UpdateCarryCamera()
        {
            if (carryCamera == null || playerCamera == null) return;

            bool visible = IsCarryingOre || (outputGrip != null && outputGrip.gameObject.activeSelf);
            if (carryCamera.enabled == visible) return;

            int mask = carryCamera.cullingMask;
            if (visible)
            {
                restoreCarryLayer = (playerCamera.cullingMask & mask) != 0;
                playerCamera.cullingMask &= ~mask;
            }
            else if (restoreCarryLayer)
            {
                playerCamera.cullingMask |= mask;
            }

            carryCamera.enabled = visible;
        }

        private void UpdatePickaxeVisibility()
        {
            if (heldPickaxe != null)
                heldPickaxe.gameObject.SetActive(HeldItemsVisible && ShouldShowPickaxe);
        }

        private void OnHotbarSelectionChanged(int index)
        {
            if (index != PickaxeSlot)
            {
                holdToMine = false;
                CancelSwing();
            }

            UpdatePickaxeVisibility();
        }

        private void CreateCarryCamera()
        {
            if (carryCamera != null) return;
            int layer = LayerMask.NameToLayer("Held Ore");
            if (layer < 0) return;
            var stack = playerCamera.GetUniversalAdditionalCameraData().cameraStack;
            if (stack == null) return;

            var view = new GameObject("Erz-Kamera", typeof(Camera));
            view.transform.SetParent(playerCamera.transform, false);
            carryCamera = view.GetComponent<Camera>();
            carryCamera.CopyFrom(playerCamera);
            carryCamera.enabled = false;
            carryCamera.cullingMask = 1 << layer;
            carryCamera.useOcclusionCulling = false;

            // Clear world depth so the floor and ceiling cannot cut through the held model.
            carryCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            stack.Add(carryCamera);
            carryLayer = layer;
        }

        private Transform CreateHand(Transform grip, string handName, float side, MeshRenderer source)
        {
            var hand = new GameObject(handName, typeof(MeshFilter), typeof(MeshRenderer));
            hand.layer = carryLayer >= 0 ? carryLayer : carryHand.gameObject.layer;
            hand.transform.SetParent(grip, false);
            hand.transform.localRotation = Quaternion.Euler(36f, -side * 22f, side * 32f);
            Vector3 scale = carryHand.transform.localScale;
            hand.transform.localScale = new Vector3(scale.x * side, scale.y, scale.z);
            hand.GetComponent<MeshFilter>().sharedMesh = carryHand.sharedMesh;
            var renderer = hand.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return hand.transform;
        }

        private void AnimateTool(float progress)
        {
            float impact = ImpactTime;
            float windupEnd = impact * 0.55f;
            Vector3 offset;
            Vector3 rotation;
            if (progress < windupEnd)
            {
                float t = Mathf.SmoothStep(0f, 1f, progress / windupEnd);
                offset = Vector3.Lerp(Vector3.zero, windupOffset, t);
                rotation = Vector3.Lerp(Vector3.zero, windupEuler, t);
            }
            else if (progress < impact)
            {
                float t = (progress - windupEnd) / (impact - windupEnd);
                t = 1f - (1f - t) * (1f - t);
                offset = Vector3.Lerp(windupOffset, strikeOffset, t);
                rotation = Vector3.Lerp(windupEuler, strikeEuler, t);
            }
            else
            {
                float t = Mathf.SmoothStep(0f, 1f, (progress - impact) / (1f - impact));
                offset = Vector3.Lerp(strikeOffset, Vector3.zero, t);
                rotation = Vector3.Lerp(strikeEuler, Vector3.zero, t);
            }
            heldPickaxe.localPosition = restPosition + offset;
            heldPickaxe.localRotation = Quaternion.Euler(rotation) * restRotation;
        }

        private void CancelSwing()
        {
            if (!IsSwinging) return;
            IsSwinging = false;
            if (poseCaptured && heldPickaxe != null)
            {
                heldPickaxe.localPosition = restPosition;
                heldPickaxe.localRotation = restRotation;
            }
            if (crosshair != null) crosshair.ResetCrosshair();
        }

        private void OnDisable()
        {
            if (hotbar != null) hotbar.OnSelectionChanged.RemoveListener(OnHotbarSelectionChanged);
            SetOutlinedOre(null);
            holdToMine = false;
            CancelSwing();
            ReturnHeldOre();
            SetOutputView(null);
        }

        private void OnDestroy()
        {
            if (carryCamera != null)
            {
                if (playerCamera != null && playerCamera.TryGetComponent(out UniversalAdditionalCameraData data))
                    data.cameraStack?.Remove(carryCamera);
                Destroy(carryCamera.gameObject);
            }

            if (carryGrip != null) Destroy(carryGrip.gameObject);
            if (outputGrip != null) Destroy(outputGrip.gameObject);
        }

        private void OnValidate()
        {
            reach = Mathf.Max(0.1f, reach);
            swingDuration = Mathf.Max(0.1f, swingDuration);
            damagePerHit = Mathf.Max(1, damagePerHit);
            tapThrowSpeed = Mathf.Max(0.1f, tapThrowSpeed);
            chargedThrowSpeed = Mathf.Max(tapThrowSpeed, chargedThrowSpeed);
            throwChargeTime = Mathf.Max(0.05f, throwChargeTime);
        }
    }
}
