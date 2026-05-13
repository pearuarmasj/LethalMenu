using LethalMenu.Handler;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalMenu.Cheats
{
    /// Master Chams cheat. Iterates each category's object collection per-frame and dispatches
    /// to ChamHandler if the category sub-toggle is enabled. Transition-aware via _wasEnabled so
    /// chams are stripped from all renderers when EnableChams toggles off.
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

            if (Hack.PlayerChams.IsEnabled())
                foreach (var p in LethalMenuMod.Players)
                    Process(p, from);

            if (Hack.EnemyChams.IsEnabled())
                foreach (var e in LethalMenuMod.Enemies)
                    Process(e, from);

            if (Hack.ItemChams.IsEnabled())
                foreach (var i in LethalMenuMod.Items)
                    Process(i, from);

            if (Hack.LandmineChams.IsEnabled())
                foreach (var m in LethalMenuMod.Landmines)
                    Process(m, from);

            if (Hack.TurretChams.IsEnabled())
                foreach (var t in LethalMenuMod.Turrets)
                    Process(t, from);

            if (Hack.DoorChams.IsEnabled())
                foreach (var d in LethalMenuMod.DoorLocks)
                    Process(d, from);

            if (Hack.BigDoorChams.IsEnabled())
                foreach (var d in LethalMenuMod.BigDoors)
                    Process(d, from);

            if (Hack.ShipDoorChams.IsEnabled())
                foreach (var d in LethalMenuMod.HangarShipDoors)
                    Process(d, from);

            if (Hack.BreakerChams.IsEnabled())
                foreach (var b in LethalMenuMod.BreakerBoxes)
                    Process(b, from);

            if (Hack.EnemyVentChams.IsEnabled())
                foreach (var v in LethalMenuMod.EnemyVents)
                    Process(v, from);

            if (Hack.ItemDropshipChams.IsEnabled())
                foreach (var d in LethalMenuMod.ItemDropships)
                    Process(d, from);

            if (Hack.CruiserChams.IsEnabled())
                foreach (var v in LethalMenuMod.Vehicles)
                    Process(v, from);

            if (Hack.MoldSporeChams.IsEnabled())
                foreach (var s in LethalMenuMod.MoldSpores)
                    Process(s, from);

            if (Hack.MineshaftElevatorChams.IsEnabled())
                foreach (var e in LethalMenuMod.MineshaftElevators)
                    Process(e, from);

            if (Hack.EntranceChams.IsEnabled())
                foreach (var e in LethalMenuMod.Entrances)
                    Process(e, from);

            if (Hack.SpikeRoofTrapChams.IsEnabled())
                foreach (var s in LethalMenuMod.SpikeRoofTraps)
                    Process(s, from);

            if (Hack.SteamValveChams.IsEnabled())
                foreach (var v in LethalMenuMod.SteamValves)
                    Process(v, from);
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
