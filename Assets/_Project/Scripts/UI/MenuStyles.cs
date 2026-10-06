#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The look the menus share (D-085): dark backdrop, blood-red titles, muted notes and one button shape, set in code
    /// so the main menu, the pause menu and the settings panel read as one family. Styling in code until the menus
    /// move into USS.
    /// </summary>
    public static class MenuStyles
    {
        public static readonly Color Backdrop = new Color(0.05f, 0.02f, 0.02f);
        public static readonly Color Overlay = new Color(0.03f, 0.01f, 0.01f, 0.88f);
        public static readonly Color Title = new Color(0.8f, 0.1f, 0.1f);
        public static readonly Color Accent = new Color(0.95f, 0.3f, 0.25f);
        public static readonly Color Text = new Color(0.9f, 0.86f, 0.8f);
        public static readonly Color Muted = new Color(0.75f, 0.7f, 0.65f);
        public static readonly Color Panel = new Color(0.1f, 0.05f, 0.05f, 0.95f);

        public const int ButtonWidth = 320;
        public const int WideButtonWidth = 420;
        public const int ButtonHeight = 64;
        public const int SmallButtonHeight = 48;

        /// <summary>A menu button with the shared shape; the name is what tests and styles find it by.</summary>
        public static Button Button(string name, string text, Action onClick, int width = ButtonWidth, int height = ButtonHeight, int fontSize = 28)
        {
            var button = new Button(onClick) { name = name, text = text };
            button.style.fontSize = fontSize;
            button.style.width = width;
            button.style.height = height;
            button.style.marginBottom = 12;
            return button;
        }

        /// <summary>A big red title.</summary>
        public static Label Heading(string name, string text, int fontSize = 56)
        {
            var title = new Label(text) { name = name };
            title.style.fontSize = fontSize;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Title;
            title.style.marginBottom = 32;
            return title;
        }

        /// <summary>A quiet line of explanation.</summary>
        public static Label Note(string name, string text, int maxWidth = 520)
        {
            var note = new Label(text) { name = name };
            note.style.fontSize = 16;
            note.style.color = Muted;
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.maxWidth = maxWidth;
            note.style.unityTextAlign = TextAnchor.MiddleCenter;
            note.style.marginTop = 8;
            return note;
        }

        /// <summary>A full-screen overlay that centers its children, for the pause menu and the death screen.</summary>
        public static VisualElement FullScreenOverlay(string name)
        {
            var root = new VisualElement { name = name };
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.right = 0;
            root.style.top = 0;
            root.style.bottom = 0;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.style.backgroundColor = Overlay;
            return root;
        }

        /// <summary>A column of controls with a label in front of each.</summary>
        public static VisualElement Column(string name)
        {
            var column = new VisualElement { name = name };
            column.style.alignItems = Align.Stretch;
            column.style.width = 560;
            return column;
        }
    }
}
