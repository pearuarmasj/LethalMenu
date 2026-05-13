using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Raises the DepositItemsDesk per-counter cap (default 150) when InfiniteDeposit is enabled.
    [HarmonyPatch(typeof(DepositItemsDesk), "PlaceItemOnCounter")]
    internal static class InfiniteDepositPatch
    {
        private static int GetCap() => Hack.InfiniteDeposit.IsEnabled() ? 100000 : 150;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getCap = AccessTools.Method(typeof(InfiniteDepositPatch), nameof(GetCap));
            foreach (var instr in instructions)
            {
                // 150 is outside sbyte range, so the IL emits Ldc_I4 not Ldc_I4_S.
                bool match = instr.opcode == OpCodes.Ldc_I4 && instr.operand is int n && n == 150;

                if (match)
                {
                    yield return new CodeInstruction(OpCodes.Call, getCap);
                }
                else
                {
                    yield return instr;
                }
            }
        }
    }
}
