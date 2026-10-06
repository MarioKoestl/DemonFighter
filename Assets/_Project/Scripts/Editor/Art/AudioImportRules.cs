#nullable enable
using System;
using System.IO;
using DemonFighter.Editor.Generate;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Art
{
    /// <summary>
    /// Import settings for everything under the audio folder (D-084): short effects decompress on load and play in
    /// mono, loops (L_) and music (M_) stream, all as Vorbis. Applied as files are imported, so a dropped clip is
    /// right without Inspector clicks. Raising the version re-imports everything with the new rules.
    /// </summary>
    internal sealed class AudioImportRules : AssetPostprocessor
    {
        private const uint RulesVersion = 1;
        private const string LoopPrefix = "L_";
        private const string MusicPrefix = "M_";
        private const float Quality = 0.6f;

        public override uint GetVersion()
        {
            return RulesVersion;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioGenerator.AudioFolder + "/", StringComparison.Ordinal) || !(assetImporter is AudioImporter importer))
            {
                return;
            }

            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool streams = name.StartsWith(LoopPrefix, StringComparison.Ordinal) || name.StartsWith(MusicPrefix, StringComparison.Ordinal);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = streams ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = Quality;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !streams;
            importer.loadInBackground = streams;
        }
    }
}
