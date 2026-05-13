using UnityEngine;

namespace LethalMenu.Cheats
{
    /// PJSpammer — sends fake "X joined the game" chat messages at a configurable rate.
    public class PJSpammerCheat : CheatBase
    {
        public override string Name => "PJ Spammer";
        public override Hack HackType => Hack.PJSpammer;

        private static readonly string[] FakeNames =
        {
            "Player_42", "xX_Sigma_Xx", "BigMoney", "ShipSlayer", "NotABot",
            "QuotaQueen", "BraceFor7", "ScrapGremlin", "MoonShine", "FireExit"
        };

        private float _accumulator;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            if (HUDManager.Instance == null) return;

            float rate = Settings.PJSpammerRate;
            if (rate <= 0f) return;

            _accumulator += Time.deltaTime * rate;
            while (_accumulator >= 1f)
            {
                _accumulator -= 1f;
                var name = FakeNames[Random.Range(0, FakeNames.Length)];
                HUDManager.Instance.AddTextToChatOnServer(name + " joined the game.", -1);
            }
        }
    }
}
