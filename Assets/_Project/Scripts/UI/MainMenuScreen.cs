#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// Main menu built in code with UI Toolkit (GAME_DESIGN, "UI"; D-085): New Run with an optional seed, Continue
    /// while a saved run exists (D-074), Settings (the shared panel) and Quit. It only raises requests; the App layer
    /// decides what happens, so this screen holds no flow logic.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private const string TitleText = "DEMON FIGHTER";
        private const string SeedLabel = "Seed (optional)";
        private const string SeedNote = "Leave the seed empty for a random cavern. Any word works as a seed and gives the same cavern every time.";

        private UIDocument _document = null!;
        private VisualElement? _menuList;
        private SettingsPanel? _settings;
        private TextField? _seedField;
        private Button? _continueButton;
        private bool _continueShown;
        private SettingsValues _settingsValues = new SettingsValues();

        /// <summary>Raised when the player clicks New Run, with the seed they typed or null for a random one.</summary>
        public event Action<int?>? NewRunRequested;

        /// <summary>Raised when the player clicks Continue.</summary>
        public event Action? ContinueRequested;

        /// <summary>Raised when the player clicks Quit.</summary>
        public event Action? QuitRequested;

        /// <summary>Raised with the complete settings after any control changed.</summary>
        public event Action<SettingsValues>? SettingsChanged;

        /// <summary>Raised when the player asks to see the first-run hints again.</summary>
        public event Action? ResetHintsRequested;

        /// <summary>Shows or hides the Continue button; the App layer knows whether a save exists.</summary>
        public void ShowContinue(bool show)
        {
            _continueShown = show;
            if (_continueButton != null)
            {
                _continueButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Shows these settings in the panel.</summary>
        public void ConfigureSettings(SettingsValues values)
        {
            _settingsValues = values ?? throw new ArgumentNullException(nameof(values));
            _settings?.Configure(_settingsValues);
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            root.Clear();
            root.Add(BuildLayout());
        }

        private void OnDisable()
        {
            if (_settings != null)
            {
                _settings.Changed -= OnSettingsChanged;
                _settings.ResetHintsRequested -= OnResetHints;
                _settings.BackRequested -= OnBack;
                _settings = null;
            }

            _menuList = null;
            _seedField = null;
            _continueButton = null;
        }

        private VisualElement BuildLayout()
        {
            var container = new VisualElement { name = "main-menu" };
            container.style.flexGrow = 1f;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            container.style.backgroundColor = MenuStyles.Backdrop;

            container.Add(MenuStyles.Heading("title", TitleText, 64));

            _menuList = new VisualElement { name = "menu-list" };
            _menuList.style.alignItems = Align.Center;

            _continueButton = MenuStyles.Button("continue", "Continue", () => ContinueRequested?.Invoke(), MenuStyles.ButtonWidth, 72);
            _continueButton.style.display = _continueShown ? DisplayStyle.Flex : DisplayStyle.None;
            _menuList.Add(_continueButton);

            _menuList.Add(MenuStyles.Button("new-run", "New Run", OnNewRunClicked, MenuStyles.ButtonWidth, 72));

            _seedField = new TextField(SeedLabel) { name = "seed" };
            _seedField.style.width = MenuStyles.ButtonWidth;
            _seedField.style.fontSize = 18;
            _seedField.style.color = MenuStyles.Text;
            _seedField.style.marginBottom = 4;
            _menuList.Add(_seedField);
            Label seedNote = MenuStyles.Note("seed-note", SeedNote, MenuStyles.ButtonWidth + 80);
            seedNote.style.marginBottom = 16;
            _menuList.Add(seedNote);

            _menuList.Add(MenuStyles.Button("settings", "Settings", OnSettingsClicked, MenuStyles.ButtonWidth, 52, 22));
            _menuList.Add(MenuStyles.Button("quit", "Quit", () => QuitRequested?.Invoke(), MenuStyles.ButtonWidth, 52, 22));
            container.Add(_menuList);

            _settings = new SettingsPanel();
            _settings.Configure(_settingsValues);
            _settings.SetVisible(false);
            _settings.Changed += OnSettingsChanged;
            _settings.ResetHintsRequested += OnResetHints;
            _settings.BackRequested += OnBack;
            container.Add(_settings.Root);
            return container;
        }

        private void OnNewRunClicked()
        {
            NewRunRequested?.Invoke(SeedParser.Parse(_seedField != null ? _seedField.value : null));
        }

        private void OnSettingsClicked()
        {
            if (_menuList != null && _settings != null)
            {
                _menuList.style.display = DisplayStyle.None;
                _settings.SetVisible(true);
            }
        }

        private void OnBack()
        {
            if (_menuList != null && _settings != null)
            {
                _settings.SetVisible(false);
                _menuList.style.display = DisplayStyle.Flex;
            }
        }

        private void OnSettingsChanged(SettingsValues values)
        {
            _settingsValues = values;
            SettingsChanged?.Invoke(values);
        }

        private void OnResetHints()
        {
            ResetHintsRequested?.Invoke();
        }
    }
}
