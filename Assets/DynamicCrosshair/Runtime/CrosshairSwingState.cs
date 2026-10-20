using UnityEngine;

namespace ToxicDepths.UI
{
    /// <summary>Add this behaviour to the pickaxe swing state in the Animator.</summary>
    public sealed class CrosshairSwingState : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent(out PickaxeCrosshairLink link))
            {
                link.BeginAnimatedSwing(stateInfo.fullPathHash, layerIndex);
            }
            else
            {
                Debug.LogWarning("CrosshairSwingState: PickaxeCrosshairLink fehlt am Animator-Objekt.", animator);
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!animator.TryGetComponent(out PickaxeCrosshairLink link)) return;

            float progress = stateInfo.loop
                ? Mathf.Repeat(stateInfo.normalizedTime, 1f)
                : Mathf.Clamp01(stateInfo.normalizedTime);
            link.UpdateAnimatedSwing(stateInfo.fullPathHash, layerIndex, progress);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent(out PickaxeCrosshairLink link))
                link.EndAnimatedSwing(stateInfo.fullPathHash, layerIndex);
        }
    }
}
