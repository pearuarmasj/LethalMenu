using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMenu.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalMenu.Handler
{
    /// Per-object material swap. Substitutes a single shared cham material (Unity's
    /// Hidden/Internal-Colored shader, ZTest Always, ZWrite Off, double-sided) so chammed
    /// objects glow through walls in their category color.
    public class ChamHandler
    {
        private static readonly Dictionary<int, Material[]> _originalMaterials = new();
        private static Material? _chamMaterial;
        private static int _colorPropId;

        private readonly Object _target;

        public ChamHandler(Object target) { _target = target; }
        public static ChamHandler For(Object obj) => new ChamHandler(obj);

        private static bool _setupAttempted;

        /// Lazy setup. Idempotent. Safe to call from any frame after Unity has booted.
        /// Wrapped in try/catch so a shader-find or material-create failure can't crash injection.
        public static void Setup()
        {
            if (_setupAttempted) return;
            _setupAttempted = true;
            try
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null)
                {
                    Debug.LogError("[ChamHandler] Shader 'Hidden/Internal-Colored' not found. Chams disabled.");
                    return;
                }

                _chamMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy
                };
                _chamMaterial.SetInt("_SrcBlend", 5);  // SrcAlpha
                _chamMaterial.SetInt("_DstBlend", 10); // OneMinusSrcAlpha
                _chamMaterial.SetInt("_Cull", 0);      // double-sided
                _chamMaterial.SetInt("_ZTest", 8);     // Always
                _chamMaterial.SetInt("_ZWrite", 0);
                _colorPropId = Shader.PropertyToID("_Color");

                if (LethalMenuMod.Instance != null)
                    LethalMenuMod.Instance.StartCoroutine(CleanupOrphanedMaterials());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ChamHandler] Setup failed, chams disabled: {ex}");
                _chamMaterial = null;
            }
        }

        public void ProcessCham(float distance)
        {
            if (_target == null || _chamMaterial == null) return;
            bool shouldApply = ShouldApplyForType() && distance >= Settings.ChamDistance;
            if (shouldApply) ApplyCham();
            else RemoveCham();
        }

        public void ApplyCham()
        {
            var renderers = GetRenderers();
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null || r.materials == null) continue;
                int id = r.GetInstanceID();
                if (_originalMaterials.ContainsKey(id)) continue; // already chammed
                _originalMaterials[id] = r.materials;
                r.SetMaterials(Enumerable.Repeat(_chamMaterial!, r.materials.Length).ToList());
                UpdateChamColor(r);
            }
        }

        public void RemoveCham()
        {
            var renderers = GetRenderers();
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                int id = r.GetInstanceID();
                if (!_originalMaterials.TryGetValue(id, out var original)) continue;
                r.SetMaterials(original.ToList());
                _originalMaterials.Remove(id);
            }
        }

        public static void RemoveAllChams()
        {
            // Best-effort cleanup on cheat disable. Restore materials we can still find;
            // orphans get cleared from the dictionary regardless.
            var ids = _originalMaterials.Keys.ToList();
            var liveRenderers = Object.FindObjectsOfType<Renderer>()
                .ToDictionary(r => r.GetInstanceID(), r => r);

            foreach (var id in ids)
            {
                if (liveRenderers.TryGetValue(id, out var r) && r != null)
                {
                    r.SetMaterials(_originalMaterials[id].ToList());
                }
                _originalMaterials.Remove(id);
            }
        }

        private bool ShouldApplyForType()
        {
            return _target switch
            {
                GrabbableObject _ => Hack.ItemChams.IsEnabled(),
                Landmine _ => Hack.LandmineChams.IsEnabled(),
                GameNetcodeStuff.PlayerControllerB _ => Hack.PlayerChams.IsEnabled(),
                EnemyAI _ => Hack.EnemyChams.IsEnabled(),
                SteamValveHazard steam => Hack.SteamValveChams.IsEnabled()
                    && !steam.Reflect().GetField<bool>("valveHasBeenRepaired"),
                TerminalAccessibleObject term => term.isBigDoor && Hack.BigDoorChams.IsEnabled(),
                DoorLock _ => Hack.DoorChams.IsEnabled(),
                HangarShipDoor _ => Hack.ShipDoorChams.IsEnabled(),
                BreakerBox _ => Hack.BreakerChams.IsEnabled(),
                EnemyVent _ => Hack.EnemyVentChams.IsEnabled(),
                ItemDropship _ => Hack.ItemDropshipChams.IsEnabled(),
                VehicleController _ => Hack.CruiserChams.IsEnabled(),
                MineshaftElevatorController _ => Hack.MineshaftElevatorChams.IsEnabled(),
                EntranceTeleport _ => Hack.EntranceChams.IsEnabled(),
                Turret _ => Hack.TurretChams.IsEnabled(),
                GameObject go when go.name.StartsWith("MoldSpore", System.StringComparison.Ordinal)
                    => Hack.MoldSporeChams.IsEnabled(),
                GameObject go when go.name.StartsWith("AnimContainer", System.StringComparison.Ordinal)
                    => Hack.SpikeRoofTrapChams.IsEnabled(),
                GameObject go when go.name.StartsWith("TurretContainer", System.StringComparison.Ordinal)
                    => Hack.TurretChams.IsEnabled(),
                _ => false
            };
        }

        private void UpdateChamColor(Renderer renderer)
        {
            if (renderer == null || renderer.materials == null) return;

            Color color = Settings.UseSingleChamColor ? Settings.ChamColor : ResolveCategoryColor();
            foreach (var m in renderer.materials)
                if (m != null) m.SetColor(_colorPropId, color);
        }

        private Color ResolveCategoryColor()
        {
            return _target switch
            {
                GrabbableObject _ => Settings.ItemChamColor,
                Landmine _ => Settings.LandmineChamColor,
                GameNetcodeStuff.PlayerControllerB _ => Settings.PlayerChamColor,
                EnemyAI _ => Settings.EnemyChamColor,
                SteamValveHazard _ => Settings.SteamValveChamColor,
                TerminalAccessibleObject _ => Settings.BigDoorChamColor,
                DoorLock _ => Settings.DoorChamColor,
                HangarShipDoor _ => Settings.ShipDoorChamColor,
                BreakerBox _ => Settings.BreakerChamColor,
                EnemyVent _ => Settings.EnemyVentChamColor,
                ItemDropship _ => Settings.ItemDropshipChamColor,
                VehicleController _ => Settings.CruiserChamColor,
                MineshaftElevatorController _ => Settings.MineshaftElevatorChamColor,
                EntranceTeleport _ => Settings.EntranceChamColor,
                Turret _ => Settings.TurretChamColor,
                GameObject go when go.name.StartsWith("MoldSpore", System.StringComparison.Ordinal)
                    => Settings.MoldSporeChamColor,
                GameObject go when go.name.StartsWith("AnimContainer", System.StringComparison.Ordinal)
                    => Settings.SpikeRoofTrapChamColor,
                GameObject go when go.name.StartsWith("TurretContainer", System.StringComparison.Ordinal)
                    => Settings.TurretChamColor,
                _ => Settings.ChamColor
            };
        }

        private List<Renderer>? GetRenderers()
        {
            if (_target == null) return null;
            // DoorLock mesh sits on the parent (LM-master special case).
            if (_target is DoorLock door) return door.GetComponentsInParent<Renderer>().ToList();
            if (_target is RadMechAI rad) return rad.GetComponentsInChildren<Renderer>().ToList();
            if (_target is GameObject go) return go.GetComponentsInChildren<Renderer>().ToList();
            if (_target is Component component) return component.GetComponentsInChildren<Renderer>().ToList();
            return null;
        }

        private static IEnumerator CleanupOrphanedMaterials()
        {
            while (true)
            {
                yield return new WaitForSeconds(15f);
                var liveIds = new HashSet<int>();
                foreach (var r in Object.FindObjectsOfType<Renderer>())
                    if (r != null) liveIds.Add(r.GetInstanceID());

                var orphans = _originalMaterials.Keys.Where(k => !liveIds.Contains(k)).ToList();
                foreach (var k in orphans) _originalMaterials.Remove(k);
            }
        }
    }
}
