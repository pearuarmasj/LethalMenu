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
        public static bool IsVisual(Renderer renderer, Transform? root = null)
        {
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

            // Keep LOD0 (or no LOD suffix); drop LOD1..LOD9.
            for (int digit = 1; digit <= 9; digit++)
            {
                if (path.Contains("lod" + digit))
                    return false;
            }

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
