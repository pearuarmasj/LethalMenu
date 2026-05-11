using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LethalMenu.Theme
{
    public static class ThemeLoader
    {
        public static string CurrentName { get; private set; } = "Default";
        public static GUISkin? Skin { get; private set; }
        private static AssetBundle? _assetBundle;

        public static string[] GetAvailableThemes()
        {
            var prefix = "LethalMenu.Resources.Theme.";
            var suffix = ".skin";
            return Assembly.GetExecutingAssembly()
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(prefix) && r.EndsWith(suffix))
                .Select(r => r.Substring(prefix.Length, r.Length - prefix.Length - suffix.Length))
                .OrderBy(n => n)
                .ToArray();
        }

        public static void SetTheme(string themeName)
        {
            if (CurrentName == themeName && Skin != null && _assetBundle != null)
                return;

            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"LethalMenu.Resources.Theme.{themeName}.skin");

            if (stream == null)
            {
                Loader.Log($"Theme '{themeName}' not found, falling back to Default");
                themeName = "Default";
                stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("LethalMenu.Resources.Theme.Default.skin");
                if (stream == null) return;
            }

            _assetBundle?.Unload(true);

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                _assetBundle = AssetBundle.LoadFromMemory(ms.ToArray());
            }
            stream.Dispose();

            if (_assetBundle == null) return;

            Skin = _assetBundle.LoadAsset<GUISkin>("assets/lethalmenu.guiskin");
            CurrentName = themeName;
            Loader.Log($"Loaded theme: {themeName}");
        }
    }
}
