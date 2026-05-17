using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class EntranceTeleportPopup : PopupMenu
    {
        private int _targetIndex;

        public EntranceTeleportPopup() : base("Entrance Teleporter", 20011, 420, 420) { }

        public void Show(PlayerControllerB? preselectTarget = null)
        {
            _targetIndex = 0;
            IsOpen = true;
            // preselectTarget wiring is added in a later task once the target list exists.
        }

        protected override void DrawBody()
        {
            var pairs = BuildEntrancePairs();
            GUILayout.Label($"(Found {pairs.Count} entrance pair(s). target index: {_targetIndex})");
        }

        private static List<EntrancePair> BuildEntrancePairs()
        {
            return LethalMenuMod.Entrances
                .Where(e => e != null)
                .GroupBy(e => e.entranceId)
                .Select(g => new EntrancePair
                {
                    Id = g.Key,
                    OutsideSide = g.FirstOrDefault(e => e.isEntranceToBuilding),
                    InsideSide = g.FirstOrDefault(e => !e.isEntranceToBuilding),
                })
                .OrderBy(p => p.Id)
                .ToList();
        }

        private sealed class EntrancePair
        {
            public int Id;
            public EntranceTeleport? OutsideSide;
            public EntranceTeleport? InsideSide;

            public Vector3 ListPosition =>
                OutsideSide != null ? OutsideSide.transform.position :
                InsideSide != null ? InsideSide.transform.position :
                Vector3.zero;

            public string Label => Id == 0 ? "Main Entrance" : $"Fire Exit #{Id}";

            public bool CanTeleportExternal => InsideSide != null;
            public bool CanTeleportInternal => OutsideSide != null;
        }
    }
}
