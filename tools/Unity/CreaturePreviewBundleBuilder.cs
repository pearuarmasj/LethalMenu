#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreaturePreviewBundleBuilder
{
    private const string OutputFolderName = "CreaturePreviewBundles";

    [MenuItem("LethalMenu/Creature Previews/Build Selected Prefabs")]
    public static void BuildSelectedPrefabs()
    {
        var prefabPaths = new List<string>();
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                continue;

            prefabPaths.Add(path);
        }

        Build(prefabPaths);
    }

    [MenuItem("LethalMenu/Creature Previews/Build All Creature Prefabs")]
    public static void BuildAllCreaturePrefabs()
    {
        var prefabPaths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!LooksLikeCreaturePrefab(name))
                continue;

            prefabPaths.Add(path);
        }

        Build(prefabPaths);
    }

    private static void Build(List<string> prefabPaths)
    {
        if (prefabPaths.Count == 0)
        {
            Debug.LogWarning("No creature preview prefabs selected/found.");
            return;
        }

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolderName));
        Directory.CreateDirectory(outputPath);

        var builds = new AssetBundleBuild[prefabPaths.Count];
        for (int i = 0; i < prefabPaths.Count; i++)
        {
            string assetPath = prefabPaths[i];
            string prefabName = Path.GetFileNameWithoutExtension(assetPath);
            builds[i] = new AssetBundleBuild
            {
                assetBundleName = Normalize(prefabName) + ".bundle",
                assetNames = new[] { assetPath }
            };
        }

        BuildPipeline.BuildAssetBundles(
            outputPath,
            builds,
            BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);

        Debug.Log($"Built {prefabPaths.Count} creature preview bundle(s) to {outputPath}");
    }

    private static bool LooksLikeCreaturePrefab(string name)
    {
        string normalized = Normalize(name);
        return normalized.Contains("giant") ||
            normalized.Contains("forest") ||
            normalized.Contains("crawler") ||
            normalized.Contains("centipede") ||
            normalized.Contains("spider") ||
            normalized.Contains("flowerman") ||
            normalized.Contains("baboon") ||
            normalized.Contains("mouthdog") ||
            normalized.Contains("hoarder") ||
            normalized.Contains("blob") ||
            normalized.Contains("jester") ||
            normalized.Contains("puffer") ||
            normalized.Contains("masked") ||
            normalized.Contains("radmech") ||
            normalized.Contains("manticoil") ||
            normalized.Contains("tulip") ||
            normalized.Contains("maneater");
    }

    private static string Normalize(string value)
    {
        char[] buffer = new char[value.Length];
        int length = 0;
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                buffer[length++] = char.ToLowerInvariant(c);
        }

        return new string(buffer, 0, length);
    }
}
#endif
