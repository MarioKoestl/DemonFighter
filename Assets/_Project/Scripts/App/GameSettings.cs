#nullable enable
using System;
using System.IO;
using DemonFighter.Common;
using Newtonsoft.Json;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// The settings of the game in one JSON file next to the save (D-085): graphics, screen, audio, mouse and the
    /// playtest toggle (O-001). Loaded once at boot, written on every change, defaults when the file is missing or
    /// unreadable. The first load carries the old PlayerPrefs offers toggle (D-076) over and forgets it there.
    /// </summary>
    internal sealed class GameSettings
    {
        public const string FileName = "settings.json";

        private const string LegacyRandomOffersKey = "settings.randomOffers";

        private readonly string _path;
        private readonly SettingsData _data;

        public GameSettings()
            : this(Path.Combine(Application.persistentDataPath, FileName))
        {
        }

        /// <summary>Reads the file at the path, or starts from defaults.</summary>
        public GameSettings(string path)
        {
            _path = string.IsNullOrEmpty(path) ? throw new ArgumentException("A path is required.", nameof(path)) : path;
            _data = Load(_path);
        }

        /// <summary>Raised after a change was written.</summary>
        public event Action? Changed;

        /// <summary>Where the file lives.</summary>
        public string FilePath => _path;

        /// <summary>The current values; change them through <see cref="Update"/> so they are written and announced.</summary>
        public SettingsData Data => _data;

        /// <summary>True when the mutation menu offers a random hand of three (O-001, B) instead of every mutation (A).</summary>
        public bool RandomOffers
        {
            get => _data.RandomOffers;
            set => Update(data => data.RandomOffers = value);
        }

        /// <summary>Applies a change, writes the file and tells the listeners.</summary>
        public void Update(Action<SettingsData> change)
        {
            if (change == null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            change(_data);
            Save();
            Changed?.Invoke();
        }

        private void Save()
        {
            try
            {
                string? folder = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                // Written beside the file and moved over it, so a crash mid-write never leaves half a file.
                string temp = _path + ".tmp";
                File.WriteAllText(temp, JsonConvert.SerializeObject(_data, Formatting.Indented));
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }

                File.Move(temp, _path);
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Writing the settings file failed.", exception);
            }
        }

        private static SettingsData Load(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    SettingsData? loaded = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        return loaded;
                    }

                    Log.Warn(LogCategory.App, "The settings file was empty; starting from defaults.");
                }
                catch (Exception exception)
                {
                    Log.Warn(LogCategory.App, "The settings file could not be read; starting from defaults. " + exception.Message);
                }
            }

            var defaults = new SettingsData();
            if (PlayerPrefs.HasKey(LegacyRandomOffersKey))
            {
                defaults.RandomOffers = PlayerPrefs.GetInt(LegacyRandomOffersKey, 0) != 0;
                PlayerPrefs.DeleteKey(LegacyRandomOffersKey);
                PlayerPrefs.Save();
            }

            return defaults;
        }
    }
}
