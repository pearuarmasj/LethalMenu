namespace LethalMenu.Cheats
{
    /// MinimalGUI — hide the player HUD for cinematic shots.
    /// Detects toggle transition in OnUpdate (OnEnable/OnDisable are only fired on cleanup).
    public class MinimalGUICheat : CheatBase
    {
        public override string Name => "Minimal GUI";
        public override Hack HackType => Hack.MinimalGUIMod;

        private bool _lastEnabled;

        public override void OnUpdate()
        {
            if (HUDManager.Instance == null) return;

            bool now = IsEnabled;
            if (now == _lastEnabled) return;
            _lastEnabled = now;

            HUDManager.Instance.HideHUD(now);
        }
    }
}
