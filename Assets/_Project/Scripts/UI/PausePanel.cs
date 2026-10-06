#nullable enable
using System;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The pause menu behind Esc (D-074, D-085): Resume, Settings (the shared panel), Save and Quit back to the main
    /// menu, and Quit to the desktop. It only raises requests; the App layer pauses, saves, applies settings and
    /// changes scenes.
    /// </summary>
    public sealed class PausePanel
    {
        private readonly VisualElement _menuList;
        private readonly SettingsPanel _settings;

        public PausePanel()
        {
            Root = MenuStyles.FullScreenOverlay("pause-panel");
            Root.Add(MenuStyles.Heading("title", "PAUSED"));

            _menuList = new VisualElement { name = "pause-list" };
            _menuList.style.alignItems = Align.Center;
            _menuList.Add(MenuStyles.Button("resume", "Resume", () => ResumeRequested?.Invoke()));
            _menuList.Add(MenuStyles.Button("pause-settings", "Settings", OnSettingsClicked));
            _menuList.Add(MenuStyles.Button("save-quit", "Save and Quit", () => SaveAndQuitRequested?.Invoke()));
            _menuList.Add(MenuStyles.Button("quit-desktop", "Quit to Desktop", () => QuitRequested?.Invoke()));
            _menuList.Add(MenuStyles.Note("hint", "Esc resumes. Save and Quit keeps this run for Continue in the main menu; quitting to the desktop saves it too. Death still ends it."));
            Root.Add(_menuList);

            _settings = new SettingsPanel();
            _settings.SetVisible(false);
            _settings.Changed += values => SettingsChanged?.Invoke(values);
            _settings.ResetHintsRequested += () => ResetHintsRequested?.Invoke();
            _settings.BackRequested += OnBack;
            Root.Add(_settings.Root);
            SetVisible(false);
        }

        /// <summary>Raised when the player clicks Resume.</summary>
        public event Action? ResumeRequested;

        /// <summary>Raised when the player clicks Save and Quit.</summary>
        public event Action? SaveAndQuitRequested;

        /// <summary>Raised when the player clicks Quit to Desktop.</summary>
        public event Action? QuitRequested;

        /// <summary>Raised with the complete settings after any control changed.</summary>
        public event Action<SettingsValues>? SettingsChanged;

        /// <summary>Raised when the player asks to see the first-run hints again.</summary>
        public event Action? ResetHintsRequested;

        /// <summary>The element to add to the HUD.</summary>
        public VisualElement Root { get; }

        /// <summary>True while the panel is shown.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Shows these settings in the panel.</summary>
        public void ConfigureSettings(SettingsValues values)
        {
            _settings.Configure(values);
        }

        /// <summary>Shows or hides the menu; it always opens on its buttons, not inside the settings.</summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                OnBack();
            }
        }

        private void OnSettingsClicked()
        {
            _menuList.style.display = DisplayStyle.None;
            _settings.SetVisible(true);
        }

        private void OnBack()
        {
            _settings.SetVisible(false);
            _menuList.style.display = DisplayStyle.Flex;
        }
    }
}
