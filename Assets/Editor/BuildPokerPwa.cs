using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPokerPwa
{
    private const string OutputPath = "Build/PWA";
    private const string PwaFilesPath =
        "Assets/WebGLTemplates/PokerPWA/PWA";
    private const string PhotonSettingsPath =
        "Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset";

    [MenuItem("Build/Poker zgadywany PWA (WebGL)")]
    public static void Build()
    {
        ValidatePhotonConfiguration();

        PlayerSettings.productName = "Poker zgadywany";
        PlayerSettings.WebGL.template = "PROJECT:PokerPWA";
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;

        string[] enabledScenes = Array.ConvertAll(
            Array.FindAll(
                EditorBuildSettings.scenes,
                scene => scene.enabled
            ),
            scene => scene.path
        );

        string[] scenes = GetPwaScenes(enabledScenes);

        BuildReport report = BuildPipeline.BuildPlayer(
            scenes,
            OutputPath,
            BuildTarget.WebGL,
            BuildOptions.None
        );

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception(
                "Build PWA nie powiódł się. Sprawdź Console Unity."
            );
        }

        CopyPwaFiles();
        Debug.Log("PWA gotowe w: " + Path.GetFullPath(OutputPath));
    }

    private static void ValidatePhotonConfiguration()
    {
        if (!File.Exists(PhotonSettingsPath))
        {
            throw new Exception(
                "Brak lokalnego PhotonServerSettings.asset. Skonfiguruj Photon przed buildem PWA.");
        }

        string settings = File.ReadAllText(PhotonSettingsPath);
        Match appId = Regex.Match(
            settings,
            @"^\s*AppIdRealtime:\s*(\S.*?)\s*$",
            RegexOptions.Multiline);

        if (!appId.Success || string.IsNullOrWhiteSpace(appId.Groups[1].Value))
        {
            throw new Exception(
                "Photon AppIdRealtime jest pusty. Uzupełnij go lokalnie przed buildem PWA.");
        }
    }

    private static string[] GetPwaScenes(string[] enabledScenes)
    {
        const string mainMenuScene = "Assets/Scenes/MainMenu.unity";
        const string bootLoadingScene = "Assets/Scenes/BootLoading.unity";
        var scenes = new List<string>();

        if (Array.Exists(enabledScenes, scene => scene == mainMenuScene))
            scenes.Add(mainMenuScene);

        foreach (string scene in enabledScenes)
        {
            if (scene == mainMenuScene || scene == bootLoadingScene)
                continue;

            scenes.Add(scene);
        }

        if (scenes.Count == 0 || scenes[0] != mainMenuScene)
            throw new Exception("Scena MainMenu musi być włączona dla buildu PWA.");

        return scenes.ToArray();
    }

    private static void CopyPwaFiles()
    {
        string sourcePath = Path.GetFullPath(PwaFilesPath);
        string destinationPath = Path.GetFullPath(OutputPath);

        foreach (string sourceFile in Directory.GetFiles(
            sourcePath,
            "*",
            SearchOption.AllDirectories
        ))
        {
            if (sourceFile.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = sourceFile.Substring(sourcePath.Length + 1);
            string destinationFile = Path.Combine(
                destinationPath,
                relativePath
            );

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
            File.Copy(sourceFile, destinationFile, true);
        }

        string serviceWorkerPath = Path.Combine(
            destinationPath,
            "service-worker.js"
        );
        string serviceWorker = File.ReadAllText(serviceWorkerPath);
        serviceWorker = serviceWorker.Replace(
            "__CACHE_VERSION__",
            DateTime.UtcNow.Ticks.ToString()
        );
        File.WriteAllText(serviceWorkerPath, serviceWorker);
    }
}
