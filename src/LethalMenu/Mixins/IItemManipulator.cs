using UnityEngine;

namespace LethalMenu.Mixins
{
    public interface IItemManipulator { }

    public static class ItemManipulatorMixin
    {
        public static void TeleportAllItemsToShip(this IItemManipulator _)
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null) return;
            var shipPos = terminal.transform.position;

            foreach (var item in LethalMenuMod.Items)
            {
                if (item == null || item.isHeld || item.isInShipRoom) continue;
                item.transform.position = shipPos + Vector3.up * 0.5f;
                item.startFallingPosition = item.transform.position;
                item.FallToGround();
            }
        }
    }
}
