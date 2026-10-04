#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Editor.Generate;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Worldgen;
using UnityEditor;

namespace DemonFighter.Editor.Tools
{
    /// <summary>
    /// Plays seeded runs headless with an aggressive AI in the player's place and reports how long it survived, how
    /// far it got and what the world looked like at the end (D-077): the tuning harness of M4. Reads the real content
    /// assets, so the numbers are the ones the game plays with. Menu: Demon Fighter > Tools; batch mode:
    /// -executeMethod DemonFighter.Editor.Tools.AutoplayHarness.Run -seeds 3 -minutes 15 -firstSeed 1.
    /// </summary>
    public static class AutoplayHarness
    {
        private const string ReportPath = "TestResults/autoplay.txt";

        [MenuItem("Demon Fighter/Tools/Autoplay 3 Seeds for 15 Minutes")]
        public static void RunDefault()
        {
            Run(3, 15, 1);
        }

        /// <summary>Batch entry: reads -seeds, -minutes and -firstSeed from the command line, defaults 3, 15 and 1.</summary>
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            Run(Argument(args, "-seeds", 3), Argument(args, "-minutes", 15), Argument(args, "-firstSeed", 1));
        }

        /// <summary>Plays the runs and writes the report; returns it as text.</summary>
        public static string Run(int seeds, int minutes, int firstSeed)
        {
            var catalogDefinition = AssetDatabase.LoadAssetAtPath<ContentCatalogDefinition>(ContentCatalogRebuilder.CatalogPath);
            var biomeDefinition = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(PlaceholderAssetGenerator.AshCavernPath);
            if (catalogDefinition == null || biomeDefinition == null)
            {
                throw new FileNotFoundException("Content catalog or biome asset missing; run Demon Fighter > Generate > Placeholder Assets.");
            }

            ContentCatalog catalog = catalogDefinition.Build();
            BiomeSpec biome = biomeDefinition.ToSpec();
            var report = new StringBuilder();
            report.AppendLine("Autoplay: aggressive AI as the player, " + minutes + " minutes per run, biome " + biome.Id);
            for (int i = 0; i < seeds; i++)
            {
                report.AppendLine(Play(firstSeed + i, minutes, catalog, biome));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
            File.WriteAllText(ReportPath, report.ToString());
            Log.Info(LogCategory.Editor, report.ToString());
            return report.ToString();
        }

        private static string Play(int seed, int minutes, ContentCatalog catalog, BiomeSpec biome)
        {
            var state = new RunState(seed, SimulationConfig.Default, catalog);
            WorldLayout layout = new CavernWorldGenerator().Generate(seed, biome);
            state.AttachWorld(layout);
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            ticker.EnableHeadlessHits();
            Demon protagonist = state.SpawnDemon(ControllerKind.Ai, biome.BlobDemon, layout.PlayerSpawn, 0f);
            ticker.Ai.AddBrain(protagonist, biome.AggressiveArchetype, layout.Bounds, null);
            for (int i = 1; i < layout.SpawnPoints.Count; i++)
            {
                Demon blob = state.SpawnDemon(ControllerKind.Ai, biome.BlobDemon, layout.SpawnPoints[i], state.Rng.NextFloat(0f, MathF.PI * 2f));
                ticker.Ai.AddBrain(blob, biome.PickBlobArchetype(state.Rng), layout.Bounds, null);
            }

            Demon elder = state.SpawnDemon(ControllerKind.Ai, biome.ElderDemon, layout.ElderSpawn, 0f);
            ticker.Ai.AddBrain(elder, biome.ElderArchetype, layout.Bounds, layout.ElderRoute);

            long diedAtTick = -1;
            string killer = string.Empty;
            events.Subscribe<DemonDied>(evt =>
            {
                if (evt.Demon == protagonist.Id)
                {
                    diedAtTick = state.Tick;
                    killer = state.TryGetDemon(evt.Killer, out Demon? k) ? k.Spec.Name : "bleeding";
                }
            });

            long totalTicks = (long)minutes * 60 * state.Config.TicksPerSecond;
            while (state.Tick < totalTicks && diedAtTick < 0)
            {
                ticker.Tick();
            }

            int alive = 0;
            for (int i = 0; i < state.Demons.Count; i++)
            {
                if (state.Demons[i].IsAlive)
                {
                    alive++;
                }
            }

            string survived = diedAtTick < 0
                ? "survived all " + minutes + " min"
                : "died at " + Clock(diedAtTick * state.Config.TickSeconds) + " (" + killer + ")";
            return "seed " + seed + ": " + survived
                + ", tier " + protagonist.HighestTier + ", level " + protagonist.Level + ", " + protagonist.Kills + " kills"
                + ", " + protagonist.Body.Parts.Count + " parts, " + protagonist.Evolutions + " evolutions"
                + ", threat " + state.Threat.ToString("0.0", CultureInfo.InvariantCulture)
                + ", " + alive + " alive of " + state.Demons.Count + ", " + state.Food.Count + " food";
        }

        private static string Clock(float seconds)
        {
            int whole = (int)seconds;
            return (whole / 60) + ":" + (whole % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static int Argument(string[] args, string name, int fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    return value;
                }
            }

            return fallback;
        }
    }
}
