using UnityEngine;

namespace LethalMenu.Mixins
{
    public interface IItemManipulator { }

    public static class ItemManipulatorMixin
    {
        public static void TeleportAllItemsToShip(this IItemManipulator _)
        {
            var startOfRound = StartOfRound.Instance ?? LethalMenuMod.GameInstance;
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (startOfRound == null || localPlayer == null) return;

            var elevatorTransform = startOfRound.elevatorTransform;
            var shipPos = GetShipDropPosition(startOfRound);
            var allItems = Object.FindObjectsOfType<GrabbableObject>();
            var teleported = 0;
            var totalValue = 0;

            foreach (var item in allItems)
            {
                if (item == null || item.isHeld || item.isHeldByEnemy || item.isPocketed || item.isInShipRoom)
                    continue;

                if (item.itemProperties == null || (!item.itemProperties.isScrap && item.scrapValue <= 0))
                    continue;

                var targetPos = shipPos + GetStackOffset(teleported);
                if (elevatorTransform != null)
                    item.transform.SetParent(elevatorTransform, true);

                item.transform.position = targetPos;
                item.hasHitGround = false;
                item.reachedFloorTarget = false;
                item.fallTime = 0f;
                item.startFallingPosition = item.transform.localPosition;
                item.targetFloorPosition = item.transform.localPosition;

                localPlayer.SetItemInElevator(true, true, item);
                if (item.itemProperties.isScrap && !item.scrapPersistedThroughRounds)
                    RoundManager.Instance?.CollectNewScrapForThisRound(item);

                item.FallToGround(false);

                teleported++;
                totalValue += item.scrapValue;
            }

            Loader.Log($"Teleported {teleported} loot items to ship (value: ${totalValue})");
        }

        private static Vector3 GetShipDropPosition(StartOfRound startOfRound)
        {
            if (startOfRound.middleOfShipNode != null)
                return startOfRound.middleOfShipNode.position + Vector3.up * 1.5f;

            if (startOfRound.insideShipPositions is { Length: > 0 } && startOfRound.insideShipPositions[0] != null)
                return startOfRound.insideShipPositions[0].position + Vector3.up * 1.5f;

            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal != null)
                return terminal.transform.position + Vector3.up * 0.5f;

            return startOfRound.elevatorTransform != null
                ? startOfRound.elevatorTransform.position + Vector3.up * 1.5f
                : Vector3.up * 1.5f;
        }

        private static Vector3 GetStackOffset(int index)
        {
            var x = (index % 5 - 2) * 0.35f;
            var z = (index / 5 % 5 - 2) * 0.35f;
            var y = index / 25 * 0.2f;
            return new Vector3(x, y, z);
        }
    }
}
