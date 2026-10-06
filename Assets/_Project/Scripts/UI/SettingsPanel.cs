#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The settings panel the main menu and the pause menu share (GAME_DESIGN, "UI"; D-085): tabs for Graphics
    /// (preset, resolution, fullscreen, vsync), Audio (four volumes), Controls (mouse sensitivity, invert Y, the key
    /// list) and Playtest (the offers toggle, the hint reset). Every change raises the whole value set at once, so
    /// the App layer applies and saves it; the panel itself remembers nothing.
    /// </summary>
    public sealed class SettingsPanel
    {
        private const string GraphicsTab = "Graphics";
        private const string AudioTab = "Audio";
        private const string ControlsTab = "Controls";
        private const string PlaytestTab = "Playtest";
        private const string KeysText = "Keyboard and mouse. WASD moves, Shift sprints, the mouse looks. Left mouse bites, right mouse claws or grabs, Space lunges, Q swings the tail, E eats. Tab opens mutations, C the stats, V switches the camera, F analyzes the target, Esc pauses.";
        private static readonly string[] Tabs = { GraphicsTab, AudioTab, ControlsTab, PlaytestTab };

        private readonly Dictionary<string, VisualElement> _pages = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private readonly Dictionary<string, Button> _tabButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        private readonly DropdownField _preset;
        private readonly DropdownField _resolution;
        private readonly Toggle _fullscreen;
        private readonly Toggle _vsync;
        private readonly Slider _master;
        private readonly Slider _effects;
        private readonly Slider _ambient;
        private readonly Slider _music;
        private readonly Slider _sensitivity;
        private readonly Toggle _invertY;
        private readonly Toggle _randomOffers;
        private readonly Toggle _testMode;
        private SettingsValues _values = new SettingsValues();
        private bool _configuring;

        public SettingsPanel()
        {
            Root = new VisualElement { name = "settings-panel" };
            Root.style.alignItems = Align.Center;
            Root.style.backgroundColor = MenuStyles.Panel;
            Root.style.paddingTop = 24;
            Root.style.paddingBottom = 24;
            Root.style.paddingLeft = 32;
            Root.style.paddingRight = 32;
            Root.Add(MenuStyles.Heading("settings-title", "SETTINGS", 40));

            var tabBar = new VisualElement { name = "settings-tabs" };
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.marginBottom = 16;
            foreach (string tab in Tabs)
            {
                string captured = tab;
                Button tabButton = MenuStyles.Button("settings-tab-" + tab, tab, () => ShowTab(captured), 130, MenuStyles.SmallButtonHeight, 20);
                tabButton.style.marginRight = 6;
                tabBar.Add(tabButton);
                _tabButtons[tab] = tabButton;
            }

            Root.Add(tabBar);

            VisualElement graphics = Page(GraphicsTab);
            _preset = Dropdown("preset", "Quality preset", graphics);
            _preset.RegisterValueChangedCallback(evt => OnChanged(v => v.PresetIndex = Math.Max(0, _preset.choices.IndexOf(evt.newValue))));
            _resolution = Dropdown("resolution", "Resolution", graphics);
            _resolution.RegisterValueChangedCallback(evt => OnChanged(v => v.ResolutionIndex = Math.Max(0, _resolution.choices.IndexOf(evt.newValue))));
            _fullscreen = ToggleRow("fullscreen", "Fullscreen", graphics);
            _fullscreen.RegisterValueChangedCallback(evt => OnChanged(v => v.Fullscreen = evt.newValue));
            _vsync = ToggleRow("vsync", "VSync", graphics);
            _vsync.RegisterValueChangedCallback(evt => OnChanged(v => v.VSync = evt.newValue));

            VisualElement audio = Page(AudioTab);
            _master = SliderRow("master", "Master", 0f, 1f, audio);
            _master.RegisterValueChangedCallback(evt => OnChanged(v => v.Master = evt.newValue));
            _effects = SliderRow("effects", "Effects", 0f, 1f, audio);
            _effects.RegisterValueChangedCallback(evt => OnChanged(v => v.Effects = evt.newValue));
            _ambient = SliderRow("ambient", "Ambient", 0f, 1f, audio);
            _ambient.RegisterValueChangedCallback(evt => OnChanged(v => v.Ambient = evt.newValue));
            _music = SliderRow("music", "Music", 0f, 1f, audio);
            _music.RegisterValueChangedCallback(evt => OnChanged(v => v.Music = evt.newValue));

            VisualElement controls = Page(ControlsTab);
            _sensitivity = SliderRow("sensitivity", "Mouse sensitivity", SettingsValues.MinSensitivity, SettingsValues.MaxSensitivity, controls);
            _sensitivity.RegisterValueChangedCallback(evt => OnChanged(v => v.MouseSensitivity = evt.newValue));
            _invertY = ToggleRow("invert-y", "Invert Y", controls);
            _invertY.RegisterValueChangedCallback(evt => OnChanged(v => v.InvertY = evt.newValue));
            controls.Add(MenuStyles.Note("keys", KeysText, 560));

            VisualElement playtest = Page(PlaytestTab);
            _randomOffers = ToggleRow("random-offers", "Mutation offers: a random hand of three instead of the whole shop", playtest);
            _randomOffers.RegisterValueChangedCallback(evt => OnChanged(v => v.RandomOffers = evt.newValue));
            _testMode = ToggleRow("test-mode", "Test mode: invincible, Biomass and stat points kept at 1000", playtest);
            _testMode.RegisterValueChangedCallback(evt => OnChanged(v => v.TestMode = evt.newValue));
            playtest.Add(MenuStyles.Button("reset-hints", "Show first-run hints again", () => ResetHintsRequested?.Invoke(), MenuStyles.WideButtonWidth, MenuStyles.SmallButtonHeight, 20));

            Root.Add(MenuStyles.Button("back", "Back", () => BackRequested?.Invoke(), MenuStyles.ButtonWidth, MenuStyles.SmallButtonHeight, 22));
            ShowTab(GraphicsTab);
        }

        /// <summary>Raised with the complete value set after any control changed.</summary>
        public event Action<SettingsValues>? Changed;

        /// <summary>Raised when the player asks to see the first-run hints again.</summary>
        public event Action? ResetHintsRequested;

        /// <summary>Raised when the player leaves the panel.</summary>
        public event Action? BackRequested;

        /// <summary>The element to add to a menu.</summary>
        public VisualElement Root { get; }

        /// <summary>Shows these values in the controls without raising a change.</summary>
        public void Configure(SettingsValues values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _values = values.Clamped();
            _configuring = true;
            try
            {
                _preset.choices = new List<string>(_values.PresetNames);
                _preset.SetValueWithoutNotify(ChoiceAt(_preset.choices, _values.PresetIndex));
                _resolution.choices = new List<string>(_values.Resolutions);
                _resolution.SetValueWithoutNotify(ChoiceAt(_resolution.choices, _values.ResolutionIndex));
                _fullscreen.SetValueWithoutNotify(_values.Fullscreen);
                _vsync.SetValueWithoutNotify(_values.VSync);
                _master.SetValueWithoutNotify(_values.Master);
                _effects.SetValueWithoutNotify(_values.Effects);
                _ambient.SetValueWithoutNotify(_values.Ambient);
                _music.SetValueWithoutNotify(_values.Music);
                _sensitivity.SetValueWithoutNotify(_values.MouseSensitivity);
                _invertY.SetValueWithoutNotify(_values.InvertY);
                _randomOffers.SetValueWithoutNotify(_values.RandomOffers);
                _testMode.SetValueWithoutNotify(_values.TestMode);
            }
            finally
            {
                _configuring = false;
            }
        }

        /// <summary>Shows or hides the panel; a shown panel opens on its first tab.</summary>
        public void SetVisible(bool visible)
        {
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                ShowTab(GraphicsTab);
            }
        }

        private void ShowTab(string tab)
        {
            foreach (KeyValuePair<string, VisualElement> page in _pages)
            {
                bool active = string.Equals(page.Key, tab, StringComparison.Ordinal);
                page.Value.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                _tabButtons[page.Key].style.color = active ? MenuStyles.Accent : MenuStyles.Text;
            }
        }

        private void OnChanged(Action<SettingsValues> change)
        {
            if (_configuring)
            {
                return;
            }

            SettingsValues next = _values.Copy();
            change(next);
            _values = next.Clamped();
            Changed?.Invoke(_values);
        }

        private VisualElement Page(string tab)
        {
            VisualElement page = MenuStyles.Column("settings-page-" + tab);
            page.style.display = DisplayStyle.None;
            _pages[tab] = page;
            Root.Add(page);
            return page;
        }

        private static DropdownField Dropdown(string name, string label, VisualElement page)
        {
            var dropdown = new DropdownField(label) { name = name };
            StyleRow(dropdown);
            page.Add(dropdown);
            return dropdown;
        }

        private static Toggle ToggleRow(string name, string label, VisualElement page)
        {
            var toggle = new Toggle(label) { name = name };
            StyleRow(toggle);
            page.Add(toggle);
            return toggle;
        }

        private static Slider SliderRow(string name, string label, float low, float high, VisualElement page)
        {
            var slider = new Slider(label, low, high) { name = name, showInputField = true };
            StyleRow(slider);
            page.Add(slider);
            return slider;
        }

        private static void StyleRow(VisualElement row)
        {
            row.style.fontSize = 18;
            row.style.marginBottom = 10;
            row.style.color = MenuStyles.Text;
        }

        private static string ChoiceAt(List<string> choices, int index)
        {
            return choices.Count == 0 ? string.Empty : choices[Mathf.Clamp(index, 0, choices.Count - 1)];
        }
    }
}
