using ToxicDepths.Mining;
using UnityEngine;

namespace ToxicDepths
{
    [DisallowMultipleComponent]
    public sealed class PickaxePickup : MonoBehaviour
    {
        internal bool CanCollect(PlayerMining player) => isActiveAndEnabled && player != null &&
            !player.HasPickaxe && !player.IsCarryingOre;

        internal bool TryCollect(PlayerMining player)
        {
            if (!CanCollect(player) || !player.GivePickaxe()) return false;
            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }
    }
}
