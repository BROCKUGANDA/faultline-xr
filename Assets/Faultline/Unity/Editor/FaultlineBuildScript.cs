namespace Faultline.Editor
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    using UnityEditor;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Release-candidate build entry point invoked from the command line:
    /// configures the project, builds the Android ARM64 APK into
    /// submission/build/, then writes a checksummed build manifest so the exact
    /// submitted artifact is auditable (playbook items 367-369, 377).
    /// </summary>
    public static class FaultlineBuildScript
    {
        private const string OutputFolder = "submission/build";
        private const string ApkName = "FAULTLINE-XR.apk";

        public static void BuildApk()
        {
            try
            {
                FaultlineProjectConfigurer.Configure();

                if (!EditorUserBuildSettings.activeBuildTarget.Equals(BuildTarget.Android))
                {
                    throw new InvalidOperationException(
                        "Faultline: active build target is " + EditorUserBuildSettings.activeBuildTarget +
                        ". Switch to Android before building the APK.");
                }

                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
                EditorUserBuildSettings.development = false;

                Directory.CreateDirectory(OutputFolder);
                var apkPath = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder, ApkName);

                var options = BuildOptions.None;
                var keystore = Environment.GetEnvironmentVariable("FAULTLINE_ANDROID_KEYSTORE");
                if (!string.IsNullOrWhiteSpace(keystore))
                {
                    PlayerSettings.Android.keystorePass =
                        Environment.GetEnvironmentVariable("FAULTLINE_ANDROID_KEYSTORE_PASS") ?? string.Empty;
                    PlayerSettings.Android.keyaliasPass =
                        Environment.GetEnvironmentVariable("FAULTLINE_ANDROID_KEYALIAS_PASS") ?? string.Empty;
                    PlayerSettings.Android.keystoreName = keystore;
                    PlayerSettings.Android.keyaliasName =
                        Environment.GetEnvironmentVariable("FAULTLINE_ANDROID_KEYALIAS") ?? null;
                    Debug.Log("Faultline: signing with the supplied release keystore.");
                }
                else
                {
                    Debug.LogWarning("Faultline: no release keystore supplied — Unity signs with its " +
                                     "debug keystore. The APK installs on a developer-mode headset, and " +
                                     "the manifest records signing=unity-debug-default.");
                }

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Length > 0
                        ? Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path)
                        : new[] { FaultlineProjectConfigurer.MainScenePath },
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    subtarget = (int)StandaloneAndroidBuildType.Application,
                    options = options,
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"Faultline: Android build {report.summary.result} — " +
                                   $"errors={report.summary.totalErrors}, " +
                                   $"warnings={report.summary.totalWarnings}, " +
                                   $"steps={report.summary.totalSteps}, " +
                                   $"time={report.summary.totalTime.TotalSeconds:F1}s");
                    throw new InvalidOperationException("Faultline: BuildPlayer reported " + report.summary.result);
                }

                var sha256 = ComputeSha256(apkPath);
                var manifest = BuildManifestJson(apkPath, sha256, report, string.IsNullOrWhiteSpace(keystore)
                    ? "unity-debug-default"
                    : "provided-release-keystore");

                var manifestPath = Path.Combine(OutputFolder, "build_manifest.json");
                File.WriteAllText(manifestPath, manifest, new UTF8Encoding(false));

                Debug.Log($"Faultline: APK built — {apkPath}\n" +
                          $"size={new FileInfo(apkPath).Length} bytes sha256={sha256}\n" +
                          $"manifest={manifestPath}");
            }
            catch (Exception exception)
            {
                Debug.LogError("Faultline: build failed — " + exception);
                EditorApplication.Exit(1);
                throw;
            }
        }

        private static string BuildManifestJson(string apkPath, string sha256, BuildReport report, string signing)
        {
            var packageVersion = ReadPackageVersion("com.meta.xr.sdk.all");
            var openXrVersion = ReadPackageVersion("com.unity.xr.openxr");
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"buildType\": \"Unity Android ARM64 APK\",\n");
            sb.Append($"  \"targetPlatform\": \"Meta Quest (Android, OpenXR + Meta XR SDK)\",\n");
            sb.Append($"  \"apkPath\": \"{PathRelativeToProject(apkPath).Replace("\\", "/")}\",\n");
            sb.Append($"  \"applicationIdentifier\": \"{PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}\",\n");
            sb.Append($"  \"productVersion\": \"{PlayerSettings.bundleVersion}\",\n");
            sb.Append($"  \"versionCode\": \"{PlayerSettings.bundleVersionCode}\",\n");
            sb.Append($"  \"unityVersion\": \"{Application.unityVersion}\",\n");
            sb.Append($"  \"metaXrSdkVersion\": \"{packageVersion}\",\n");
            sb.Append($"  \"openXrVersion\": \"{openXrVersion}\",\n");
            sb.Append($"  \"scriptingBackend\": \"IL2CPP\",\n");
            sb.Append($"  \"targetArchitectures\": \"ARM64\",\n");
            sb.Append($"  \"minSdkVersion\": {MinimumSdk()},\n");
            sb.Append($"  \"signing\": \"{signing}\",\n");
            sb.Append($"  \"sha256Checksum\": \"{sha256}\",\n");
            sb.Append($"  \"apkSizeBytes\": {new FileInfo(apkPath).Length},\n");
            sb.Append($"  \"gitCommit\": \"{GitCommit()}\",\n");
            sb.Append($"  \"buildTimestampUtc\": \"{DateTime.UtcNow:O}\",\n");
            sb.Append($"  \"buildResult\": \"{report.summary.result}\",\n");
            sb.Append($"  \"buildTotalSeconds\": {report.summary.totalTime.TotalSeconds:F1},\n");
            sb.Append($"  \"scenes\": [\"{FaultlineProjectConfigurer.MainScenePath.Replace("\\", "/")}\"],\n");
            sb.Append($"  \"deviceValidation\": \"not run in this environment — install and smoke-test on a headset before submission\"\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        private static int MinimumSdk() =>
            (int)PlayerSettings.Android.minSdkVersion;

        /// <summary>
        /// Read a pinned package version straight out of Packages/manifest.json so
        /// the manifest records the toolchain that actually produced the APK.
        /// </summary>
        private static string ReadPackageVersion(string packageName)
        {
            try
            {
                var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
                if (!File.Exists(manifestPath)) return "unknown";

                var key = "\"" + packageName + "\":";
                foreach (var line in File.ReadAllLines(manifestPath))
                {
                    var index = line.IndexOf(key, StringComparison.Ordinal);
                    if (index < 0) continue;

                    var value = line.Substring(index + key.Length).Trim().TrimEnd(',').Trim();
                    value = value.Trim('"');
                    if (!string.IsNullOrEmpty(value)) return value;
                }
                return "unknown";
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private static string GitCommit()
        {
            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using (var process = Process.Start(start))
                {
                    if (process == null) return "unknown";
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(3000);
                    return string.IsNullOrEmpty(output) ? "unknown" : output;
                }
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private static string PathRelativeToProject(string absolutePath)
        {
            var root = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar);
            return absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? absolutePath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar)
                : absolutePath;
        }
    }
}