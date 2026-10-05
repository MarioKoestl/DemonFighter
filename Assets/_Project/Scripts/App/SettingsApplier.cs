#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Presentation.Audio;
using DemonFighter.Presentation.Rendering;
using DemonFighter.UI;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// Pushes the settings file into the systems that act on it (D-085): the graphics preset into the pipeline, the
    /// resolution and vsync into the screen, the volumes into the audio mix. Builds the value set the settings panel
    /// shows and reads the panel's changes back into the file. Mouse look is read by the run controller through
    /// <see cref="LookSensitivity"/> and <see cref="InvertY"/>.
    /// </summary>
    internal sealed class SettingsApplier
    {
        private readonly GameSettings _settings;
        private readonly GraphicsPresetCatalog _presets;
        private readonly AudioMix _mix;

        public SettingsApplier(GameSettings settings, GraphicsPresetCatalog presets, AudioMix mix)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _presets = presets != null ? presets : throw new ArgumentNullException(nameof(presets));
            _mix = mix ?? throw new ArgumentNullException(nameof(mix));
        }

        /// <summary>Multiplier on the tuned mouse look.</summary>
        public float LookSensitivity => _settings.Data.MouseSensitivity;

        public bool InvertY => _settings.Data.InvertY;

        /// <summary>Applies everything in the file; called at boot and after every change.</summary>
        public void ApplyAll()
        {
            SettingsData data = _settings.Data;
            try
            {
                GraphicsQuality.Apply(_presets.Find(data.GraphicsPreset));
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Applying the graphics preset failed.", exception);
            }

            ApplyScreen(data);
            _mix.Set(data.Master, data.Effects, data.Ambient, data.Music);
        }

        /// <summary>The values the settings panel shows: the preset list and the resolutions come from the catalog and the screen.</summary>
        public SettingsValues ToValues()
        {
            SettingsData data = _settings.Data;
            var presetNames = new List<string>();
            int presetIndex = _presets.DefaultIndex;
            for (int i = 0; i < _presets.Presets.Count; i++)
            {
                presetNames.Add(_presets.Presets[i].Name);
                if (string.Equals(_presets.Presets[i].Name, data.GraphicsPreset, StringComparison.OrdinalIgnoreCase))
                {
                    presetIndex = i;
                }
            }

            Vector2Int current = data.Width > 0 && data.Height > 0 ? new Vector2Int(data.Width, data.Height) : new Vector2Int(Screen.width, Screen.height);
            List<string> resolutions = ResolutionOptions.Build(ScreenSizes(), current, out int resolutionIndex);
            return new SettingsValues
            {
                PresetNames = presetNames,
                PresetIndex = presetIndex,
                Resolutions = resolutions,
                ResolutionIndex = resolutionIndex,
                Fullscreen = data.Fullscreen,
                VSync = data.VSync,
                Master = data.Master,
                Effects = data.Effects,
                Ambient = data.Ambient,
                Music = data.Music,
                MouseSensitivity = data.MouseSensitivity,
                InvertY = data.InvertY,
                RandomOffers = data.RandomOffers,
                TestMode = data.TestMode,
            }.Clamped();
        }

        /// <summary>Writes the panel's values into the file and applies them.</summary>
        public void Apply(SettingsValues values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            SettingsValues clamped = values.Clamped();
            _settings.Update(data =>
            {
                data.GraphicsPreset = clamped.PresetNames.Count > 0 ? clamped.PresetNames[clamped.PresetIndex] : string.Empty;
                if (clamped.Resolutions.Count > 0 && ResolutionOptions.TryParse(clamped.Resolutions[clamped.ResolutionIndex], out Vector2Int size))
                {
                    data.Width = size.x;
                    data.Height = size.y;
                }

                data.Fullscreen = clamped.Fullscreen;
                data.VSync = clamped.VSync;
                data.Master = clamped.Master;
                data.Effects = clamped.Effects;
                data.Ambient = clamped.Ambient;
                data.Music = clamped.Music;
                data.MouseSensitivity = clamped.MouseSensitivity;
                data.InvertY = clamped.InvertY;
                data.RandomOffers = clamped.RandomOffers;
                data.TestMode = clamped.TestMode;
            });
            ApplyAll();
        }

        // The screen only changes when the file names a size; a fresh installation keeps whatever Unity started with.
        private static void ApplyScreen(SettingsData data)
        {
            FullScreenMode mode = data.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (data.Width > 0 && data.Height > 0 && (Screen.width != data.Width || Screen.height != data.Height || Screen.fullScreenMode != mode))
            {
                Screen.SetResolution(data.Width, data.Height, mode);
            }
            else if (Screen.fullScreenMode != mode)
            {
                Screen.fullScreenMode = mode;
            }

            QualitySettings.vSyncCount = data.VSync ? 1 : 0;
        }

        private static IEnumerable<Vector2Int> ScreenSizes()
        {
            Resolution[] resolutions = Screen.resolutions;
            for (int i = 0; i < resolutions.Length; i++)
            {
                yield return new Vector2Int(resolutions[i].width, resolutions[i].height);
            }
        }
    }
}
