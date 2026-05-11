using UnityEngine;

namespace LethalMenu.Mixins
{
    public interface ITeleporter { }

    public static class TeleporterMixin
    {
        public static void TeleportTo(this ITeleporter _, Vector3 position)
        {
            LethalMenuMod.LocalPlayer?.TeleportPlayer(position);
        }

        public static void TeleportToShip(this ITeleporter _)
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal != null)
                _.TeleportTo(terminal.transform.position);
        }

        public static void TeleportToEntrance(this ITeleporter _, bool mainEntrance)
        {
            foreach (var entrance in LethalMenuMod.Entrances)
            {
                if (entrance == null) continue;
                bool isMain = entrance.entranceId == 0;
                if (mainEntrance == isMain)
                {
                    var pos = entrance.entrancePoint?.position ?? entrance.transform.position;
                    _.TeleportTo(pos);
                    return;
                }
            }
        }

        public static void TeleportPlayerTo(this ITeleporter _, GameNetcodeStuff.PlayerControllerB player, Vector3 position)
        {
            player?.TeleportPlayer(position);
        }
    }
}
