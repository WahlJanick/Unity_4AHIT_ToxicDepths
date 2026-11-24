using UnityEngine;
using UnityEngine.Events;

namespace ToxicDepths.Mining
{
    [AddComponentMenu("Toxic Depths/Mining/Ore Deposit")]
    [DisallowMultipleComponent]
    public sealed class OreDeposit : MonoBehaviour
    {
        [SerializeField] private string oreName = "Smaragd";
        [SerializeField] private Sprite hotbarIcon;
        [SerializeField, Min(1)] private int hitsToBreak = 4;
        [SerializeField] private UnityEvent onDepleted = new UnityEvent();

        [Header("Giftwolke")]
        [SerializeField] private ParticleSystem poisonSmoke;
        [SerializeField] private Material poisonSmokeMaterial;

        [Header("Verarbeitung")]
        [Tooltip("Dauer pro Erzstueck im Standard-Shredder. Schnellere Modelle reduzieren diese Zeit.")]
        [SerializeField, Min(0.1f)] private float shredderSeconds = 6f;

        [Header("Trageansicht")]
        [SerializeField] private bool overrideHeldMeshPose;
        [SerializeField] private float heldMeshHeight;
        [SerializeField] private Vector3 heldMeshEuler;

        private int hitsTaken;
        private float feedbackTime;
        private Quaternion restRotation;

        public string OreName => oreName;
        public Sprite HotbarIcon => hotbarIcon;
        public float ShredderSeconds => Mathf.Max(0.1f, shredderSeconds);
        public int RemainingHits => Mathf.Max(0, hitsToBreak - hitsTaken);
        public bool IsDepleted => RemainingHits == 0;
        internal bool ReleasesPoison => poisonSmoke != null;
        internal bool WasScanned { get; private set; }
        internal void MarkScanned() => WasScanned = true;
        internal string ScanName => oreName switch { "Smaragd" => "Emerald", "Rubin" => "Ruby", _ => oreName };
        internal static event UnityAction<OreDeposit> Available;

        private void OnEnable() => Available?.Invoke(this);

        internal bool TryGetHeldMeshPose(Vector3 meshPosition, out Pose pose)
        {
            pose = default;
            if (!overrideHeldMeshPose) return false;

            meshPosition.y = heldMeshHeight;
            pose = new Pose(meshPosition, Quaternion.Euler(heldMeshEuler));
            return true;
        }

        private void Awake()
        {
            restRotation = transform.localRotation;
        }

        // True only on the hit that depletes this node, so the chunk only drops once.
        public bool ApplyHit(int damage)
        {
            if (!isActiveAndEnabled || IsDepleted || damage <= 0) return false;
            hitsTaken += Mathf.Min(RemainingHits, damage);
            if (!IsDepleted)
            {
                feedbackTime = 0.18f;
                return false;
            }

            transform.localRotation = restRotation;
            OreChunk.Spawn(this);
            if (poisonSmoke != null)
            {
                Vector3 center = TryGetComponent(out Collider oreCollider) ? oreCollider.bounds.center : transform.position;
                PoisonCloud.Spawn(poisonSmoke, poisonSmokeMaterial, center + Vector3.up * 0.55f, gameObject.scene);
            }
            gameObject.SetActive(false);
            onDepleted.Invoke();
            return true;
        }

        private void Update()
        {
            if (feedbackTime <= 0f) return;
            feedbackTime = Mathf.Max(0f, feedbackTime - Time.deltaTime);
            float strength = feedbackTime / 0.18f;
            float shake = Mathf.Sin((1f - strength) * Mathf.PI * 6f) * strength;
            transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, shake * 1.5f);
        }

        private void OnDisable()
        {
            if (feedbackTime > 0f) transform.localRotation = restRotation;
            feedbackTime = 0f;
        }
    }
}
