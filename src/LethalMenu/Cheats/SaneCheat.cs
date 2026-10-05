namespace LethalMenu.Cheats
{
    /// SaneMode — zero out the local player's insanity meter every frame.
    public class SaneCheat : CheatBase
    {
        public override string Name => "Sane Mode";
        public override Hack HackType => Hack.SaneMode;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            var p = LethalMenuMod.LocalPlayer;
            if (p == null) return;
            p.insanityLevel = 0f;
        }
    }
}
