#nullable enable
namespace DemonFighter.App
{
    /// <summary>
    /// What the settings file holds (D-085): graphics, screen, audio, mouse and the playtest toggle, as plain values
    /// Newtonsoft reads and writes. Zero width or height means the screen stays as it is. The empty preset name means
    /// the default preset of the catalog.
    /// </summary>
    internal sealed class SettingsData
    {
        public string GraphicsPreset { get; set; } = string.Empty;

        public int Width { get; set; }

        public int Height { get; set; }

        public bool Fullscreen { get; set; } = true;

        public bool VSync { get; set; } = true;

        public float Master { get; set; } = 0.8f;

        public float Effects { get; set; } = 1f;

        public float Ambient { get; set; } = 1f;

        public float Music { get; set; } = 0.7f;

        /// <summary>Multiplier on the tuned mouse look, 1 being the default.</summary>
        public float MouseSensitivity { get; set; } = 1f;

        public bool InvertY { get; set; }

        /// <summary>The playtest toggle of O-001: a random hand of three instead of the whole shop.</summary>
        public bool RandomOffers { get; set; }

        /// <summary>Test mode (D-089): the player takes no damage and keeps 1000 Biomass and stat points.</summary>
        public bool TestMode { get; set; }
    }
}
