#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonFighter.UI
{
    /// <summary>
    /// Everything the settings panel shows and edits (GAME_DESIGN, "UI"; D-085), as plain values the App layer fills
    /// from its settings file and reads back when a control changes. The UI never knows where the values live.
    /// </summary>
    public sealed class SettingsValues
    {
        public const float MinSensitivity = 0.2f;
        public const float MaxSensitivity = 3f;

        /// <summary>Names of the graphics presets, lowest first.</summary>
        public IReadOnlyList<string> PresetNames { get; set; } = Array.Empty<string>();

        public int PresetIndex { get; set; }

        /// <summary>Resolutions as "1920 x 1080" labels, the current one included.</summary>
        public IReadOnlyList<string> Resolutions { get; set; } = Array.Empty<string>();

        public int ResolutionIndex { get; set; }

        public bool Fullscreen { get; set; } = true;

        public bool VSync { get; set; } = true;

        public float Master { get; set; } = 0.8f;

        public float Effects { get; set; } = 1f;

        public float Ambient { get; set; } = 1f;

        public float Music { get; set; } = 0.7f;

        /// <summary>Multiplier on the mouse look of the camera settings, 1 being the tuned default.</summary>
        public float MouseSensitivity { get; set; } = 1f;

        public bool InvertY { get; set; }

        /// <summary>The playtest toggle of O-001: a random hand of three instead of the whole shop.</summary>
        public bool RandomOffers { get; set; }

        /// <summary>Test mode (D-089): the player takes no damage and keeps 1000 Biomass and stat points.</summary>
        public bool TestMode { get; set; }

        /// <summary>A copy with every value inside its range and every index inside its list.</summary>
        public SettingsValues Clamped()
        {
            return new SettingsValues
            {
                PresetNames = PresetNames,
                PresetIndex = ClampIndex(PresetIndex, PresetNames.Count),
                Resolutions = Resolutions,
                ResolutionIndex = ClampIndex(ResolutionIndex, Resolutions.Count),
                Fullscreen = Fullscreen,
                VSync = VSync,
                Master = Mathf.Clamp01(Master),
                Effects = Mathf.Clamp01(Effects),
                Ambient = Mathf.Clamp01(Ambient),
                Music = Mathf.Clamp01(Music),
                MouseSensitivity = Mathf.Clamp(MouseSensitivity, MinSensitivity, MaxSensitivity),
                InvertY = InvertY,
                RandomOffers = RandomOffers,
                TestMode = TestMode,
            };
        }

        /// <summary>A copy with the same values; the panel edits a copy and hands it on.</summary>
        public SettingsValues Copy()
        {
            return new SettingsValues
            {
                PresetNames = PresetNames,
                PresetIndex = PresetIndex,
                Resolutions = Resolutions,
                ResolutionIndex = ResolutionIndex,
                Fullscreen = Fullscreen,
                VSync = VSync,
                Master = Master,
                Effects = Effects,
                Ambient = Ambient,
                Music = Music,
                MouseSensitivity = MouseSensitivity,
                InvertY = InvertY,
                RandomOffers = RandomOffers,
                TestMode = TestMode,
            };
        }

        private static int ClampIndex(int index, int count)
        {
            return count == 0 ? 0 : Mathf.Clamp(index, 0, count - 1);
        }
    }
}
