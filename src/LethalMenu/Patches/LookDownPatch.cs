using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Widens the vertical look clamp from +-80 to +-89 when LookDown is enabled.
    /// Verified against LCSource: CalculateNormalLookingInput contains exactly one Mathf.Clamp(cameraUp, -80f, 80f)
    /// and no other -80f/80f literals. Replacing every match is safe for this specific method.
    [HarmonyPatch(typeof(PlayerControllerB), "CalculateNormalLookingInput")]
    internal static class LookDownPatch
    {
        private static float GetMinLook() => Hack.LookDown.IsEnabled() ? -89f : -80f;
        private static float GetMaxLook() => Hack.LookDown.IsEnabled() ? 89f : 80f;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var minMethod = AccessTools.Method(typeof(LookDownPatch), nameof(GetMinLook));
            var maxMethod = AccessTools.Method(typeof(LookDownPatch), nameof(GetMaxLook));
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float f && f == -80f)
                    yield return new CodeInstruction(OpCodes.Call, minMethod);
                else if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float g && g == 80f)
                    yield return new CodeInstruction(OpCodes.Call, maxMethod);
                else
                    yield return instr;
            }
        }
    }
}
