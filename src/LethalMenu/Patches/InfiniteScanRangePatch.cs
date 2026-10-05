using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Widens the Q-scanner's sphere-cast radius (20f) and max distance (80f) when InfiniteScanRange is enabled.
    /// AssignNewNodes has two 20f literals: the sphere radius and the ray-origin offset (`forward * 20f`). The offset
    /// is recognised by the op_Multiply right after it and left alone, otherwise the cast would start 10 km ahead
    /// of the camera and miss everything nearby.
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AssignNewNodes))]
    internal static class InfiniteScanRangePatch
    {
        private static float Scale(float orig) => Hack.InfiniteScanRange.IsEnabled() ? 10000f : orig;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var scale = AccessTools.Method(typeof(InfiniteScanRangePatch), nameof(Scale));
            var vectorTimesFloat = AccessTools.Method(typeof(Vector3), "op_Multiply", new[] { typeof(Vector3), typeof(float) });
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var instr = codes[i];
                yield return instr;
                if (instr.opcode != OpCodes.Ldc_R4 || instr.operand is not float f) continue;
                if (f != 20f && f != 80f) continue;
                if (i + 1 < codes.Count && codes[i + 1].Calls(vectorTimesFloat)) continue;
                yield return new CodeInstruction(OpCodes.Call, scale);
            }
        }
    }

    /// HUDManager.MeetsScanNodeRequirements rejects nodes outside node.maxRange/minRange and, optionally, behind
    /// geometry (linecast). With InfiniteScanRange enabled every non-null node passes so the scanner shows everything.
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.MeetsScanNodeRequirements))]
    internal static class ScanNodeRequirementsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ScanNodeProperties node, ref bool __result)
        {
            if (!Hack.InfiniteScanRange.IsEnabled() || node == null) return true;
            __result = true;
            return false;
        }
    }
}
