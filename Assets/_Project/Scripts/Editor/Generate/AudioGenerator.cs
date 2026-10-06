#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Setup;
using DemonFighter.Simulation.Content;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Generate
{
    /// <summary>
    /// Creates the placeholder sound library from code (D-084): synthesized WAV clips under the audio folder, one
    /// audio event asset per sound with its volume, pitch, distance and cooldown, and the audio catalog. A clip file
    /// that exists is kept, so a recorded or generated file under the same name replaces the synthesized one on the
    /// next generate; an event asset that exists keeps its tuning. Public because the generator calls it.
    /// </summary>
    public static class AudioGenerator
    {
        internal const string AudioFolder = "Assets/_Project/Audio";
        internal const string GeneratedFolder = AudioFolder + "/Generated";
        internal const string AudioContentFolder = "Assets/_Project/Content/Audio";
        internal const string CatalogPath = AudioContentFolder + "/AudioCatalog.asset";
        internal const string DroneClip = "L_CavernDrone";
        internal const string LavaClip = "L_Lava";
        internal const string MenuTrackClip = "M_Menu_Placeholder";
        internal const string CalmTrackClip = "M_Calm_Placeholder";
        internal const string CombatTrackClip = "M_Combat_Placeholder";

        private const string WavExtension = ".wav";
        private const float LoopSeconds = 12f;
        private const float MusicSeconds = 16f;

        /// <summary>The clips and events the generator created, by name, plus the catalog.</summary>
        internal sealed class AudioLibrary
        {
            private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
            private readonly Dictionary<string, AudioEventDefinition> _events = new Dictionary<string, AudioEventDefinition>(StringComparer.Ordinal);

            public AudioCatalogDefinition Catalog { get; internal set; } = null!;

            public AudioClip Clip(string name) => _clips[name];

            public AudioEventDefinition Event(string name) => _events[name];

            /// <summary>The use sound of a placeholder skill by its id; null for skills that make no sound of their own.</summary>
            public AudioEventDefinition? EventForSkill(string skillId)
            {
                switch (skillId)
                {
                    case PlaceholderContent.BiteId:
                        return Event("Bite");
                    case PlaceholderContent.ClawId:
                        return Event("Claw");
                    case PlaceholderContent.GrabId:
                        return Event("Grab");
                    case PlaceholderContent.LungeId:
                        return Event("Lunge");
                    case PlaceholderContent.TailSwingId:
                        return Event("TailSwing");
                    default:
                        return null;
                }
            }

            /// <summary>The sound a part makes when it is lost: a tear for severed parts, a crunch for destroyed ones.</summary>
            public AudioEventDefinition SeverSoundFor(PartFate fate)
            {
                return fate == PartFate.Severed ? Event("Sever") : Event("PartDestroyed");
            }

            internal void Add(string name, AudioClip clip) => _clips[name] = clip;

            internal void Add(string name, AudioEventDefinition sound) => _events[name] = sound;
        }

        /// <summary>Ensures every placeholder clip, event and the catalog exist and returns them.</summary>
        internal static AudioLibrary Ensure()
        {
            EditorAssets.EnsureFolder(GeneratedFolder);
            EditorAssets.EnsureFolder(AudioContentFolder);
            var library = new AudioLibrary();

            AudioClip bite = Clip(library, "S_Bite", () => AudioSynth.Bite(1));
            AudioClip claw = Clip(library, "S_Claw", () => AudioSynth.Claw(2));
            AudioClip grab = Clip(library, "S_Grab", () => AudioSynth.Grab(3));
            AudioClip lunge = Clip(library, "S_Lunge", () => AudioSynth.Lunge(4));
            AudioClip tailSwing = Clip(library, "S_TailSwing", () => AudioSynth.TailSwing(5));
            AudioClip wetImpact = Clip(library, "S_WetImpact", () => AudioSynth.WetImpact(6));
            AudioClip footstepA = Clip(library, "S_Footstep_01", () => AudioSynth.Footstep(7));
            AudioClip footstepB = Clip(library, "S_Footstep_02", () => AudioSynth.Footstep(8));
            AudioClip eat = Clip(library, "S_Eat", () => AudioSynth.Eat(9));
            AudioClip mutationStart = Clip(library, "S_MutationStart", () => AudioSynth.MutationStart(10));
            AudioClip mutationComplete = Clip(library, "S_MutationComplete", () => AudioSynth.MutationComplete(11));
            AudioClip evolved = Clip(library, "S_Evolved", () => AudioSynth.Evolved(12));
            AudioClip levelUp = Clip(library, "S_LevelUp", () => AudioSynth.LevelUp(13));
            AudioClip threatRise = Clip(library, "S_ThreatRise", () => AudioSynth.ThreatRise(14));
            AudioClip playerDeath = Clip(library, "S_PlayerDeath", () => AudioSynth.PlayerDeath(15));
            AudioClip demonDeath = Clip(library, "S_DemonDeath", () => AudioSynth.DemonDeath(16));
            AudioClip sever = Clip(library, "S_Sever", () => AudioSynth.Sever(17));
            AudioClip partDestroyed = Clip(library, "S_PartDestroyed", () => AudioSynth.PartDestroyed(18));
            AudioClip burn = Clip(library, "S_Burn", () => AudioSynth.Burn(23));
            Clip(library, DroneClip, () => AudioSynth.CavernDrone(LoopSeconds, 19));
            Clip(library, LavaClip, () => AudioSynth.LavaLoop(LoopSeconds, 20));
            AudioClip menuTrack = Clip(library, MenuTrackClip, () => AudioSynth.MenuTrack(MusicSeconds));
            Clip(library, CalmTrackClip, () => AudioSynth.CalmTrack(MusicSeconds, 21));
            Clip(library, CombatTrackClip, () => AudioSynth.CombatTrack(MusicSeconds, 22));

            Event(library, "Bite", new[] { bite }, 0.9f, 0.15f, 0.12f, true, 2f, 35f, 0.05f);
            Event(library, "Claw", new[] { claw }, 0.8f, 0.15f, 0.12f, true, 2f, 35f, 0.05f);
            Event(library, "Grab", new[] { grab }, 0.8f, 0.15f, 0.1f, true, 2f, 35f, 0.05f);
            Event(library, "Lunge", new[] { lunge }, 0.8f, 0.1f, 0.1f, true, 3f, 40f, 0.05f);
            Event(library, "TailSwing", new[] { tailSwing }, 0.9f, 0.1f, 0.1f, true, 3f, 40f, 0.05f);
            Event(library, "WetImpact", new[] { wetImpact }, 0.8f, 0.2f, 0.15f, true, 2f, 35f, 0.04f);
            Event(library, "Footstep", new[] { footstepA, footstepB }, 0.35f, 0.3f, 0.15f, true, 1.5f, 25f, 0.03f);
            Event(library, "Eat", new[] { eat }, 0.6f, 0.15f, 0.1f, true, 2f, 25f, 0.35f);
            Event(library, "MutationStart", new[] { mutationStart }, 0.7f, 0.1f, 0.05f, true, 3f, 40f, 0.2f);
            Event(library, "MutationComplete", new[] { mutationComplete }, 0.7f, 0.1f, 0.05f, true, 3f, 40f, 0.2f);
            Event(library, "Evolved", new[] { evolved }, 0.9f, 0.05f, 0.03f, true, 4f, 60f, 0.5f);
            Event(library, "LevelUp", new[] { levelUp }, 0.8f, 0.05f, 0.02f, false, 1f, 10f, 0.5f);
            Event(library, "ThreatRise", new[] { threatRise }, 0.8f, 0.05f, 0.03f, false, 1f, 10f, 1f);
            Event(library, "PlayerDeath", new[] { playerDeath }, 1f, 0f, 0f, false, 1f, 10f, 1f);
            Event(library, "DemonDeath", new[] { demonDeath }, 0.8f, 0.15f, 0.15f, true, 3f, 50f, 0.1f);
            Event(library, "Sever", new[] { sever }, 0.9f, 0.15f, 0.12f, true, 2f, 40f, 0.05f);
            Event(library, "PartDestroyed", new[] { partDestroyed }, 0.8f, 0.15f, 0.12f, true, 2f, 35f, 0.05f);
            Event(library, "Burn", new[] { burn }, 0.6f, 0.15f, 0.12f, true, 2f, 30f, 0.35f);

            library.Catalog = EditorAssets.LoadOrCreate<AudioCatalogDefinition>(CatalogPath, catalog => catalog.Configure(
                library.Event("Footstep"),
                library.Event("Eat"),
                library.Event("WetImpact"),
                library.Event("Sever"),
                library.Event("PartDestroyed"),
                library.Event("DemonDeath"),
                library.Event("PlayerDeath"),
                library.Event("MutationStart"),
                library.Event("MutationComplete"),
                library.Event("Evolved"),
                library.Event("LevelUp"),
                library.Event("ThreatRise"),
                menuTrack));
            if (library.Catalog.Burn == null)
            {
                // The burn sound came with D-086; an older catalog gets it once.
                library.Catalog.SetBurn(library.Event("Burn"));
                EditorUtility.SetDirty(library.Catalog);
            }

            if (library.Catalog.MenuTrack == null)
            {
                // A catalog whose menu track file was replaced points at nothing; it gets the current clips again.
                library.Catalog.Configure(
                    library.Event("Footstep"),
                    library.Event("Eat"),
                    library.Event("WetImpact"),
                    library.Event("Sever"),
                    library.Event("PartDestroyed"),
                    library.Event("DemonDeath"),
                    library.Event("PlayerDeath"),
                    library.Event("MutationStart"),
                    library.Event("MutationComplete"),
                    library.Event("Evolved"),
                    library.Event("LevelUp"),
                    library.Event("ThreatRise"),
                    menuTrack);
                EditorUtility.SetDirty(library.Catalog);
            }

            return library;
        }

        // A clip file is synthesized once; afterwards the file on disk is the truth, whoever wrote it.
        private static AudioClip Clip(AudioLibrary library, string name, Func<float[]> synthesize)
        {
            string path = GeneratedFolder + "/" + name + WavExtension;
            var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (existing == null)
            {
                File.WriteAllBytes(path, AudioSynth.ToWav(synthesize(), AudioSynth.SampleRate));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (existing == null)
                {
                    throw new IOException("Synthesized " + path + " but Unity did not import it.");
                }

                Log.Info(LogCategory.Editor, "Synthesized placeholder sound " + path + ".");
            }

            library.Add(name, existing);
            return existing;
        }

        // An event asset keeps its tuning once it exists; only an event that lost its clips gets them back.
        private static void Event(AudioLibrary library, string name, AudioClip[] clips, float volume, float volumeVariance, float pitchVariance, bool spatial, float minDistance, float maxDistance, float cooldownSeconds)
        {
            string path = AudioContentFolder + "/AE_" + name + ".asset";
            AudioEventDefinition sound = EditorAssets.LoadOrCreate<AudioEventDefinition>(path, s =>
                s.Configure("sound." + name.ToLowerInvariant(), clips, volume, volumeVariance, 1f, pitchVariance, spatial, minDistance, maxDistance, cooldownSeconds));
            if (!sound.HasClips)
            {
                sound.SetClips(clips);
                EditorUtility.SetDirty(sound);
            }

            library.Add(name, sound);
        }
    }
}
