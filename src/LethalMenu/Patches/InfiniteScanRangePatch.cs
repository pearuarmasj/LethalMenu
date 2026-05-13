using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Widens the Q-scanner's sphere-cast radius (20f) and distance (80f) when InfiniteScanRange is enabled.
    [HarmonyPatch(typeof(HUDManager), "AssignNewNodes")]
    internal static class InfiniteScanRangePatch
    {
        private static float Scale(float orig) => Hack.InfiniteScanRange.IsEnabled() ? 10000f : orig;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var scale = AccessTools.Method(typeof(InfiniteScanRangePatch), nameof(Scale));
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4
                    && instr.operand is float f
                    && (f == 20f || f == 80f))
                {
                    yield return instr;
                    yield return new CodeInstruction(OpCodes.Call, scale);
                }
                else
                {
                    yield return instr;
                }
            }
        }
    }
}
