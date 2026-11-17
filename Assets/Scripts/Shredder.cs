using System.Collections.Generic;
using ToxicDepths.Mining;
using UnityEngine;

namespace ToxicDepths
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class Shredder : MonoBehaviour
    {
        internal static readonly IReadOnlyList<(string Name, float DurationMultiplier, int Price)> Models =
            new (string, float, int)[] { ("Standard", 1f, 0), ("Schnell", 0.45f, 2), ("Turbo", 0.225f, 4) };

        [SerializeField] private Transform outlet;
        [SerializeField] private Transform animatedModel;
        [SerializeField] private Rigidbody emeraldPrefab;
        [SerializeField] private Rigidbody stonePrefab;

        [Header("Abbauen")]
        [SerializeField, Min(1)] private int hitsToDismantle = 3;
        [SerializeField] private bool isPacked;

        private BoxCollider intake;
        private Pose restPose;
        private readonly Queue<float> pendingBatches = new Queue<float>();
        private float currentCycleSeconds;
        private float readyAt;
        private int emeraldsToEject;
        private int stonesToEject;
        private float nextEjectionAt;
        private int dismantleHits;
        private float hitFeedbackTime;
        private MeshRenderer[] outlineRenderers;

        internal int ModelIndex { get; private set; }
        internal GameObject Root => outlet != null && outlet.parent != null ? outlet.parent.gameObject : gameObject;
        private bool IsIdle => !IsProcessing && emeraldsToEject + stonesToEject == 0;
        internal bool CanPickUp => isPacked && isActiveAndEnabled && IsIdle;
        internal bool CanAcceptOre => !isPacked && isActiveAndEnabled && outlet != null && emeraldPrefab != null && stonePrefab != null &&
            (!Root.TryGetComponent(out Rigidbody body) || body.isKinematic);
        internal bool IsProcessing => pendingBatches.Count > 0;
        internal float SecondsRemaining => IsProcessing ? Mathf.Max(0f, readyAt - Time.time) : 0f;
        internal float Progress => IsProcessing && currentCycleSeconds > 0f
            ? Mathf.Clamp01(1f - SecondsRemaining / currentCycleSeconds)
            : 0f;

        internal Rigidbody GetOutputPrefab(ShredderOutputPickup.Kind kind) =>
            kind == ShredderOutputPickup.Kind.Emerald ? emeraldPrefab : stonePrefab;

        internal static Shredder Find(Collider collider)
        {
            if (collider == null) return null;
            var machine = collider.GetComponentInParent<Shredder>();
            if (machine == null && collider.attachedRigidbody != null)
                machine = collider.attachedRigidbody.GetComponentInChildren<Shredder>();
            return machine != null ? machine : collider.GetComponentInChildren<Shredder>();
        }

        internal void ConfigureModel(int index)
        {
            ModelIndex = index;
        }

        internal void SetOutline(Material material)
        {
            bool visible = material != null && CanPickUp;
            if (visible && outlineRenderers == null)
            {
                var created = new List<MeshRenderer>();
                foreach (var visual in Root.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!visual.enabled || !visual.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                        continue;
                    var mesh = filter.sharedMesh;
                    var outline = new GameObject("Shredderkontur", typeof(MeshFilter), typeof(MeshRenderer));
                    outline.layer = visual.gameObject.layer;
                    outline.transform.SetParent(visual.transform, false);
                    outline.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = outline.GetComponent<MeshRenderer>();
                    var materials = new Material[mesh.subMeshCount];
                    System.Array.Fill(materials, material);
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    created.Add(renderer);
                }
                outlineRenderers = created.ToArray();
            }

            if (outlineRenderers == null) return;
            foreach (var renderer in outlineRenderers)
                if (renderer != null) renderer.enabled = visible;
        }

        internal bool ApplyPickaxeHit(int damage)
        {
            if (!isActiveAndEnabled || isPacked || !IsIdle || damage <= 0) return false;
            int remaining = Mathf.Max(1, hitsToDismantle) - dismantleHits;
            dismantleHits += Mathf.Min(damage, remaining);
            hitFeedbackTime = 0.18f;
            return dismantleHits >= Mathf.Max(1, hitsToDismantle) && PackForPickup();
        }

        internal bool PackForPickup()
        {
            if (!isActiveAndEnabled || isPacked || !IsIdle) return false;
            hitFeedbackTime = 0f;
            RestoreModel();
            var root = Root;
            var box = root.GetComponent<BoxCollider>();
            if (box == null) return false;

            isPacked = true;
            if (intake != null) intake.enabled = false;
            var body = root.GetComponent<Rigidbody>();
            if (body == null) body = root.AddComponent<Rigidbody>();
            body.mass = 25f;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return true;
        }

        private void Awake()
        {
            intake = GetComponent<BoxCollider>();
            if (animatedModel != null)
                restPose = new Pose(animatedModel.localPosition, animatedModel.localRotation);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!CanAcceptOre) return;
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.linearVelocity.y > 0.15f ||
                !body.TryGetComponent(out OreChunk ore)) return;

            float standardSeconds = ore.ShredderSeconds;

            // The chunk can overhang the rim, but its center must be above the opening.
            Vector3 center = transform.InverseTransformPoint(body.worldCenterOfMass);
            if (center.y < -0.04f ||
                Mathf.Abs(center.x - intake.center.x) > intake.size.x * 0.5f ||
                Mathf.Abs(center.z - intake.center.z) > intake.size.z * 0.5f ||
                !ore.TryConsumeThrown()) return;

            EnqueueOre(standardSeconds);
        }

        private void EnqueueOre(float standardSeconds)
        {
            float duration = Mathf.Max(0.1f, standardSeconds) * Models[ModelIndex].DurationMultiplier;
            pendingBatches.Enqueue(duration);
            if (pendingBatches.Count == 1) StartNextCycle();
        }

        private void StartNextCycle()
        {
            hitFeedbackTime = 0f;
            currentCycleSeconds = pendingBatches.Peek();
            readyAt = Time.time + currentCycleSeconds;
        }

        private void Update()
        {
            if (IsProcessing)
            {
                float remaining = readyAt - Time.time;
                if (remaining > 0f)
                {
                    ShakeModel(remaining);
                }
                else if (emeraldsToEject + stonesToEject == 0)
                {
                    RestoreModel();
                    emeraldsToEject = Random.Range(1, 3);
                    stonesToEject = Random.Range(1, 4);
                    pendingBatches.Dequeue();
                    if (IsProcessing) StartNextCycle();
                }
            }
            else UpdateHitFeedback(Time.deltaTime);

            if (emeraldsToEject + stonesToEject > 0 && Time.time >= nextEjectionAt)
                EjectNextPiece();
        }

        private void UpdateHitFeedback(float deltaTime)
        {
            if (hitFeedbackTime <= 0f) return;
            hitFeedbackTime = Mathf.Max(0f, hitFeedbackTime - deltaTime);
            if (animatedModel == null) return;
            float strength = hitFeedbackTime / 0.18f;
            float shake = Mathf.Sin((1f - strength) * Mathf.PI * 6f) * strength;
            animatedModel.localRotation = restPose.rotation * Quaternion.Euler(0f, 0f, shake * 1.5f);
        }

        private void ShakeModel(float remaining)
        {
            if (animatedModel == null) return;

            float elapsed = currentCycleSeconds - remaining;
            float strength = Mathf.Clamp01(Mathf.Min(elapsed, remaining) * 16f);
            float phase = elapsed * 11f * (2f * Mathf.PI);
            float sway = Mathf.Sin(phase);
            Vector3 offset = new Vector3(sway * 0.03f, (1f - Mathf.Cos(phase)) * 0.006f,
                Mathf.Sin(phase * 1.3f) * 0.01f);
            Quaternion tilt = Quaternion.Euler(0f, 0f, -sway * 1.5f * strength);
            animatedModel.SetLocalPositionAndRotation(restPose.position + offset * strength, tilt * restPose.rotation);
        }

        private void RestoreModel()
        {
            if (animatedModel != null)
                animatedModel.SetLocalPositionAndRotation(restPose.position, restPose.rotation);
        }

        private void OnDisable()
        {
            hitFeedbackTime = 0f;
            SetOutline(null);
            RestoreModel();
        }

        private void EjectNextPiece()
        {
            bool emerald = Random.Range(0, emeraldsToEject + stonesToEject) < emeraldsToEject;
            if (emerald) emeraldsToEject--;
            else stonesToEject--;

            Rigidbody prefab = emerald ? emeraldPrefab : stonePrefab;
            Vector3 position = outlet.TransformPoint(new Vector3(Random.Range(-0.07f, 0.07f), 0f, 0.035f));
            Quaternion rotation = outlet.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var piece = Instantiate(prefab, position, rotation);
            Vector3 velocity = outlet.forward * Random.Range(2.35f, 2.85f) +
                outlet.up * Random.Range(0.5f, 0.85f) + outlet.right * Random.Range(-0.7f, 0.7f);
            if (!emerald)
            {
                // A clear upward kick in world Y, even when the outlet points down a slope.
                velocity.y = Random.Range(2.2f, 3f);
                piece.linearDamping = 0f;
            }
            piece.linearVelocity = velocity;
            piece.angularVelocity = outlet.right * Random.Range(2.5f, 4.5f) +
                outlet.forward * Random.Range(-2f, 2f);
            nextEjectionAt = Time.time + Random.Range(0.15f, 0.21f);
        }
    }
}
