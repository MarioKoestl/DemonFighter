#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Commands;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// Drives the simulation at its fixed rate from FixedUpdate with an accumulator, so the tick rate is independent
    /// of the frame rate and of the physics rate (ARCHITECTURE, "Tick"). Before every tick it collects the commands of
    /// the registered sources. While paused (a menu is open, the player died) no time passes, but commands dropped
    /// into the queue directly, such as spending a stat point, still apply. This is the boundary where simulation
    /// exceptions are caught: the run stops and the error is logged with the Sim category.
    /// </summary>
    internal sealed class SimulationRunner : MonoBehaviour
    {
        private readonly List<ICommandSource> _commandSources = new List<ICommandSource>();
        private readonly List<IFrameUpdatable> _frameUpdatables = new List<IFrameUpdatable>();
        private SimulationTicker? _ticker;
        private float _accumulator;

        /// <summary>True after an exception stopped the run; no further ticks happen.</summary>
        public bool IsFaulted { get; private set; }

        /// <summary>While true, time stands still: no ticks, no input, no frame updates; menu commands still apply.</summary>
        public bool Paused { get; set; }

        /// <summary>Ticks applied so far, for diagnostics.</summary>
        public long TickCount => _ticker == null ? 0 : _ticker.State.Tick;

        /// <summary>Binds the runner to a run; called by the controller that created this component.</summary>
        public void Initialize(SimulationTicker ticker)
        {
            _ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
            _accumulator = 0f;
            IsFaulted = false;
            Paused = false;
        }

        /// <summary>Asks this source for commands before every tick, in registration order.</summary>
        public void AddCommandSource(ICommandSource source)
        {
            _commandSources.Add(source ?? throw new ArgumentNullException(nameof(source)));
        }

        /// <summary>Gives this object one call per rendered frame.</summary>
        public void AddFrameUpdatable(IFrameUpdatable updatable)
        {
            _frameUpdatables.Add(updatable ?? throw new ArgumentNullException(nameof(updatable)));
        }

        private void Update()
        {
            if (_ticker == null || IsFaulted)
            {
                return;
            }

            if (Paused)
            {
                ApplyMenuCommands();
                return;
            }

            for (int i = 0; i < _frameUpdatables.Count; i++)
            {
                _frameUpdatables[i].UpdateFrame();
            }
        }

        private void FixedUpdate()
        {
            if (_ticker == null || IsFaulted)
            {
                return;
            }

            if (Paused)
            {
                _accumulator = 0f;
                return;
            }

            _accumulator += Time.fixedDeltaTime;
            float tickSeconds = _ticker.State.Config.TickSeconds;
            try
            {
                while (!Paused && _accumulator >= tickSeconds)
                {
                    for (int i = 0; i < _commandSources.Count; i++)
                    {
                        _commandSources[i].SubmitCommands(_ticker.Commands);
                    }

                    _ticker.Tick();
                    _accumulator -= tickSeconds;
                }
            }
            catch (Exception exception)
            {
                Fault(exception);
            }
        }

        private void ApplyMenuCommands()
        {
            try
            {
                _ticker!.ApplyPendingCommands();
            }
            catch (Exception exception)
            {
                Fault(exception);
            }
        }

        private void Fault(Exception exception)
        {
            IsFaulted = true;
            Log.Error(LogCategory.Sim, "Simulation tick failed; the run is stopped.", exception, this);
        }
    }
}
