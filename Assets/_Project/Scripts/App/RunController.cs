#nullable enable
using System;
using System.Globalization;
using DemonFighter.Common;
using DemonFighter.Input;
using DemonFighter.Presentation.Demons;
using DemonFighter.Presentation.World;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.App
{
    /// <summary>
    /// Owns one run from start to end (ARCHITECTURE, "App"): generates the world from the seed, spawns the player,
    /// the blobs and the elder in the simulation, builds the Unity side of all of it, binds camera, HUD and input,
    /// and starts the runner. Everything it creates dies with the Run scene or in <see cref="EndRun"/>.
    /// </summary>
    internal sealed class RunController
    {
        private readonly GameServices _services;
        private SimulationRunner? _runner;
        private WorldView? _world;
        private Transform? _demons;
        private PlayerInputAdapter? _input;

        public RunController(GameServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>The run in progress, or null between runs.</summary>
        public RunState? CurrentRun { get; private set; }

        /// <summary>Starts a run in the loaded Run scene; the scene must contain a <see cref="RunSceneRoot"/>.</summary>
        public void StartRun(int seed)
        {
            if (CurrentRun != null)
            {
                throw new InvalidOperationException("A run is already in progress.");
            }

            RunSceneRoot root = Object.FindAnyObjectByType<RunSceneRoot>();
            if (root == null)
            {
                throw new InvalidOperationException("The Run scene has no RunSceneRoot; run Demon Fighter > Generate > Scenes.");
            }

            root.Validate();

            var state = new RunState(seed, _services.SimulationConfig);
            WorldLayout layout = _services.WorldGenerator.Generate(seed, _services.Biome);
            state.AttachWorld(layout);
            var ticker = new SimulationTicker(state, _services.Events);
            Demon player = SpawnDemons(state, ticker, layout);

            _world = new WorldBuilder(root.Palette, root.WorldSettings).Build(layout, root.transform);
            DemonView playerView = SpawnViews(state, player, root);
            root.CameraRig.Follow(playerView, root.CameraSettings);
            root.Hud.Bind(state, player);

            _input = new PlayerInputAdapter(_services.Actions, player.Id, root.CameraRig, root.CameraRig);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var runnerObject = new GameObject(nameof(SimulationRunner));
            _runner = runnerObject.AddComponent<SimulationRunner>();
            _runner.AddCommandSource(_input);
            _runner.AddFrameUpdatable(_input);
            _runner.Initialize(ticker);

            CurrentRun = state;
            Log.Info(LogCategory.App, "Run started with seed " + seed + ": " + state.Demons.Count + " demons, " + layout.Features.Count + " features.");
        }

        /// <summary>Stops ticking, tears down what the run created and forgets it; safe to call between runs.</summary>
        public void EndRun()
        {
            if (CurrentRun == null)
            {
                return;
            }

            string seconds = CurrentRun.Time.ToString("0.0", CultureInfo.InvariantCulture);
            Log.Info(LogCategory.App, "Run ended after " + CurrentRun.Tick + " ticks (" + seconds + " s).");

            _input?.Dispose();
            _input = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_runner != null)
            {
                Object.Destroy(_runner.gameObject);
            }

            if (_demons != null)
            {
                Object.Destroy(_demons.gameObject);
            }

            _world?.Destroy();
            _runner = null;
            _demons = null;
            _world = null;
            CurrentRun = null;
        }

        // The player comes first, so SpawnPoints[0] and Demons[0] are both the player.
        private static Demon SpawnDemons(RunState state, SimulationTicker ticker, WorldLayout layout)
        {
            BiomeSpec biome = layout.Biome;
            Demon player = state.SpawnDemon(ControllerKind.Player, biome.BlobTemplate, layout.PlayerSpawn, 0f);
            for (int i = 1; i < layout.SpawnPoints.Count; i++)
            {
                float yaw = state.Rng.NextFloat(0f, MathF.PI * 2f);
                Demon blob = state.SpawnDemon(ControllerKind.Ai, biome.BlobTemplate, layout.SpawnPoints[i], yaw);
                ticker.Ai.AddBrain(blob, biome.BlobArchetype, layout.Bounds, null);
            }

            Demon elder = state.SpawnDemon(ControllerKind.Ai, biome.ElderTemplate, layout.ElderSpawn, 0f);
            ticker.Ai.AddBrain(elder, biome.ElderArchetype, layout.Bounds, layout.ElderRoute);
            return player;
        }

        private DemonView SpawnViews(RunState state, Demon player, RunSceneRoot root)
        {
            var factory = new DemonViewFactory(root.DemonPrefab, root.Palette, root.DemonSettings);
            _demons = new GameObject("Demons").transform;
            _demons.SetParent(root.transform, false);

            DemonView? playerView = null;
            for (int i = 0; i < state.Demons.Count; i++)
            {
                DemonView view = factory.Spawn(state.Demons[i], _demons);
                if (state.Demons[i] == player)
                {
                    playerView = view;
                }
            }

            return playerView ?? throw new InvalidOperationException("The player demon got no view.");
        }
    }
}
