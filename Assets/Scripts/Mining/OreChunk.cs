using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ToxicDepths.Mining
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class OreChunk : MonoBehaviour
    {
        private Rigidbody body;
        private BoxCollider hitbox;
        private MeshRenderer[] visuals;
        private bool[] hiddenRenderStates;
        private MeshRenderer[] outlineRenderers;
        private Pose[] restVisualPoses;
        private Pose heldVisualAdjustment = Pose.identity;
        private Vector3 pickupPosition;
        private Quaternion pickupRotation;
        private bool wasThrown;

        public bool IsHeld { get; private set; }
        public Vector3 Size => hitbox.size;

        internal static void Spawn(OreDeposit deposit)
        {
            GameObject root = null;
            Bounds bounds = default;
            foreach (var renderer in deposit.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled || !renderer.TryGetComponent(out MeshFilter filter) ||
                    filter.sharedMesh == null) continue;

                if (root == null)
                {
                    root = new GameObject($"{deposit.OreName} (Erzstück)");
                    SceneManager.MoveGameObjectToScene(root, deposit.gameObject.scene);
                    bounds = renderer.bounds;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                var part = new GameObject(renderer.name, typeof(MeshFilter), typeof(MeshRenderer));
                part.layer = deposit.gameObject.layer;
                part.transform.SetParent(root.transform, false);
                part.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                part.transform.localScale = renderer.transform.lossyScale;
                part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                part.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }

            if (root == null) return;

            // Recenter the original mesh and lift it clear of the floor as it breaks free.
            foreach (Transform part in root.transform)
                part.localPosition -= bounds.center;
            float lift = Mathf.Max(0.06f, bounds.extents.y * 0.3f);
            root.transform.position = bounds.center + Vector3.up * lift;
            root.layer = deposit.gameObject.layer;

            var chunk = root.AddComponent<OreChunk>();
            chunk.visuals = root.GetComponentsInChildren<MeshRenderer>();
            chunk.ConfigureHeldVisualPose(deposit);
            chunk.hitbox = root.GetComponent<BoxCollider>();
            chunk.hitbox.center = Vector3.zero;
            chunk.hitbox.size = Vector3.Max(bounds.size, Vector3.one * 0.04f);
            chunk.body = root.GetComponent<Rigidbody>();
            chunk.body.mass = 25f;
            chunk.body.interpolation = RigidbodyInterpolation.Interpolate;
            chunk.body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        internal bool TryPickUp()
        {
            if (!isActiveAndEnabled || IsHeld) return false;

            pickupPosition = transform.position;
            pickupRotation = transform.rotation;
            IsHeld = true;
            wasThrown = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            hitbox.enabled = false;
            SetCarryAppearance(true);
            return true;
        }

        internal void SetOutline(Material material)
        {
            if (material != null && outlineRenderers == null)
            {
                outlineRenderers = new MeshRenderer[visuals.Length];
                for (int i = 0; i < visuals.Length; i++)
                {
                    var visual = visuals[i];
                    var mesh = visual.GetComponent<MeshFilter>().sharedMesh;
                    var outline = new GameObject("Erzkontur", typeof(MeshFilter), typeof(MeshRenderer));
                    outline.layer = visual.gameObject.layer;
                    outline.transform.SetParent(visual.transform, false);
                    outline.GetComponent<MeshFilter>().sharedMesh = mesh;

                    var renderer = outline.GetComponent<MeshRenderer>();
                    var materials = new Material[mesh.subMeshCount];
                    Array.Fill(materials, material);
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    outlineRenderers[i] = renderer;
                }
            }

            if (outlineRenderers == null) return;
            foreach (var renderer in outlineRenderers)
                renderer.enabled = material != null;
        }

        internal void SetCarryPose(Transform grip)
        {
            transform.SetPositionAndRotation(grip.position, grip.rotation);
        }

        internal void SetHeldModelVisible(bool visible)
        {
            if (visible)
            {
                if (hiddenRenderStates == null) return;
                for (int i = 0; i < visuals.Length; i++)
                    visuals[i].forceRenderingOff = hiddenRenderStates[i];
                hiddenRenderStates = null;
                return;
            }

            if (hiddenRenderStates != null) return;
            hiddenRenderStates = new bool[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                hiddenRenderStates[i] = visuals[i].forceRenderingOff;
                visuals[i].forceRenderingOff = true;
            }
        }

        internal void SetViewLayer(int layer)
        {
            foreach (var renderer in visuals)
                renderer.gameObject.layer = layer;
        }

        internal bool TryThrow(Vector3 origin, Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            // Move the held visual adjustment onto the body before restoring the mesh's local pose.
            position += rotation * heldVisualAdjustment.position;
            rotation *= heldVisualAdjustment.rotation;
            return TryReleaseThrow(origin, position, rotation, velocity, rotation * new Vector3(0.8f, 0.3f, 0.45f));
        }

        internal bool TryThrowInto(Vector3 origin, Vector3 intakePosition)
        {
            Vector3 toIntake = Vector3.ProjectOnPlane(intakePosition - origin, Vector3.up);
            float distance = toIntake.magnitude;
            float gravity = -Physics.gravity.y;
            if (distance < 0.1f || gravity <= 0f) return false;

            Vector3 direction = toIntake / distance;
            Quaternion rotation = Quaternion.LookRotation(direction) * heldVisualAdjustment.rotation;
            Vector3 halfSize = Size * 0.5f;
            float halfHeight = Mathf.Abs((rotation * Vector3.right).y) * halfSize.x +
                Mathf.Abs((rotation * Vector3.up).y) * halfSize.y +
                Mathf.Abs((rotation * Vector3.forward).y) * halfSize.z;
            Vector3 landing = intakePosition + Vector3.up * (halfHeight + 0.13f);

            // Lift short tosses clear of the case and let gravity take the chunk into the hopper.
            Vector3 start = origin + direction * Mathf.Min(1.1f, distance * 0.55f);
            start.y = Mathf.Max(origin.y + 0.25f, landing.y - Mathf.Max(0f, distance - 1.7f) * 0.55f);
            if (TryLob(origin, start, rotation, landing, gravity)) return true;

            // At close range, the initial pose or arc may hit the case or the player.
            float raisedStart = Mathf.Max(origin.y + 0.46f, intakePosition.y + 0.16f) + halfHeight;
            if (raisedStart <= start.y) return false;
            start.y = raisedStart;
            return TryLob(origin, start, rotation, landing, gravity);
        }

        private bool TryLob(Vector3 origin, Vector3 start, Quaternion rotation, Vector3 landing, float gravity)
        {
            float apex = Mathf.Max(start.y, landing.y) + 0.26f;
            float travelTime = Mathf.Sqrt(2f * (apex - start.y) / gravity) +
                Mathf.Sqrt(2f * (apex - landing.y) / gravity);
            Vector3 acceleration = Physics.gravity;
            Vector3 velocity = (landing - start) / travelTime - acceleration * (travelTime * 0.5f);

            Vector3 previous = start;
            const int segments = 8;
            for (int i = 1; i <= segments; i++)
            {
                float t = travelTime * i / segments;
                Vector3 next = start + velocity * t + acceleration * (t * t * 0.5f);
                Vector3 delta = next - previous;
                if (Physics.BoxCast(previous, Size * 0.5f, delta.normalized, rotation, delta.magnitude,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;

                previous = next;
            }

            return TryReleaseThrow(origin, start, rotation, velocity, Vector3.zero);
        }

        private bool TryReleaseThrow(Vector3 origin, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 spin)
        {
            Vector3 toChunk = position - origin;
            if (Physics.Raycast(origin, toChunk.normalized, toChunk.magnitude, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) ||
                Physics.CheckBox(position, hitbox.size * 0.5f, rotation,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            Release(position, rotation, velocity, spin);
            wasThrown = true;
            return true;
        }

        internal bool TryConsumeThrown()
        {
            if (!isActiveAndEnabled || IsHeld || !wasThrown) return false;

            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }

        internal void ReturnToPickupSpot()
        {
            Release(pickupPosition, pickupRotation, Vector3.zero, Vector3.zero);
        }

        private void Release(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 spin)
        {
            SetHeldModelVisible(true);
            IsHeld = false;
            body.position = position;
            body.rotation = rotation;
            hitbox.enabled = true;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = velocity;
            body.angularVelocity = spin;
            SetViewLayer(gameObject.layer);
            SetCarryAppearance(false);
        }

        private void ConfigureHeldVisualPose(OreDeposit deposit)
        {
            Transform anchor = visuals[0].transform;
            if (!deposit.TryGetHeldMeshPose(anchor.localPosition, out Pose heldPose)) return;

            // Apply the anchor's change to every part so the model stays together.
            Quaternion rotation = heldPose.rotation * Quaternion.Inverse(anchor.localRotation);
            heldVisualAdjustment = new Pose(heldPose.position - rotation * anchor.localPosition, rotation);
            restVisualPoses = new Pose[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                Transform part = visuals[i].transform;
                restVisualPoses[i] = new Pose(part.localPosition, part.localRotation);
            }
        }

        private void SetCarryAppearance(bool carrying)
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                var renderer = visuals[i];
                renderer.shadowCastingMode = carrying ? ShadowCastingMode.Off : ShadowCastingMode.On;
                if (restVisualPoses == null) continue;

                Pose pose = restVisualPoses[i];
                if (carrying)
                    pose = pose.GetTransformedBy(heldVisualAdjustment);
                renderer.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
            }
        }
    }
}
