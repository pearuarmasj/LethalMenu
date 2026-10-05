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

        // Callers re-request the theme every OnGUI while Skin is null; remember a failed load so it
        // isn't retried (and logged) several times a frame.
        private static string? _failedTheme;

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
            if (themeName == _failedTheme)
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

            Skin = _assetBundle != null ? _assetBundle.LoadAsset<GUISkin>("assets/lethalmenu.guiskin") : null;
            if (Skin == null)
            {
                _failedTheme = themeName;
                Loader.LogError($"Theme '{themeName}' failed to load");
                return;
            }

            _failedTheme = null;
            CurrentName = themeName;
            Loader.Log($"Loaded theme: {themeName}");
        }
    }
}
