namespace LethalMenu.Cheats
{
    /// SaneMod — zero out the local player's insanity meter every frame.
    public class SaneCheat : CheatBase
    {
        public override string Name => "Sane Mode";
        public override Hack HackType => Hack.SaneMod;

        public override void OnUpdate()
        {
            if (!IsEnabled) return;
            var p = LethalMenuMod.LocalPlayer;
            if (p == null) return;
            p.insanityLevel = 0f;
        }
    }
}
