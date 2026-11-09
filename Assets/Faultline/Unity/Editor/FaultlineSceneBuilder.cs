namespace Faultline.Editor
{
    using System;
    using System.IO;

    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.XR.CoreUtils;

    using Faultline.Runtime;
    using Faultline.Runtime.UI;

    /// <summary>
    /// Builds the single runtime scene deterministically: an XR Origin with the
    /// tracked camera, the seated environment, and the FAULTLINE application
    /// root. Generating the scene from code keeps it reproducible from a clean
    /// clone instead of depending on hand-edited binary scene state.
    /// </summary>
    public static class FaultlineSceneBuilder
    {
        private const string SettingsFolder = "Assets/Faultline/Settings";
        private const string SceneFolder = "Assets/Faultline/Scenes";

        [MenuItem("Faultline/Build Main Scene")]
        public static void BuildScene()
        {
            EnsureFolder(SceneFolder);
            EnsureFolder(SettingsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var xrOrigin = CreateXrOrigin();
            CreateEnvironment(xrOrigin.transform);
            CreateApplicationRoot();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, FaultlineProjectConfigurer.MainScenePath);
            FaultlineProjectConfigurer.Configure();

            Debug.Log($"Faultline: scene written to {FaultlineProjectConfigurer.MainScenePath}");
        }

        private static XROrigin CreateXrOrigin()
        {
            var originObject = new GameObject("XR Origin");
            var origin = originObject.AddComponent<XROrigin>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(originObject.transform, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FaultlinePalette.Void;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60f;
            camera.allowDynamicResolution = true;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;

            origin.Camera = camera;
            origin.OriginType = OriginTypes.ReferencePoint;
            origin.UsePhysicalCameras = true;
            return origin;
        }

        private static void CreateEnvironment(Transform origin)
        {
            var environment = new GameObject("Environment");
            environment.transform.SetParent(null, false);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Seated Floor Reference";
            floor.transform.SetParent(environment.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            var floorRenderer = floor.GetComponent<MeshRenderer>();
            floorRenderer.sharedMaterial = PaletteMaterial(FaultlinePalette.Void * 0.7f, 0.05f);

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            backdrop.name = "Backdrop";
            backdrop.transform.SetParent(environment.transform, false);
            backdrop.transform.localPosition = new Vector3(0f, 1.1f, -6f);
            backdrop.transform.localScale = Vector3.one * 14f;
            var backdropRenderer = backdrop.GetComponent<MeshRenderer>();
            backdropRenderer.sharedMaterial = PaletteMaterial(FaultlinePalette.Obsidian, 0.08f);
            backdropRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.Destroy(backdrop.GetComponent<Collider>());

            var light = new GameObject("Key Light");
            light.transform.SetParent(environment.transform, false);
            light.transform.localPosition = new Vector3(0f, 2.4f, -1.2f);
            light.transform.localRotation = Quaternion.Euler(58f, -18f, 0f);
            var directional = light.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = Color.white;
            directional.intensity = 0.85f;
            directional.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = FaultlinePalette.Graphite * 0.6f;

            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void CreateApplicationRoot()
        {
            var root = new GameObject("Faultline");
            var app = root.AddComponent<FaultlineUnityApp>();

            var panelCount = ScreenRegistry.Count;
            if (panelCount != FaultlineRuntimeScreenInventory.ExpectedScreenCount)
                Debug.LogError($"Faultline: screen registry lists {panelCount} screens but the " +
                               $"contract expects {FaultlineRuntimeScreenInventory.ExpectedScreenCount}.");
            _ = app;
        }

        private static Material PaletteMaterial(Color colour, float emission)
        {
            var assetPath = Path.Combine(SettingsFolder,
                $"FaultlineMaterial_{Mathf.RoundToInt(colour.r * 255)}_{Mathf.RoundToInt(colour.g * 255)}_" +
                $"{Mathf.RoundToInt(colour.b * 255)}_{Mathf.RoundToInt(emission * 100)}.mat");

            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("Faultline: no lit shader available to build scene materials.");

            var material = new Material(shader) { name = Path.GetFileNameWithoutExtension(assetPath), color = colour };
            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colour * emission);
            }

            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    /// <summary>
    /// The PRD §6.1 contract count, kept separate from the registry so a screen
    /// dropped from the registry fails the build loudly instead of silently.
    /// </summary>
    public static class FaultlineRuntimeScreenInventory
    {
        internal const int ExpectedScreenCount = 23;
    }
}