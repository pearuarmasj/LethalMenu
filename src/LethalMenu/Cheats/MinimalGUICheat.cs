namespace LethalMenu.Cheats
{
    /// MinimalGUI — hide the player HUD for cinematic shots. HideHUD is idempotent, so re-asserting it
    /// each frame keeps the HUD hidden across game-driven reveals and a new HUDManager after scene load.
    public class MinimalGUICheat : CheatBase
    {
        public override string Name => "Minimal GUI";
        public override Hack HackType => Hack.MinimalGUIMod;

        public override void OnUpdate()
        {
            if (IsEnabled) HUDManager.Instance?.HideHUD(true);
        }

        public override void OnDisable() => HUDManager.Instance?.HideHUD(false);
    }
}
