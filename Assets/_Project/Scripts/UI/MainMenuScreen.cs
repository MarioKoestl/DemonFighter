#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// Main menu built in code with UI Toolkit: one New Run button for M0. It only raises
    /// <see cref="NewRunRequested"/>; the App layer decides what happens, so this screen holds no flow logic.
    /// Styling here is a placeholder until the real menus of M5 move it into USS.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private const string TitleText = "DEMON FIGHTER";
        private const string NewRunText = "New Run";

        private UIDocument _document = null!;
        private Button? _newRunButton;

        /// <summary>Raised when the player clicks New Run.</summary>
        public event Action? NewRunRequested;

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
            if (_newRunButton != null)
            {
                _newRunButton.clicked -= OnNewRunClicked;
                _newRunButton = null;
            }
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

            _newRunButton = new Button { name = "new-run", text = NewRunText };
            _newRunButton.style.fontSize = 28;
            _newRunButton.style.width = 320;
            _newRunButton.style.height = 72;
            _newRunButton.clicked += OnNewRunClicked;

            container.Add(title);
            container.Add(_newRunButton);
            return container;
        }

        private void OnNewRunClicked()
        {
            NewRunRequested?.Invoke();
        }
    }
}
