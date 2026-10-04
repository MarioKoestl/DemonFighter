#nullable enable
using System;
using System.IO;
using DemonFighter.Common;
using DemonFighter.Simulation.Persistence;
using Newtonsoft.Json;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// The one save slot (D-026, D-074): the snapshot of a run as JSON under the persistent data path, written
    /// atomically so a crash mid-write never leaves a half file. Resuming and death delete it; this class only moves
    /// the snapshot to and from disk and never looks inside it.
    /// </summary>
    internal sealed class RunSaveService
    {
        private const string FileName = "run.json";

        private readonly string _path;

        public RunSaveService()
            : this(Path.Combine(Application.persistentDataPath, FileName))
        {
        }

        /// <summary>Uses a specific file, for tests.</summary>
        public RunSaveService(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A save path is required.", nameof(path));
            }

            _path = path;
        }

        /// <summary>Where the slot lives.</summary>
        public string FilePath => _path;

        /// <summary>True while a saved run waits in the slot.</summary>
        public bool HasSave => File.Exists(_path);

        /// <summary>Writes the snapshot, replacing whatever the slot held.</summary>
        public void Save(RunSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            string? directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(snapshot, Formatting.Indented));
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
            Log.Info(LogCategory.App, "Run saved to " + _path + " at tick " + snapshot.Tick + ".");
        }

        /// <summary>Reads the slot; null when it is empty or unreadable, which is logged and otherwise ignored.</summary>
        public RunSnapshot? Load()
        {
            if (!HasSave)
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<RunSnapshot>(File.ReadAllText(_path));
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "The saved run could not be read and is ignored.", exception);
                return null;
            }
        }

        /// <summary>Empties the slot; nothing happens when it already is.</summary>
        public void Delete()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
