#nullable enable
using System.IO;
using DemonFighter.App;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Setup;
using DemonFighter.Presentation;
using DemonFighter.Presentation.Cameras;
using DemonFighter.Presentation.Demons;
using DemonFighter.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Creates the three scenes from code and sets the build list, so scene files are never hand-edited YAML
    /// (CLAUDE.md rule 4). Needs the placeholder assets, so run Generate > Placeholder Assets first. Re-running
    /// overwrites the scene files; anything placed in them by hand is lost on purpose.
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class SceneGenerator
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string UiSettingsFolder = "Assets/_Project/Settings/UI";
        private const string ThemePath = UiSettingsFolder + "/DemonFighterRuntimeTheme.tss";
        private const string PanelSettingsPath = UiSettingsFolder + "/DemonFighterPanelSettings.asset";
        private const string MainCameraTag = "MainCamera";
        private const string PanelSettingsProperty = "m_PanelSettings";

        // Same content Unity writes for its own default runtime theme; the Button and Label styles come from it.
        private const string ThemeContent = "@import url(\"unity-theme://default\");\nVisualElement {}\n";

        private static readonly Color MenuBackground = new Color(0.03f, 0.01f, 0.01f);
        private static readonly Color RunBackground = new Color(0.12f, 0.06f, 0.05f);
        private static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);

        [MenuItem("Demon Fighter/Generate/Scenes")]
        public static void Generate()
        {
            EnsureFolder(ScenesFolder);
            EnsureFolder(UiSettingsFolder);
            EnsurePanelSettingsAsset();

            string bootstrapPath = CreateBootstrapScene();
            string mainMenuPath = CreateMainMenuScene();
            string runPath = CreateRunScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(bootstrapPath, true),
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(runPath, true),
            };
            AssetDatabase.SaveAssets();
            Log.Info(LogCategory.Editor, "Generated scenes and build list: Bootstrap, MainMenu, Run.");
        }

        // Assets are loaded after each scene switch on purpose: closing every scene can unload an asset that nothing
        // references yet, and a reference held across that point serializes as null.
        private static string CreateBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapper = new GameObject(nameof(Bootstrapper)).AddComponent<Bootstrapper>();
            SetReference(bootstrapper, "_biome", LoadRequired<BiomeDefinition>(PlaceholderAssetGenerator.AshCavernPath));
            SetReference(bootstrapper, "_actions", LoadRequired<InputActionAsset>(ProjectWideInputActions.AssetPath));
            return Save(scene, SceneNames.Bootstrap);
        }

        private static string CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(MenuBackground);

            var menu = new GameObject(nameof(MainMenuScreen));
            UIDocument document = menu.AddComponent<UIDocument>();
            SetReference(document, PanelSettingsProperty, LoadRequired<PanelSettings>(PanelSettingsPath));
            menu.AddComponent<MainMenuScreen>();
            return Save(scene, SceneNames.MainMenu);
        }

        // Camera with brain, the rig with its two Cinemachine cameras, the HUD, and the root that hands the run
        // controller every reference. World, lights and demons are built at run start.
        private static string CreateRunScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = CreateCamera(RunBackground);
            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();

            var rigObject = new GameObject("Camera Rig");
            CameraRig rig = rigObject.AddComponent<CameraRig>();
            GameObject thirdPersonObject = Child("Third Person Camera", rigObject.transform);
            CinemachineCamera thirdPerson = thirdPersonObject.AddComponent<CinemachineCamera>();
            CinemachineOrbitalFollow orbit = thirdPersonObject.AddComponent<CinemachineOrbitalFollow>();
            CinemachineRotationComposer composer = thirdPersonObject.AddComponent<CinemachineRotationComposer>();
            GameObject firstPersonObject = Child("First Person Camera", rigObject.transform);
            CinemachineCamera firstPerson = firstPersonObject.AddComponent<CinemachineCamera>();
            SetReference(rig, "_brain", brain);
            SetReference(rig, "_thirdPerson", thirdPerson);
            SetReference(rig, "_orbit", orbit);
            SetReference(rig, "_composer", composer);
            SetReference(rig, "_firstPerson", firstPerson);

            var hudObject = new GameObject("HUD");
            UIDocument hudDocument = hudObject.AddComponent<UIDocument>();
            SetReference(hudDocument, PanelSettingsProperty, LoadRequired<PanelSettings>(PanelSettingsPath));
            HudScreen hud = hudObject.AddComponent<HudScreen>();

            var rootObject = new GameObject("Run Scene Root");
            RunSceneRoot root = rootObject.AddComponent<RunSceneRoot>();
            SetReference(root, "_palette", LoadRequired<PlaceholderPalette>(PlaceholderAssetGenerator.PalettePath));
            SetReference(root, "_worldSettings", LoadRequired<WorldBuildSettings>(PlaceholderAssetGenerator.WorldBuildSettingsPath));
            SetReference(root, "_demonSettings", LoadRequired<DemonViewSettings>(PlaceholderAssetGenerator.DemonViewSettingsPath));
            SetReference(root, "_cameraSettings", LoadRequired<CameraRigSettings>(PlaceholderAssetGenerator.CameraRigSettingsPath));
            SetReference(root, "_demonPrefab", LoadRequired<GameObject>(PlaceholderAssetGenerator.DemonPrefabPath).GetComponent<DemonView>());
            SetReference(root, "_cameraRig", rig);
            SetReference(root, "_hud", hud);
            return Save(scene, SceneNames.Run);
        }

        private static GameObject CreateCamera(Color background)
        {
            var cameraObject = new GameObject("Main Camera") { tag = MainCameraTag };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            cameraObject.AddComponent<AudioListener>();
            return cameraObject;
        }

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        // Written through the serialized property, as the Inspector does, so no edit-mode side effects run.
        private static void SetReference(Object target, string propertyName, Object value)
        {
            using var serialized = new SerializedObject(target);
            SerializedProperty? property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new IOException(target.GetType().Name + " has no serialized property named " + propertyName + ".");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadRequired<T>(string path)
            where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new IOException("Missing asset " + path + "; run Demon Fighter > Generate > Placeholder Assets first.");
            }

            return asset;
        }

        private static string Save(Scene scene, string sceneName)
        {
            string path = ScenesFolder + "/" + sceneName + ".unity";
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException("Could not save scene " + path);
            }

            return path;
        }

        private static void EnsurePanelSettingsAsset()
        {
            if (!File.Exists(ThemePath))
            {
                File.WriteAllText(ThemePath, ThemeContent);
                AssetDatabase.ImportAsset(ThemePath);
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                throw new IOException("Theme style sheet did not import: " + ThemePath);
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = ReferenceResolution;
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            panelSettings.themeStyleSheet = theme;
            EditorUtility.SetDirty(panelSettings);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
