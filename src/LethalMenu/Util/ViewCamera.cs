using LethalMenu.Cheats;
using UnityEngine;

namespace LethalMenu.Util
{
    /// The camera currently presenting the world to the local user, for world-to-screen overlays.
    public static class ViewCamera
    {
        public static Camera? Current
        {
            get
            {
                if (FreeCamCheat.ActiveCamera != null) return FreeCamCheat.ActiveCamera;
                if (SpectatePlayerCheat.ActiveCamera != null) return SpectatePlayerCheat.ActiveCamera;
                if (ThirdPersonCheat.ActiveCamera != null) return ThirdPersonCheat.ActiveCamera;

                var player = LethalMenuMod.LocalPlayer;
                if (player == null) return Camera.main;
                if (player.isPlayerDead && StartOfRound.Instance != null) return StartOfRound.Instance.spectateCamera;
                return player.gameplayCamera != null ? player.gameplayCamera : Camera.main;
            }
        }
    }
}
