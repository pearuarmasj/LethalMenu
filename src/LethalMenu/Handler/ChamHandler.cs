using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalMenu.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalMenu.Handler
{
    /// Per-object material swap. Substitutes a cham material (Unity's Hidden/Internal-Colored
    /// shader, ZTest Always, ZWrite Off, double-sided) so chammed objects glow through walls in their
    /// category color. Works on sharedMaterials only: Renderer.materials instantiates a copy per
    /// renderer on every access, which leaked materials and saved copies instead of the originals.
    /// One cham material per color is cached; renderers swap to the matching one when a color changes.
    public class ChamHandler
    {
        private static readonly Dictionary<int, Material[]> _originalMaterials = new();
        private static readonly Dictionary<Color, Material> _colorMaterials = new();
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
            var material = MaterialFor(Settings.UseSingleChamColor ? Settings.ChamColor : ResolveCategoryColor());
            foreach (var r in renderers)
            {
                if (r == null) continue;
                int id = r.GetInstanceID();
                var current = r.sharedMaterials;
                if (!_originalMaterials.ContainsKey(id))
                    _originalMaterials[id] = current;
                else if (current.Length > 0 && current[0] == material)
                    continue; // already chammed in this color

                var swapped = new Material[_originalMaterials[id].Length];
                for (int i = 0; i < swapped.Length; i++) swapped[i] = material;
                r.sharedMaterials = swapped;
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
                r.sharedMaterials = original;
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
                    r.sharedMaterials = _originalMaterials[id];
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

        private static Material MaterialFor(Color color)
        {
            if (_colorMaterials.TryGetValue(color, out var material) && material != null)
                return material;

            material = new Material(_chamMaterial!) { hideFlags = _chamMaterial!.hideFlags };
            material.SetColor(_colorPropId, color);
            _colorMaterials[color] = material;
            return material;
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
