using UnityEngine;

namespace LethalMenu.Cheats
{
    /// Screen-center crosshair.
    public class CrosshairCheat : CheatBase
    {
        public override string Name => "Crosshair";
        public override Hack HackType => Hack.Crosshair;

        public override void OnGUI()
        {
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            float scale = Settings.CrosshairScale;
            float thickness = Settings.CrosshairThickness;

            GUI.color = Settings.CrosshairColor;
            GUI.DrawTexture(new Rect(centerX - scale, centerY - thickness / 2, scale * 2, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - thickness / 2, centerY - scale, thickness, scale * 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    /// Numeric HP readout, colored by health band.
    public class HPDisplayCheat : CheatBase
    {
        public override string Name => "HP Display";
        public override Hack HackType => Hack.HPDisplay;

        private GUIStyle? _style;

        public override void OnGUI()
        {
            var player = LethalMenuMod.LocalPlayer;
            if (player == null) return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            GUI.color = player.health < 25 ? Color.red : player.health < 50 ? Color.yellow : Color.green;
            GUI.Label(new Rect(20, 80, 100, 40), $"HP: {player.health}", _style);
            GUI.color = Color.white;
        }
    }

    /// Hides the helmet visor model. Third Person also hides it while active and defers to this
    /// flag when it hands the view back.
    public class NoVisorCheat : CheatBase
    {
        public override string Name => "No Visor";
        public override Hack HackType => Hack.NoVisor;

        private const string VisorPath = "Systems/Rendering/PlayerHUDHelmetModel/";
        private GameObject? _visor;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            if (_visor == null) _visor = GameObject.Find(VisorPath);
            if (_visor != null && _visor.activeSelf) _visor.SetActive(false);
        }

        public override void OnDisable()
        {
            if (_visor == null) _visor = GameObject.Find(VisorPath);
            if (_visor != null && !ThirdPersonCheat.ViewState) _visor.SetActive(true);
        }
    }
}
