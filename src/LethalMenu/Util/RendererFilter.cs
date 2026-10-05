using System.Collections.Generic;
using UnityEngine;

namespace LethalMenu.Util
{
    /// Separates an object's visible body meshes from the helper renderers prefabs carry along:
    /// scan nodes, radar/map dots, trigger volumes, terminal labels, lower LODs and runtime
    /// placeholder materials. Used by the creature preview and by ESP bounds.
    public static class RendererFilter
    {
        /// `root` limits the name path checks to the object's own hierarchy (so a scene container
        /// like "mapPropsContainer" above it doesn't count); null checks the full scene path.
        public static bool IsVisual(Renderer renderer, Transform? root = null) =>
            IsBodyRenderer(renderer, root, out int lod) && lod <= 0;

        /// The visible body renderers under `target`, highest detail only: renderers without a LOD tag plus
        /// those of the lowest LOD level present. Some models start at LOD1 (the player / Masked body mesh is
        /// named "LOD1"), so "lowest present" rather than "LOD0".
        public static void CollectVisual(Component target, List<Renderer> into)
        {
            into.Clear();
            var root = target.transform;
            int lowest = int.MaxValue;
            var lods = new List<int>();
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!IsBodyRenderer(renderer, root, out int lod)) continue;
                into.Add(renderer);
                lods.Add(lod);
                if (lod >= 0 && lod < lowest) lowest = lod;
            }

            for (int i = into.Count - 1; i >= 0; i--)
                if (lods[i] >= 0 && lods[i] != lowest)
                    into.RemoveAt(i);
        }

        /// False for helper renderers. `lod` is the LOD level named in the renderer's path, -1 when untagged.
        private static bool IsBodyRenderer(Renderer renderer, Transform? root, out int lod)
        {
            lod = -1;
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) return false;

            string path = PathFrom(renderer.transform, root).ToLowerInvariant();
            if (path.Contains("scannode") ||
                path.Contains("scan node") ||
                path.Contains("scan") ||
                path.Contains("mapdot") ||
                path.Contains("map dot") ||
                path.Contains("map") ||
                path.Contains("radar") ||
                path.Contains("terminal") ||
                path.Contains("trigger") ||
                path.Contains("playerhead") ||
                path.Contains("player head") ||
                path.Contains("tongue"))
                return false;

            int tag = path.LastIndexOf("lod");
            if (tag >= 0 && tag + 3 < path.Length && char.IsDigit(path[tag + 3]))
                lod = path[tag + 3] - '0';

            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                string name = material.name.ToLowerInvariant();
                if (name.Contains("testtrigger") || name.Contains("ghostsheet") ||
                    name.Contains("mapdot") || name.Contains("defaulthdmaterial"))
                    return false;
            }

            return true;
        }

        private static string PathFrom(Transform transform, Transform? root)
        {
            string path = transform.name;
            while (transform != root && transform.parent != null && transform.parent != root)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
