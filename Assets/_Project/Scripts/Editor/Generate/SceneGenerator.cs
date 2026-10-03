#nullable enable
using System.IO;
using DemonFighter.App;
using DemonFighter.Common;
using DemonFighter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Creates the three scenes from code and sets the build list, so scene files are never hand-edited YAML
    /// (CLAUDE.md rule 4). Re-running overwrites the scene files; anything placed in them by hand is lost on purpose.
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

        private static string CreateBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapper = new GameObject(nameof(Bootstrapper));
            bootstrapper.AddComponent<Bootstrapper>();
            return Save(scene, SceneNames.Bootstrap);
        }

        private static string CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(MenuBackground);

            // Loaded after the scene switch on purpose: closing every scene can unload an asset that nothing
            // references yet, and a reference held across that point serializes as null.
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                throw new IOException("Panel settings asset is missing: " + PanelSettingsPath);
            }

            var menu = new GameObject(nameof(MainMenuScreen));
            UIDocument document = menu.AddComponent<UIDocument>();
            AssignPanelSettings(document, panelSettings);
            menu.AddComponent<MainMenuScreen>();
            return Save(scene, SceneNames.MainMenu);
        }

        // Written through the serialized property, as the Inspector does, so no edit-mode panel gets attached.
        private static void AssignPanelSettings(UIDocument document, PanelSettings panelSettings)
        {
            using var serialized = new SerializedObject(document);
            SerializedProperty? property = serialized.FindProperty(PanelSettingsProperty);
            if (property == null)
            {
                throw new IOException("UIDocument has no serialized property named " + PanelSettingsProperty);
            }

            property.objectReferenceValue = panelSettings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string CreateRunScene()
        {
            // Camera and directional light only; the world builder fills the scene at run start (M1).
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            return Save(scene, SceneNames.Run);
        }

        private static void CreateCamera(Color background)
        {
            var cameraObject = new GameObject("Main Camera") { tag = MainCameraTag };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            cameraObject.AddComponent<AudioListener>();
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
