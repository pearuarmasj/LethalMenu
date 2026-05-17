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
            var localPlayer = LethalMenuMod.LocalPlayer;
            var targets = BuildTargetList(localPlayer);
            if (_targetIndex < 0 || _targetIndex >= targets.Count)
                _targetIndex = 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target:", GUILayout.Width(60));
            if (GUILayout.Button(targets[_targetIndex].Label))
                _targetIndex = (_targetIndex + 1) % targets.Count;
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            var pairs = BuildEntrancePairs();
            if (pairs.Count == 0)
            {
                GUILayout.Label("No entrances on this moon.");
                return;
            }

            foreach (var pair in pairs)
            {
                string distanceLabel = localPlayer != null
                    ? $"{Vector3.Distance(localPlayer.transform.position, pair.ListPosition):F1} m"
                    : "— m";

                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();
                GUILayout.Label(pair.Label);
                GUILayout.FlexibleSpace();
                GUILayout.Label(distanceLabel);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                bool externalEnabled = pair.CanTeleportExternal;
                GUI.enabled = externalEnabled;
                GUILayout.Button(externalEnabled ? "External" : "External (—)");
                GUI.enabled = true;

                bool internalEnabled = pair.CanTeleportInternal;
                GUI.enabled = internalEnabled;
                GUILayout.Button(internalEnabled ? "Internal" : "Internal (—)");
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(2f);
            }
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

        private static List<TargetEntry> BuildTargetList(PlayerControllerB? localPlayer)
        {
            var result = new List<TargetEntry>
            {
                new() { Label = "Self (you)", Player = localPlayer },
            };

            foreach (var p in LethalMenuMod.Players
                .Where(p => p != null && p != localPlayer)
                .OrderBy(p => p.playerUsername))
            {
                result.Add(new TargetEntry { Label = p.playerUsername, Player = p });
            }

            return result;
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

        private sealed class TargetEntry
        {
            public string Label = "";
            public PlayerControllerB? Player;
        }
    }
}
