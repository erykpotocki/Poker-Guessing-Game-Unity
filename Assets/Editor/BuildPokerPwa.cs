using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPokerPwa
{
    private const string OutputPath = "Build/PWA";
    private const string PwaFilesPath =
        "Assets/WebGLTemplates/PokerPWA/PWA";

    [MenuItem("Build/Poker Zgadywany PWA (WebGL)")]
    public static void Build()
    {
        PlayerSettings.productName = "Poker Zgadywany";
        PlayerSettings.WebGL.template = "PROJECT:PokerPWA";
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;

        string[] scenes = Array.ConvertAll(
            Array.FindAll(
                EditorBuildSettings.scenes,
                scene => scene.enabled
            ),
            scene => scene.path
        );

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
