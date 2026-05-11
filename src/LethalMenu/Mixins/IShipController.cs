using UnityEngine;

namespace LethalMenu.Mixins
{
    public interface IShipController { }

    public static class ShipControllerMixin
    {
        public static void ToggleShipLights(this IShipController _)
        {
            var lights = Object.FindObjectOfType<ShipLights>();
            if (lights != null)
                lights.ToggleShipLights();
        }

        public static void ForceShipLeave(this IShipController _)
        {
            var instance = StartOfRound.Instance;
            if (instance != null)
                instance.EndGameServerRpc((int)(instance.localPlayerController?.playerClientId ?? 0));
        }

        public static void ForceStart(this IShipController _)
        {
            var instance = StartOfRound.Instance;
            if (instance != null)
                instance.StartGameServerRpc();
        }
    }
}
