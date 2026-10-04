#nullable enable
using System;
using System.Globalization;
using DemonFighter.Common;
using DemonFighter.Input;
using DemonFighter.Presentation.Combat;
using DemonFighter.Presentation.Demons;
using DemonFighter.Presentation.World;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Worldgen;
using DemonFighter.UI;
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
        private CombatPresenter? _combat;
        private SimulationTicker? _ticker;
        private HudScreen? _hud;
        private Demon? _player;
        private IDisposable? _deathSubscription;
        private IDisposable? _spawnSubscription;
        private DemonViewFactory? _factory;

        public RunController(GameServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>The run in progress, or null between runs.</summary>
        public RunState? CurrentRun { get; private set; }

        /// <summary>Raised when the death screen asks for the main menu; the scene flow ends the run and loads it.</summary>
        public event Action? ReturnToMenuRequested;

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

            var state = new RunState(seed, _services.SimulationConfig, _services.Catalog);
            WorldLayout layout = _services.WorldGenerator.Generate(seed, _services.Biome);
            state.AttachWorld(layout);
            var ticker = new SimulationTicker(state, _services.Events);
            Demon player = SpawnDemons(state, ticker, layout);

            _world = new WorldBuilder(root.Palette, root.WorldSettings).Build(layout, root.transform);
            var combat = new CombatPresenter(state, _services.Events, root.Palette, player.Id, root.transform);
            _combat = combat;
            DemonView playerView = SpawnViews(state, player, root, combat);
            root.CameraRig.Follow(playerView, root.CameraSettings);
            root.Hud.Bind(state, player, () => combat.AimedFood);
            root.Hud.StatPointRequested += OnStatPointRequested;
            root.Hud.BackToMenuRequested += OnBackToMenuRequested;
            _hud = root.Hud;
            _ticker = ticker;
            _player = player;
            _deathSubscription = _services.Events.Subscribe<DemonDied>(OnDemonDied);
            _spawnSubscription = _services.Events.Subscribe<DemonSpawned>(OnDemonSpawned);

            _input = new PlayerInputAdapter(_services.Actions, player, root.CameraRig, root.CameraRig, new CombatAim(combat));
            _input.StatsMenuToggled += OnStatsMenuToggled;
            SetCursorLocked(true);

            var runnerObject = new GameObject(nameof(SimulationRunner));
            _runner = runnerObject.AddComponent<SimulationRunner>();
            _runner.AddCommandSource(_input);
            _runner.AddFrameUpdatable(_input);
            _runner.AddCommandSource(_combat);
            _runner.AddFrameUpdatable(_combat);
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

            if (_input != null)
            {
                _input.StatsMenuToggled -= OnStatsMenuToggled;
                _input.Dispose();
                _input = null;
            }

            if (_hud != null)
            {
                _hud.StatPointRequested -= OnStatPointRequested;
                _hud.BackToMenuRequested -= OnBackToMenuRequested;
                _hud = null;
            }

            _deathSubscription?.Dispose();
            _deathSubscription = null;
            _spawnSubscription?.Dispose();
            _spawnSubscription = null;
            _factory = null;
            _combat?.Dispose();
            _combat = null;
            _ticker = null;
            _player = null;
            SetCursorLocked(false);

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

        // C opens the Stats panel and freezes the run; its plus buttons drop commands the paused runner still applies.
        private void OnStatsMenuToggled()
        {
            if (_runner == null || _hud == null || _player == null || !_player.IsAlive)
            {
                return;
            }

            bool open = _hud.ToggleStatsPanel();
            _runner.Paused = open;
            SetCursorLocked(!open);
        }

        private void OnStatPointRequested(StatId stat)
        {
            if (_ticker != null && _player != null)
            {
                _ticker.Commands.Submit(new SpendStatPointCommand(_player.Id, stat));
            }
        }

        // The death of the player freezes the run and shows the summary; the world stays as it was (GAME_DESIGN, "End of run").
        private void OnDemonDied(DemonDied evt)
        {
            if (_player == null || evt.Demon != _player.Id || _runner == null || _hud == null || CurrentRun == null)
            {
                return;
            }

            _runner.Paused = true;
            _hud.ShowRunSummary(CurrentRun, _player);
            SetCursorLocked(false);
            Log.Info(LogCategory.App, "The player died after " + CurrentRun.Tick + " ticks with " + _player.Kills + " kills.");
        }

        // A demon the simulation spawned mid-run gets its body the same way as the ones from run start.
        private void OnDemonSpawned(DemonSpawned evt)
        {
            if (CurrentRun == null || _factory == null || _demons == null || _combat == null || !CurrentRun.TryGetDemon(evt.Demon, out Demon? demon))
            {
                return;
            }

            DemonView view = _factory.Spawn(demon, _demons);
            _combat.Register(view);
        }

        private void OnBackToMenuRequested()
        {
            ReturnToMenuRequested?.Invoke();
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // The player comes first, so SpawnPoints[0] and Demons[0] are both the player.
        private static Demon SpawnDemons(RunState state, SimulationTicker ticker, WorldLayout layout)
        {
            BiomeSpec biome = layout.Biome;
            Demon player = state.SpawnDemon(ControllerKind.Player, biome.BlobDemon, layout.PlayerSpawn, 0f);
            for (int i = 1; i < layout.SpawnPoints.Count; i++)
            {
                float yaw = state.Rng.NextFloat(0f, MathF.PI * 2f);
                Demon blob = state.SpawnDemon(ControllerKind.Ai, biome.BlobDemon, layout.SpawnPoints[i], yaw);
                ticker.Ai.AddBrain(blob, biome.BlobArchetype, layout.Bounds, null);
            }

            Demon elder = state.SpawnDemon(ControllerKind.Ai, biome.ElderDemon, layout.ElderSpawn, 0f);
            ticker.Ai.AddBrain(elder, biome.ElderArchetype, layout.Bounds, layout.ElderRoute);
            return player;
        }

        private DemonView SpawnViews(RunState state, Demon player, RunSceneRoot root, CombatPresenter combat)
        {
            var factory = new DemonViewFactory(root.DemonPrefab, root.Palette, root.DemonSettings);
            _factory = factory;
            _demons = new GameObject("Demons").transform;
            _demons.SetParent(root.transform, false);

            DemonView? playerView = null;
            for (int i = 0; i < state.Demons.Count; i++)
            {
                DemonView view = factory.Spawn(state.Demons[i], _demons);
                combat.Register(view);
                if (state.Demons[i] == player)
                {
                    playerView = view;
                }
            }

            return playerView ?? throw new InvalidOperationException("The player demon got no view.");
        }
    }
}
