#nullable enable
using System;
using System.Globalization;
using DemonFighter.Common;
using DemonFighter.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.App
{
    /// <summary>
    /// Owns one run from start to end: creates the state from a seed, wires the ticker to a runner in the run scene
    /// and logs the lifecycle. World building, views and saving join in later milestones (ARCHITECTURE, "App").
    /// </summary>
    internal sealed class RunController
    {
        private readonly GameServices _services;
        private SimulationRunner? _runner;

        public RunController(GameServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>The run in progress, or null between runs.</summary>
        public RunState? CurrentRun { get; private set; }

        /// <summary>Starts a run in the active scene; the runner object dies with that scene.</summary>
        public void StartRun(int seed)
        {
            if (CurrentRun != null)
            {
                throw new InvalidOperationException("A run is already in progress.");
            }

            var state = new RunState(seed, _services.SimulationConfig);
            var ticker = new SimulationTicker(state, _services.Events);
            var runnerObject = new GameObject(nameof(SimulationRunner));
            _runner = runnerObject.AddComponent<SimulationRunner>();
            _runner.Initialize(ticker);
            CurrentRun = state;
            Log.Info(LogCategory.App, "Run started with seed " + seed + ".");
        }

        /// <summary>Stops ticking and forgets the run; safe to call when no run is in progress.</summary>
        public void EndRun()
        {
            if (CurrentRun == null)
            {
                return;
            }

            string seconds = CurrentRun.Time.ToString("0.0", CultureInfo.InvariantCulture);
            Log.Info(LogCategory.App, "Run ended after " + CurrentRun.Tick + " ticks (" + seconds + " s).");
            if (_runner != null)
            {
                Object.Destroy(_runner.gameObject);
            }

            _runner = null;
            CurrentRun = null;
        }
    }
}
