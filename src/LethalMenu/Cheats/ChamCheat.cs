using LethalMenu.Handler;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalMenu.Cheats
{
    /// Master Chams cheat. Iterates every category's object collection per-frame unconditionally and
    /// dispatches to ChamHandler.ProcessCham, which decides apply-vs-remove based on the sub-toggle.
    /// Unconditional iteration is required so that flipping a sub-toggle OFF strips chams on the next frame.
    public class ChamCheat : CheatBase
    {
        public override string Name => "Chams";
        public override Hack HackType => Hack.EnableChams;

        private bool _wasEnabled;

        public override void OnUpdate()
        {
            bool now = IsEnabled;
            if (!now)
            {
                if (_wasEnabled)
                {
                    ChamHandler.RemoveAllChams();
                    _wasEnabled = false;
                }
                return;
            }

            // Lazy init the cham material on the first frame we're enabled.
            ChamHandler.Setup();
            _wasEnabled = true;

            var local = LethalMenuMod.LocalPlayer;
            if (local == null) return;
            Vector3 from = local.transform.position;

            // Iterate every category unconditionally — ProcessCham gates on the sub-toggle and removes
            // chams when the toggle is off. Skipping disabled categories here would leave chammed
            // renderers stuck because ProcessCham would never run for them.

            foreach (var p in LethalMenuMod.Players)
            {
                // Skip local player — own body renderers sit at camera position and would fill the screen.
                if (p == LethalMenuMod.LocalPlayer) continue;
                Process(p, from);
            }

            foreach (var e in LethalMenuMod.Enemies) Process(e, from);
            foreach (var i in LethalMenuMod.Items) Process(i, from);
            foreach (var m in LethalMenuMod.Landmines) Process(m, from);
            foreach (var t in LethalMenuMod.Turrets) Process(t, from);
            foreach (var d in LethalMenuMod.DoorLocks) Process(d, from);
            foreach (var d in LethalMenuMod.BigDoors) Process(d, from);
            foreach (var d in LethalMenuMod.HangarShipDoors) Process(d, from);
            foreach (var b in LethalMenuMod.BreakerBoxes) Process(b, from);
            foreach (var v in LethalMenuMod.EnemyVents) Process(v, from);
            foreach (var d in LethalMenuMod.ItemDropships) Process(d, from);
            foreach (var v in LethalMenuMod.Vehicles) Process(v, from);
            foreach (var s in LethalMenuMod.MoldSpores) Process(s, from);
            foreach (var e in LethalMenuMod.MineshaftElevators) Process(e, from);
            foreach (var e in LethalMenuMod.Entrances) Process(e, from);
            foreach (var s in LethalMenuMod.SpikeRoofTraps) Process(s, from);
            foreach (var v in LethalMenuMod.SteamValves) Process(v, from);
        }

        private static void Process(Object obj, Vector3 from)
        {
            if (obj == null) return;
            Vector3 pos = obj switch
            {
                Component c => c.transform.position,
                GameObject go => go.transform.position,
                _ => from
            };
            float distance = Vector3.Distance(pos, from);
            ChamHandler.For(obj).ProcessCham(distance);
        }
    }
}
