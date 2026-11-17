using System.Collections.Generic;
using ToxicDepths.Mining;
using UnityEngine;

namespace ToxicDepths
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class Shredder : MonoBehaviour
    {
        private const float StandardSeconds = 4f;

        internal static readonly IReadOnlyList<(string Name, float Seconds, int Price)> Models =
            new (string, float, int)[] { ("Standard", StandardSeconds, 0), ("Schnell", 1.8f, 2), ("Turbo", 0.9f, 4) };

        [SerializeField] private Transform outlet;
        [SerializeField] private Transform animatedModel;
        [SerializeField] private Rigidbody emeraldPrefab;
        [SerializeField] private Rigidbody stonePrefab;
        [SerializeField, Min(0f)] private float processingTime = StandardSeconds;

        private BoxCollider intake;
        private Pose restPose;
        private int pendingBatches;
        private float readyAt;
        private int emeraldsToEject;
        private int stonesToEject;
        private float nextEjectionAt;

        internal int ModelIndex { get; private set; }
        internal bool CanAcceptOre => isActiveAndEnabled && outlet != null && emeraldPrefab != null && stonePrefab != null;
        internal bool IsProcessing => pendingBatches > 0;
        internal float SecondsRemaining => IsProcessing ? Mathf.Max(0f, readyAt - Time.time) : 0f;
        internal float Progress => IsProcessing && processingTime > 0f
            ? Mathf.Clamp01(1f - SecondsRemaining / processingTime)
            : 0f;

        internal Rigidbody GetOutputPrefab(ShredderOutputPickup.Kind kind) =>
            kind == ShredderOutputPickup.Kind.Emerald ? emeraldPrefab : stonePrefab;

        internal void ConfigureModel(int index)
        {
            ModelIndex = index;
            processingTime = Models[index].Seconds;
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

            // The chunk can overhang the rim, but its center must be above the opening.
            Vector3 center = transform.InverseTransformPoint(body.worldCenterOfMass);
            if (center.y < -0.04f ||
                Mathf.Abs(center.x - intake.center.x) > intake.size.x * 0.5f ||
                Mathf.Abs(center.z - intake.center.z) > intake.size.z * 0.5f ||
                !ore.TryConsumeThrown()) return;

            if (pendingBatches == 0) readyAt = Time.time + processingTime;
            pendingBatches++;
        }

        private void Update()
        {
            if (pendingBatches > 0)
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
                    pendingBatches--;
                    readyAt = Time.time + processingTime;
                }
            }

            if (emeraldsToEject + stonesToEject > 0 && Time.time >= nextEjectionAt)
                EjectNextPiece();
        }

        private void ShakeModel(float remaining)
        {
            if (animatedModel == null) return;

            float elapsed = processingTime - remaining;
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

        private void OnDisable() => RestoreModel();

        private void EjectNextPiece()
        {
            bool emerald = Random.Range(0, emeraldsToEject + stonesToEject) < emeraldsToEject;
            if (emerald) emeraldsToEject--;
            else stonesToEject--;

            Rigidbody prefab = emerald ? emeraldPrefab : stonePrefab;
            Vector3 position = outlet.TransformPoint(new Vector3(Random.Range(-0.07f, 0.07f), 0f, 0.035f));
            Quaternion rotation = outlet.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var piece = Instantiate(prefab, position, rotation);
            piece.linearVelocity = outlet.forward * Random.Range(2.35f, 2.85f) +
                outlet.up * Random.Range(0.5f, 0.85f) + outlet.right * Random.Range(-0.7f, 0.7f);
            piece.angularVelocity = outlet.right * Random.Range(2.5f, 4.5f) +
                outlet.forward * Random.Range(-2f, 2f);
            nextEjectionAt = Time.time + Random.Range(0.15f, 0.21f);
        }
    }
}