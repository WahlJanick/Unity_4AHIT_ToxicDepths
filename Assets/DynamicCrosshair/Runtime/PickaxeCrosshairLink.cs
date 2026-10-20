using UnityEngine;

namespace ToxicDepths.UI
{
    [AddComponentMenu("Toxic Depths/UI/Pickaxe Crosshair Link")]
    [DisallowMultipleComponent]
    public sealed class PickaxeCrosshairLink : MonoBehaviour
    {
        [Tooltip("Das Crosshair aus der Spielszene hier zuweisen. Auf dasselbe Objekt wie den Animator setzen.")]
        [SerializeField] private DynamicCrosshair crosshair;

        private int activeState;
        private int activeLayer;
        private bool animationOwnsCrosshair;

        // Alternative for Animation Events or a mining controller with a fixed duration.
        public void PlaySwing()
        {
            animationOwnsCrosshair = false;
            if (crosshair != null) crosshair.PlaySwing();
        }

        public void CancelSwing()
        {
            animationOwnsCrosshair = false;
            if (crosshair != null) crosshair.ResetCrosshair();
        }

        internal void BeginAnimatedSwing(int state, int layer)
        {
            activeState = state;
            activeLayer = layer;
            animationOwnsCrosshair = true;
            if (crosshair != null) crosshair.SetSwingProgress(0f);
        }

        internal void UpdateAnimatedSwing(int state, int layer, float progress)
        {
            if (OwnsSwing(state, layer) && crosshair != null) crosshair.SetSwingProgress(progress);
        }

        internal void EndAnimatedSwing(int state, int layer)
        {
            // An outgoing state must not reset a new swing during a crossfade.
            if (OwnsSwing(state, layer)) CancelSwing();
        }

        private bool OwnsSwing(int state, int layer)
        {
            return animationOwnsCrosshair && activeState == state && activeLayer == layer;
        }

        private void OnDisable()
        {
            CancelSwing();
        }
    }
}
