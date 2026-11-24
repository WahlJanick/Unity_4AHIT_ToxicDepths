using ToxicDepths.UI;
using ToxicDepths.Mining;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace ToxicDepths
{
    [RequireComponent(typeof(PlayerMovement))]
    [DisallowMultipleComponent]
    public sealed class IndustrialScanner : MonoBehaviour
    {
        private const int ScannerSlot = 6;

        [SerializeField] private HotbarController hotbar;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private GameObject scannerModel;
        [SerializeField] private MeshRenderer rightHand;

        [Header("Scan")]
        [SerializeField, Min(0.1f)] private float scanRange = 5f;
        [SerializeField, Min(0.1f)] private float scanDuration = 1.5f;
        [SerializeField, Range(0f, 15f)] private float aimAssistAngle = 6f;
        [SerializeField] private LayerMask scanLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private Material wireframeMaterial;
        [SerializeField] private Material beamMaterial;
        [SerializeField] private Texture2D oreTypeIcon;
        [SerializeField] private Texture2D poisonIcon;

        [Header("Scan Audio")]
        [SerializeField] private AudioClip scanStart;
        [SerializeField] private AudioClip scanLoop;
        [SerializeField] private AudioClip scanComplete;
        [SerializeField] private AudioClip scanCancelled;

        private PlayerMovement movement;
        private GameObject equippedModel;
        private Vector3 muzzle;
        private OreScanEffects effects;
        private OreDeposit target;
        private OreDeposit fadingTarget;
        private float elapsed;
        private float fadeRemaining;
        private float fadeProgress;
        private OreDeposit hoveredScan;
        private OreDeposit lastDisplayedScan;
        private bool scanReady;
        private float resultRemaining;
        private float beamWeight;
        private float beamExtension;
        private Vector3 beamHit;
        private Vector3 steeredHit;
        private OreDeposit beamOre;
        private Collider lockedCollider;
        private Vector3 lockedPoint;
        private readonly RaycastHit[] aimHits = new RaycastHit[32];
        private AudioSource cues;
        private AudioSource loop;
        private OreScanHUD hud;

        private const float FadeDuration = 0.32f;
        private const float FlashDuration = 0.24f;
        private static readonly Quaternion RestPose = Quaternion.Euler(8f, -34f, 8f);

        private void Awake() => movement = GetComponent<PlayerMovement>();

        private void OnEnable()
        {
            if (playerCamera == null || wireframeMaterial == null || beamMaterial == null) return;
            effects = new OreScanEffects(playerCamera, wireframeMaterial, beamMaterial);
            foreach (var ore in FindObjectsByType<OreDeposit>()) PrepareOre(ore);
            OreDeposit.Available += PrepareOre;
        }

        private void PrepareOre(OreDeposit ore) => effects.Prepare(ore);

        private void LateUpdate()
        {
            bool equipped = movement.isActiveAndEnabled && hotbar != null && playerCamera != null &&
                scannerModel != null && hotbar.SelectedIndex == ScannerSlot && !hotbar.SelectionLocked;

            if (equipped && equippedModel == null) CreateHeldModel();
            if (equippedModel != null && equippedModel.activeSelf != equipped) equippedModel.SetActive(equipped);

            if (!equipped || !movement.CanUseTools || effects == null)
            {
                if (scanReady) ClearScan(true);
            }
            else
            {
                scanReady = true;
                UpdateScan(Time.deltaTime);
            }
            effects?.DrawScanned(target);
            hoveredScan = GetHoveredScan();
        }

        private OreDeposit GetHoveredScan()
        {
            if (target != null || resultRemaining > 0f || playerCamera == null || !movement.CanUseTools)
                return null;
            Transform view = playerCamera.transform;
            if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit,
                scanRange, scanLayers, QueryTriggerInteraction.Ignore)) return null;
            var ore = hit.collider.GetComponentInParent<OreDeposit>();
            return Valid(ore) && ore.WasScanned ? ore : null;
        }

        private void UpdateScan(float step)
        {
            bool held = Mouse.current != null && Mouse.current.leftButton.isPressed;
            var view = playerCamera.transform;
            OreDeposit aimedOre = null;
            if (held)
            {
                aimedOre = ResolveAim(view, beamWeight <= 0f);
            }
            else lockedCollider = null;

            beamWeight = Mathf.MoveTowards(beamWeight, held ? 1f : 0f, step * (held ? 9f : 6f));
            if (held) beamExtension = Mathf.MoveTowards(beamExtension, 1f, step * 4.5f);
            else if (beamWeight <= 0f) beamExtension = 0f;
            Vector3 origin = equippedModel.transform.TransformPoint(muzzle);
            if (aimedOre != null)
            {
                if (TryReachOre(origin, beamHit, aimedOre, out RaycastHit reached) ||
                    TryReachOre(origin, lockedCollider.bounds.center, aimedOre, out reached))
                    beamHit = reached.point;
                else
                {
                    aimedOre = null;
                    lockedCollider = null;
                }
            }
            if (held) steeredHit = Vector3.Lerp(steeredHit, beamHit, 1f - Mathf.Exp(-25f * step));
            UpdateAimPose(held, step);
            origin = equippedModel.transform.TransformPoint(muzzle);
            if (held) beamOre = aimedOre;
            Vector3 visualHit = steeredHit;
            bool surfaceContact = false;
            Vector3 visualDirection = visualHit - origin;
            if (beamWeight > 0f && visualDirection.sqrMagnitude > 0.0001f &&
                Physics.Raycast(origin, visualDirection.normalized, out RaycastHit visualSurface,
                    Mathf.Min(visualDirection.magnitude + 0.025f, scanRange), scanLayers, QueryTriggerInteraction.Ignore))
            {
                visualHit = visualSurface.point;
                surfaceContact = Valid(beamOre) && visualSurface.collider.GetComponentInParent<OreDeposit>() == beamOre;
            }
            effects.DrawBeam(origin, visualHit, beamWeight, scanRange, beamExtension, surfaceContact);

            if (!ReferenceEquals(target, null) && (!Valid(target) || aimedOre != target)) EndScan(false);
            else if (target == null && aimedOre != null && !aimedOre.WasScanned) BeginScan(aimedOre);

            fadeRemaining = Mathf.Max(0f, fadeRemaining - step);
            resultRemaining = Mathf.Max(0f, resultRemaining - step);
            if (fadeRemaining > 0f && Valid(fadingTarget) && fadingTarget != target)
            {
                float glow = fadeProgress >= 1f ? Mathf.Clamp01((fadeRemaining - FadeDuration) / FlashDuration) : 0f;
                effects.DrawOre(fadingTarget, fadeProgress, Mathf.SmoothStep(0f, 1f, fadeRemaining / FadeDuration),
                    glow, fadingTarget == beamOre);
            }
            else if (fadeRemaining <= 0f) fadingTarget = null;

            if (target == null)
            {
                if (beamWeight > 0f && Valid(beamOre) && (fadeRemaining <= 0f || beamOre != fadingTarget))
                    effects.DrawOre(beamOre, 0f, 0f, 0f, true);
                return;
            }
            elapsed += step;
            float progress = Mathf.Clamp01(elapsed / scanDuration);
            effects.DrawOre(target, progress, Mathf.SmoothStep(0f, 1f, elapsed * 10f), 0f, target == beamOre);
            if (progress >= 1f) EndScan(true);
        }

        private static bool Valid(OreDeposit ore) => ore != null && ore.isActiveAndEnabled && !ore.IsDepleted;

        private OreDeposit ResolveAim(Transform view, bool beginBeam)
        {
            bool hitSomething = Physics.Raycast(view.position, view.forward, out RaycastHit straight,
                scanRange, scanLayers, QueryTriggerInteraction.Ignore);
            beamHit = hitSomething ? straight.point : view.position + view.forward * scanRange;
            if (beginBeam) steeredHit = beamHit;
            if (hitSomething)
            {
                var directOre = straight.collider.GetComponentInParent<OreDeposit>();
                if (Valid(directOre))
                {
                    if (!directOre.WasScanned) return LockOre(directOre, straight);
                    lockedCollider = null;
                    return null;
                }
            }

            if (aimAssistAngle <= 0f) return null;
            if (lockedCollider != null && lockedCollider.enabled)
            {
                var previous = lockedCollider.GetComponentInParent<OreDeposit>();
                Vector3 previousPoint = lockedCollider.transform.TransformPoint(lockedPoint);
                if (Valid(previous) && !previous.WasScanned && InAimCone(view, previousPoint, aimAssistAngle + 3f) &&
                    TryReachOre(view.position, previousPoint, previous, out RaycastHit retained))
                    return LockOre(previous, retained);
            }

            lockedCollider = null;
            float radius = Mathf.Sin(aimAssistAngle * Mathf.Deg2Rad) * scanRange;
            int count = Physics.SphereCastNonAlloc(view.position, radius, view.forward, aimHits,
                scanRange, scanLayers, QueryTriggerInteraction.Ignore);
            OreDeposit bestOre = null;
            RaycastHit bestSurface = default;
            float bestAlignment = 0f;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = aimHits[i];
                var ore = candidate.collider.GetComponentInParent<OreDeposit>();
                if (!Valid(ore) || ore.WasScanned || !InAimCone(view, candidate.point, aimAssistAngle)) continue;
                float alignment = Vector3.Dot(view.forward, (candidate.point - view.position).normalized);
                if (alignment <= bestAlignment || !TryReachOre(view.position, candidate.point, ore, out RaycastHit surface))
                    continue;
                bestOre = ore;
                bestSurface = surface;
                bestAlignment = alignment;
            }
            return bestOre != null ? LockOre(bestOre, bestSurface) : null;
        }

        private OreDeposit LockOre(OreDeposit ore, RaycastHit surface)
        {
            beamHit = surface.point;
            lockedCollider = surface.collider;
            lockedPoint = lockedCollider.transform.InverseTransformPoint(beamHit);
            return ore;
        }

        private static bool InAimCone(Transform view, Vector3 point, float angle)
        {
            Vector3 direction = point - view.position;
            return Vector3.Dot(view.forward, direction.normalized) >= Mathf.Cos(angle * Mathf.Deg2Rad);
        }

        private bool TryReachOre(Vector3 origin, Vector3 point, OreDeposit ore, out RaycastHit surface)
        {
            Vector3 direction = point - origin;
            float distance = direction.magnitude;
            surface = default;
            return distance > 0.01f && distance <= scanRange &&
                Physics.Raycast(origin, direction / distance, out surface, Mathf.Min(distance + 0.025f, scanRange),
                    scanLayers, QueryTriggerInteraction.Ignore) && surface.collider.GetComponentInParent<OreDeposit>() == ore;
        }

        private void UpdateAimPose(bool aiming, float step)
        {
            Transform grip = equippedModel.transform;
            Quaternion pose = RestPose;
            if (aiming)
            {
                Transform view = playerCamera.transform;
                Vector3 direction = view.InverseTransformPoint(steeredHit) -
                    (grip.localPosition + grip.localRotation * muzzle);
                if (direction.z > 0.025f) pose = Quaternion.LookRotation(direction, Vector3.up);
            }
            grip.localRotation = Quaternion.Slerp(grip.localRotation, pose, 1f - Mathf.Exp(-24f * step));
        }

        private void BeginScan(OreDeposit ore)
        {
            target = ore;
            elapsed = 0f;
            resultRemaining = 0f;
            (hud ??= new OreScanHUD(oreTypeIcon, poisonIcon)).Begin();
            PlayCue(scanStart);
            if (scanLoop == null) return;
            loop ??= CreateAudioSource(true);
            loop.clip = scanLoop;
            loop.Play();
        }

        private void EndScan(bool success)
        {
            fadingTarget = target;
            fadeProgress = success ? 1f : Mathf.Clamp01(elapsed / scanDuration);
            fadeRemaining = FadeDuration + (success ? FlashDuration : 0f);
            target = null;
            if (loop != null) loop.Stop();
            if (success)
            {
                fadingTarget.MarkScanned();
                resultRemaining = 2.5f;
                SetDisplayedScan(fadingTarget);
            }
            PlayCue(success ? scanComplete : scanCancelled);
        }

        private void ClearScan(bool playCancel)
        {
            bool interrupted = !ReferenceEquals(target, null);
            if (loop != null) loop.Stop();
            if (cues != null) cues.Stop();
            target = fadingTarget = beamOre = null;
            hoveredScan = null;
            lockedCollider = null;
            elapsed = fadeRemaining = resultRemaining = beamWeight = beamExtension = 0f;
            scanReady = false;
            if (playCancel && interrupted) PlayCue(scanCancelled);
        }

        private AudioSource CreateAudioSource(bool looping)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = looping;
            source.spatialBlend = 0f;
            return source;
        }

        private void PlayCue(AudioClip clip)
        {
            if (clip == null) return;
            cues ??= CreateAudioSource(false);
            cues.PlayOneShot(clip);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (target != null) hud.DrawProgress(elapsed / scanDuration);
            else if (resultRemaining > 0f) hud.DrawResult();
            else if (Valid(hoveredScan))
            {
                SetDisplayedScan(hoveredScan);
                hud.DrawResult();
            }
        }

        private void SetDisplayedScan(OreDeposit ore)
        {
            if (ore == lastDisplayedScan) return;
            (hud ??= new OreScanHUD(oreTypeIcon, poisonIcon)).Complete(ore.ScanName, ore.ReleasesPoison);
            lastDisplayedScan = ore;
        }

        private void CreateHeldModel()
        {
            equippedModel = new GameObject("Industrial Scanner (Held)");
            Transform grip = equippedModel.transform;
            grip.SetParent(playerCamera.transform, false);
            grip.SetLocalPositionAndRotation(new Vector3(0.36f, -0.33f, 0.77f), RestPose);

            Transform model = Instantiate(scannerModel, grip, false).transform;
            Transform hand = null;
            if (rightHand != null)
            {
                hand = Instantiate(rightHand.gameObject, grip, false).transform;
                hand.name = "Pilzling (Right Hand)";
                hand.localScale *= 0.75f;
            }

            foreach (Transform part in grip.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = Physics.IgnoreRaycastLayer;

            var filter = model.GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer renderer)) return;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Mesh mesh = filter.sharedMesh;
            Bounds bounds = mesh.bounds;
            Vector3 size = bounds.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            model.localScale *= 0.41f / Mathf.Max(size[axis] * filter.transform.lossyScale.x, 0.0001f);

            // The narrow handle and wide scanner body identify the two ends in imported mesh space.
            Vector3[] vertices = mesh.vertices;
            Bounds lower = Slice(vertices, bounds, axis, 0.24f);
            Bounds upper = Slice(vertices, bounds, axis, 0.76f);
            bool handleAtMinimum = lower.size.sqrMagnitude < upper.size.sqrMagnitude;
            Vector3 handle = handleAtMinimum ? lower.center : upper.center;
            Vector3 shoulder = Slice(vertices, bounds, axis, handleAtMinimum ? 0.33f : 0.67f).center;
            Vector3 bodyUp = Vector3.zero;
            bodyUp[axis] = handleAtMinimum ? 1f : -1f;
            Vector3 headDirection = bounds.center - handle;
            headDirection[axis] = 0f;

            Quaternion scannerPose = Quaternion.LookRotation(filter.transform.TransformDirection(headDirection),
                filter.transform.TransformDirection(bodyUp));
            model.rotation = grip.rotation * Quaternion.Inverse(scannerPose) * model.rotation;
            Vector3 handContact = Vector3.zero;
            if (hand != null)
            {
                Vector3 handleAxis = grip.InverseTransformDirection(filter.transform.TransformDirection(shoulder - handle));
                hand.localRotation = Quaternion.FromToRotation(hand.localRotation * Vector3.up, handleAxis) * hand.localRotation;
                handContact = grip.InverseTransformPoint(hand.TransformPoint(new Vector3(0.015f, -0.045f, -0.025f)));
            }
            model.localPosition += handContact - grip.InverseTransformPoint(filter.transform.TransformPoint(handle));

            // Only the upper head can contribute to the optical emitter.
            float headHeight = Mathf.Lerp(bounds.min[axis], bounds.max[axis], handleAtMinimum ? 0.64f : 0.36f);
            var modelToGrip = grip.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            float front = float.NegativeInfinity;
            foreach (Vector3 vertex in vertices)
                if ((vertex[axis] - headHeight) * bodyUp[axis] >= 0f)
                    front = Mathf.Max(front, modelToGrip.MultiplyPoint3x4(vertex).z);
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Vector3 vertex in vertices)
            {
                if ((vertex[axis] - headHeight) * bodyUp[axis] < 0f) continue;
                Vector3 local = modelToGrip.MultiplyPoint3x4(vertex);
                if (local.z < front - 0.018f) continue;
                sum += local;
                count++;
            }
            if (count > 0)
            {
                muzzle = sum / count;
                muzzle.z = front + 0.004f;
            }
        }

        private static Bounds Slice(Vector3[] vertices, Bounds bounds, int axis, float fraction)
        {
            float height = Mathf.Lerp(bounds.min[axis], bounds.max[axis], fraction);
            float tolerance = bounds.size[axis] * 0.035f;
            var slice = new Bounds(bounds.center, Vector3.zero);
            bool found = false;
            foreach (Vector3 vertex in vertices)
            {
                if (Mathf.Abs(vertex[axis] - height) > tolerance) continue;
                if (!found) slice.center = vertex;
                slice.Encapsulate(vertex);
                found = true;
            }
            return slice;
        }

        private void OnDisable()
        {
            OreDeposit.Available -= PrepareOre;
            ClearScan(false);
            effects?.Dispose();
            effects = null;
            if (equippedModel != null) equippedModel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (equippedModel != null) Destroy(equippedModel);
            if (cues != null) Destroy(cues);
            if (loop != null) Destroy(loop);
        }
    }
}
