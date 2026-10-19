using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WhereDarknessDwells.Editor
{
    /// <summary>
    /// One-click web release: applies the web player settings, builds the Run scene, and zips the result ready to
    /// attach to a GitHub release, which publishes it to GitHub Pages (.github/workflows/pages.yml).
    /// </summary>
    static class WebBuild
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";
        const string OutputFolder = "Builds/Web";
        const string ZipPath = "Builds/where-darkness-dwells-web.zip";

        [MenuItem("Where Darkness Dwells/Apply Web Settings")]
        public static void ApplySettings()
        {
            var target = NamedBuildTarget.WebGL;

            // GitHub Pages can't send the Content-Encoding header Unity's compressed files need, so the build
            // carries its own small decompressor. Gzip unpacks quickly that way; Brotli would be slow.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:WhereDarknessDwells";

            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;

            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;
            // UI Toolkit and the Input System find parts of themselves by reflection, which heavier stripping can
            // remove. The size saved isn't worth that risk.
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Minimal);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);

            AssetDatabase.SaveAssets();
            Debug.Log("[Editor] Web settings saved: "
                + $"compression {PlayerSettings.WebGL.compressionFormat} with fallback {PlayerSettings.WebGL.decompressionFallback}, "
                + $"template {PlayerSettings.WebGL.template}, managed stripping {PlayerSettings.GetManagedStrippingLevel(target)}.");
        }

        [MenuItem("Where Darkness Dwells/Build for Web")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Editor] Stop Play mode before building.");
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("Web build support is missing",
                    "Add the Web Build Support module to this Unity version in Unity Hub (Installs > Add modules), then try again.", "OK");
                return;
            }

            ApplySettings();

            if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { RunScenePath },
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Editor] Web build {report.summary.result}: see the errors above.");
                return;
            }

            Zip(OutputFolder, ZipPath);
            long bytes = new FileInfo(ZipPath).Length;
            Debug.Log($"[Editor] Web build ready: {ZipPath} ({bytes / (1024f * 1024f):0.0} MB). Attach it to a new GitHub release to publish it.");
            EditorUtility.RevealInFinder(ZipPath);
        }

        /// <summary>Zips a folder's contents (not the folder itself), so index.html sits at the top of the zip.</summary>
        static void Zip(string folder, string zipPath)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            string root = Path.GetFullPath(folder);
            using (FileStream file = File.Create(zipPath))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string name = path.Substring(root.Length + 1).Replace('\\', '/');
                    // The build files are already compressed, so light compression is plenty.
                    ZipArchiveEntry entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Fastest);
                    using (Stream input = File.OpenRead(path))
                    using (Stream output = entry.Open())
                        input.CopyTo(output);
                }
            }
        }
    }
}
