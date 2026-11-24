using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToxicDepths.Mining
{
    [DisallowMultipleComponent]
    public sealed class PoisonCloud : MonoBehaviour
    {
        private const float Lifetime = 4f;
        private const float Radius = 3f;
        private const float PoisonDuration = 1.5f;
        private const float ParticleLifetime = 1.35f;

        internal static void Spawn(ParticleSystem prefab, Material material, Vector3 center, Scene scene)
        {
            var smoke = Instantiate(prefab, center, Quaternion.identity);
            var cloud = smoke.gameObject;
            cloud.name = "Poison Cloud";
            SceneManager.MoveGameObjectToScene(cloud, scene);

            if (material != null)
                smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

            smoke.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = smoke.main;
            main.duration = Lifetime - ParticleLifetime;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.95f, ParticleLifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(1.45f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);

            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Radius - 0.8f;
            shape.radiusThickness = 1f;
            shape.scale = new Vector3(1f, 0.55f, 1f);

            var emission = smoke.emission;
            emission.rateOverTime = 28f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
            smoke.Play(false);

            var body = cloud.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var area = cloud.AddComponent<SphereCollider>();
            area.radius = Radius;
            area.isTrigger = true;
            cloud.AddComponent<PoisonCloud>();
            Destroy(cloud, Lifetime);
        }

        private void OnTriggerEnter(Collider other) => ApplyPoison(other);
        private void OnTriggerStay(Collider other) => ApplyPoison(other);

        private static void ApplyPoison(Collider other)
        {
            var player = other.GetComponentInParent<PlayerVitals>();
            if (player != null) player.RefreshPoison(PoisonDuration);
        }
    }
}
