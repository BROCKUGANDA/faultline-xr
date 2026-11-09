namespace Faultline.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using UnityEditor;
    using UnityEditor.XR.Management;
    using UnityEngine;
    using UnityEngine.XR.Management;
    using UnityEngine.XR.OpenXR;

    using Faultline.Runtime.UI;

    using UnityXRLoader = UnityEngine.XR.Management.XRLoader;

    /// <summary>
    /// Reproducible project configuration for the FAULTLINE XR Android build.
    /// Everything the GUI steps in docs/engineering/BUILD_GUIDE.md describe is
    /// applied here so a clean clone produces the identical player settings,
    /// XR loader stack, shader inclusion list and scene list.
    /// </summary>
    public static class FaultlineProjectConfigurer
    {
        internal const string BundleIdentifier = "com.faultline.xr";
        internal const string ProductName = "FAULTLINE XR";
        internal const string CompanyName = "Faultline";
        internal const int MinimumSdk = 29;
        internal const string MainScenePath = "Assets/Faultline/Scenes/Main.unity";

        /// <summary>OpenXR features required for controller-free hand interaction on Quest.</summary>
        private static readonly (string TypeName, string AssemblyName, bool Required)[] FeatureTypes =
        {
            ("UnityEngine.XR.Hands.OpenXR.HandTracking", "Unity.XR.Hands", true),
            ("UnityEngine.XR.OpenXR.Features.Interactions.MetaQuestTouchProControllerProfile", "Unity.XR.OpenXR", false),
            ("UnityEngine.XR.OpenXR.Features.Interactions.HandInteractionProfile", "Unity.XR.OpenXR", false),
            ("UnityEngine.XR.OpenXR.Features.Meta.MetaOpenXRFeature", "Unity.XR.MetaOpenXR", false),
        };

        [MenuItem("Faultline/Configure Project")]
        public static void Configure()
        {
            ApplyPlayerSettings();
            ApplyXrSettings();
            ApplyGraphicsSettings();
            ApplySceneList();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Faultline: project configuration applied (Android ARM64, OpenXR + hand tracking, scenes).");
        }

        // ------------------------------------------------------------------
        // Player settings
        // ------------------------------------------------------------------

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleIdentifier);

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(BuildTargetGroup.Android, 2); // 2 == ARM64 only
            PlayerSettings.SetScriptingDefineStrings(BuildTargetGroup.Android, Array.Empty<string>());

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)Mathf.Max(
                (int)PlayerSettings.Android.minSdkVersion,
                (int)PlayerSettings.Android.targetSdkVersion);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.useCustomGradleTemplate = false;

            PlayerSettings.allowUnsafeCode = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripEditorTestAssemblies = true;
            PlayerSettings.visibleInBackground = false;
            PlayerSettings.runInBackground = false;

            // 72 Hz matches the Quest refresh ladder and the app target frame rate.
            PlayerSettings.SetUseDefaultResolution(BuildTargetGroup.Android, true);
        }

        // ------------------------------------------------------------------
        // XR plug-in management + OpenXR features
        // ------------------------------------------------------------------

        private static void ApplyXrSettings()
        {
            var perTarget = XRGeneralSettingsPerBuildTarget.Instance;

            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var manager = perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            if (manager == null)
            {
                Debug.LogError("Faultline: could not obtain Android XR manager settings.");
                return;
            }

            var loaders = manager.Loaders ?? new List<UnityXRLoader>();
            var openXrLoader = loaders.OfType<UnityEngine.XR.OpenXR.OpenXRLoader>().FirstOrDefault();
            if (openXrLoader == null)
            {
                openXrLoader = ScriptableObject.CreateInstance<UnityEngine.XR.OpenXR.OpenXRLoader>();
                AssetDatabase.AddObjectToAsset(openXrLoader, manager);
                manager.TryAddLoader(openXrLoader);
                Debug.Log("Faultline: OpenXR loader added for Android.");
            }

            EnableOpenXrFeatures();
            EditorUtility.SetDirty(manager);
        }

        private static void EnableOpenXrFeatures()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                Debug.LogError("Faultline: no OpenXR settings instance for Android. " +
                               "Is com.unity.xr.openxr installed?");
                return;
            }

            RefreshFeatureDiscovery();

            foreach (var (typeName, assemblyName, required) in FeatureTypes)
            {
                var type = Type.GetType(typeName + ", " + assemblyName);
                if (type == null)
                {
                    var message = $"Faultline: OpenXR feature type '{typeName}' was not found.";
                    if (required) Debug.LogError(message);
                    else Debug.LogWarning(message + " (optional interaction profile)");
                    continue;
                }

                var feature = settings.GetFeature(type);
                if (feature == null)
                {
                    var message = required
                        ? $"Faultline: required OpenXR feature '{type.Name}' is not present in the feature list."
                        : $"Faultline: optional OpenXR feature '{type.Name}' is not installed.";
                    Debug.LogWarning(message);
                    continue;
                }

                feature.enabled = true;
                EditorUtility.SetDirty(feature);
                Debug.Log($"Faultline: enabled OpenXR feature '{type.Name}'.");
            }

            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// Unity's OpenXR package discovers feature sets through an editor-only
        /// helper. Call it through reflection so the project configuration does
        /// not hard-bind to an internal API that moves between package versions.
        /// </summary>
        private static void RefreshFeatureDiscovery()
        {
            var helpers = Type.GetType("UnityEditor.XR.OpenXR.Features.FeatureHelpers, Unity.XR.OpenXR.Editor");
            var refresh = helpers?.GetMethod("RefreshFeatures",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(BuildTargetGroup) }, null);

            if (refresh == null)
            {
                Debug.LogWarning("Faultline: FeatureHelpers.RefreshFeatures is unavailable; " +
                                 "feature discovery relies on the installed package's own list.");
                return;
            }

            refresh.Invoke(null, new object[] { BuildTargetGroup.Android });
        }

        // ------------------------------------------------------------------
        // Graphics
        // ------------------------------------------------------------------

        private static void ApplyGraphicsSettings()
        {
            // Runtime-created materials resolve their shaders through Shader.Find,
            // so the shaders must survive build stripping.
            var alwaysIncluded = new List<Shader>();
            foreach (var name in new[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit",
                "Standard",
                "UI/Default",
                "Sprites/Default",
                "Hidden/InternalErrorShader",
            })
            {
                var shader = Shader.Find(name);
                if (shader != null && !alwaysIncluded.Contains(shader))
                    alwaysIncluded.Add(shader);
            }

            GraphicsSettings.defaultAlwaysIncludedShaders = alwaysIncluded.ToArray();

            var urpAsset = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            if (urpAsset != null)
            {
                GraphicsSettings.defaultRenderPipeline = urpAsset;
                QualitySettings.renderPipeline = urpAsset;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, UnityEngine.Rendering.GraphicsAPIType.OpenGLES3);
                Debug.Log($"Faultline: render pipeline assigned ({urpAsset.name}).");
            }
            else
            {
                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, UnityEngine.Rendering.GraphicsAPIType.OpenGLES3);
                Debug.LogWarning("Faultline: no UniversalRenderPipelineAsset found — the player " +
                                 "builds on the built-in pipeline. Assign a URP asset before the " +
                                 "release candidate if the Meta SDK requires it.");
            }
        }

        private static void ApplySceneList()
        {
            if (!File.Exists(MainScenePath))
            {
                Debug.LogError($"Faultline: required scene '{MainScenePath}' does not exist. " +
                               "Run Faultline/Build Main Scene first.");
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true),
            };
        }
    }
}