using LethalMenu.Patches;

namespace LethalMenu.Cheats
{
    /// Swaps the gameplay camera's render texture for a screen-resolution one. Newly spawned local
    /// players are handled by FullRenderResolutionPatch (PlayerControllerB.Start postfix).
    public class FullRenderResolutionCheat : CheatBase
    {
        public override string Name => "Full Render Resolution";
        public override Hack HackType => Hack.FullRenderResolution;

        public override void OnEnable() => FullRenderResolutionPatch.ApplyResolution(LethalMenuMod.LocalPlayer);

        public override void OnDisable() => FullRenderResolutionPatch.ApplyResolution(LethalMenuMod.LocalPlayer);
    }
}
