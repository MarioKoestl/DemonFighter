#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The pause panel behind Esc (D-074): Resume, or Save and Quit, which keeps the run for Continue in the main
    /// menu. It only raises requests; the App layer pauses, saves and changes scenes. Not the full pause menu of the
    /// design, that is M5. Built in code until styling moves into USS.
    /// </summary>
    public sealed class PausePanel
    {
        private static readonly Color Background = new Color(0.03f, 0.01f, 0.01f, 0.85f);
        private static readonly Color TitleColor = new Color(0.95f, 0.3f, 0.25f);
        private static readonly Color MutedText = new Color(0.75f, 0.7f, 0.65f);

        public PausePanel()
        {
            Root = new VisualElement { name = "pause-panel" };
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.top = 0;
            Root.style.bottom = 0;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = Background;

            var title = new Label("PAUSED") { name = "title" };
            title.style.fontSize = 56;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TitleColor;
            title.style.marginBottom = 32;
            Root.Add(title);

            var resume = new Button(() => ResumeRequested?.Invoke()) { name = "resume", text = "Resume" };
            StyleButton(resume);
            Root.Add(resume);

            var saveAndQuit = new Button(() => SaveAndQuitRequested?.Invoke()) { name = "save-quit", text = "Save and Quit" };
            StyleButton(saveAndQuit);
            Root.Add(saveAndQuit);

            var hint = new Label("Esc resumes. Save and Quit keeps this run for Continue in the main menu; death still ends it.") { name = "hint" };
            hint.style.fontSize = 16;
            hint.style.color = MutedText;
            hint.style.marginTop = 24;
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.maxWidth = 520;
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            Root.Add(hint);
            SetVisible(false);
        }

        /// <summary>Raised when the player clicks Resume.</summary>
        public event Action? ResumeRequested;

        /// <summary>Raised when the player clicks Save and Quit.</summary>
        public event Action? SaveAndQuitRequested;

        /// <summary>The element to add to the HUD.</summary>
        public VisualElement Root { get; }

        /// <summary>True while the panel is shown.</summary>
        public bool IsVisible { get; private set; }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void StyleButton(Button button)
        {
            button.style.fontSize = 28;
            button.style.width = 320;
            button.style.height = 64;
            button.style.marginBottom = 12;
        }
    }
}
