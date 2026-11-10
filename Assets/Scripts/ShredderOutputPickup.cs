using UnityEngine;

namespace ToxicDepths
{
    [DisallowMultipleComponent]
    public sealed class ShredderOutputPickup : MonoBehaviour
    {
        public enum Kind { Emerald, Stone }

        [SerializeField] private Kind itemKind;
        internal Kind ItemKind => itemKind;

        internal bool TryCollect()
        {
            if (!isActiveAndEnabled) return false;

            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }
    }
}
