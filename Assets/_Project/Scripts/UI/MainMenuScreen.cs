#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// Main menu built in code with UI Toolkit: New Run, Continue while a saved run exists (D-074), and a small
    /// Settings box with the random offers toggle and the hint reset (D-076). It only raises requests; the App layer
    /// decides what happens, so this screen holds no flow logic. Styling here is a placeholder until the real menus of
    /// M5 move it into USS.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private const string TitleText = "DEMON FIGHTER";
        private const string NewRunText = "New Run";
        private const string ContinueText = "Continue";
        private const string SettingsText = "Settings";
        private const string ShopOffersText = "Offers: the whole shop";
        private const string RandomOffersText = "Offers: a random hand of three";
        private const string ResetHintsText = "Show first-run hints again";

        private static readonly Color MutedText = new Color(0.75f, 0.7f, 0.65f);

        private UIDocument _document = null!;
        private Button? _newRunButton;
        private Button? _continueButton;
        private Button? _settingsButton;
        private Button? _randomOffersButton;
        private Button? _resetHintsButton;
        private VisualElement? _settingsBox;
        private bool _continueShown;
        private bool _randomOffers;

        /// <summary>Raised when the player clicks New Run.</summary>
        public event Action? NewRunRequested;

        /// <summary>Raised when the player clicks Continue.</summary>
        public event Action? ContinueRequested;

        /// <summary>Raised when the player flips the offers setting; carries the new value.</summary>
        public event Action<bool>? RandomOffersToggled;

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

        /// <summary>Shows the current offers setting on its toggle.</summary>
        public void ShowSettings(bool randomOffers)
        {
            _randomOffers = randomOffers;
            if (_randomOffersButton != null)
            {
                _randomOffersButton.text = randomOffers ? RandomOffersText : ShopOffersText;
            }
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
            Unhook(ref _newRunButton, OnNewRunClicked);
            Unhook(ref _continueButton, OnContinueClicked);
            Unhook(ref _settingsButton, OnSettingsClicked);
            Unhook(ref _randomOffersButton, OnRandomOffersClicked);
            Unhook(ref _resetHintsButton, OnResetHintsClicked);
            _settingsBox = null;
        }

        private VisualElement BuildLayout()
        {
            var container = new VisualElement { name = "main-menu" };
            container.style.flexGrow = 1f;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            container.style.backgroundColor = new Color(0.05f, 0.02f, 0.02f);

            var title = new Label(TitleText) { name = "title" };
            title.style.fontSize = 64;
            title.style.color = new Color(0.8f, 0.1f, 0.1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 48;

            _continueButton = new Button { name = "continue", text = ContinueText };
            StyleButton(_continueButton, 320);
            _continueButton.style.display = _continueShown ? DisplayStyle.Flex : DisplayStyle.None;
            _continueButton.clicked += OnContinueClicked;

            _newRunButton = new Button { name = "new-run", text = NewRunText };
            StyleButton(_newRunButton, 320);
            _newRunButton.clicked += OnNewRunClicked;

            _settingsButton = new Button { name = "settings", text = SettingsText };
            StyleButton(_settingsButton, 320);
            _settingsButton.style.fontSize = 22;
            _settingsButton.style.height = 52;
            _settingsButton.clicked += OnSettingsClicked;

            _settingsBox = new VisualElement { name = "settings-box" };
            _settingsBox.style.display = DisplayStyle.None;
            _settingsBox.style.alignItems = Align.Center;
            _settingsBox.style.marginTop = 16;
            var note = new Label("Playtest settings; the real settings menu comes later.") { name = "settings-note" };
            note.style.fontSize = 14;
            note.style.color = MutedText;
            note.style.marginBottom = 8;
            _settingsBox.Add(note);
            _randomOffersButton = new Button { name = "random-offers", text = _randomOffers ? RandomOffersText : ShopOffersText };
            StyleButton(_randomOffersButton, 420);
            _randomOffersButton.style.fontSize = 20;
            _randomOffersButton.style.height = 48;
            _randomOffersButton.clicked += OnRandomOffersClicked;
            _settingsBox.Add(_randomOffersButton);
            _resetHintsButton = new Button { name = "reset-hints", text = ResetHintsText };
            StyleButton(_resetHintsButton, 420);
            _resetHintsButton.style.fontSize = 20;
            _resetHintsButton.style.height = 48;
            _resetHintsButton.clicked += OnResetHintsClicked;
            _settingsBox.Add(_resetHintsButton);

            container.Add(title);
            container.Add(_continueButton);
            container.Add(_newRunButton);
            container.Add(_settingsButton);
            container.Add(_settingsBox);
            return container;
        }

        private static void StyleButton(Button button, int width)
        {
            button.style.fontSize = 28;
            button.style.width = width;
            button.style.height = 72;
            button.style.marginBottom = 12;
        }

        private static void Unhook(ref Button? button, Action handler)
        {
            if (button != null)
            {
                button.clicked -= handler;
                button = null;
            }
        }

        private void OnNewRunClicked()
        {
            NewRunRequested?.Invoke();
        }

        private void OnContinueClicked()
        {
            ContinueRequested?.Invoke();
        }

        private void OnSettingsClicked()
        {
            if (_settingsBox != null)
            {
                bool open = _settingsBox.style.display == DisplayStyle.None;
                _settingsBox.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void OnRandomOffersClicked()
        {
            _randomOffers = !_randomOffers;
            ShowSettings(_randomOffers);
            RandomOffersToggled?.Invoke(_randomOffers);
        }

        private void OnResetHintsClicked()
        {
            ResetHintsRequested?.Invoke();
        }
    }
}
