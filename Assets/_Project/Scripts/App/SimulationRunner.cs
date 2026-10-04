#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// Drives the simulation at its fixed rate from FixedUpdate with an accumulator, so the tick rate is independent
    /// of the frame rate and of the physics rate (ARCHITECTURE, "Tick"). This is the boundary where simulation
    /// exceptions are caught: the run stops and the error is logged with the Sim category.
    /// </summary>
    internal sealed class SimulationRunner : MonoBehaviour
    {
        private SimulationTicker? _ticker;
        private float _accumulator;

        /// <summary>True after an exception stopped the run; no further ticks happen.</summary>
        public bool IsFaulted { get; private set; }

        /// <summary>Ticks applied so far, for diagnostics.</summary>
        public long TickCount => _ticker == null ? 0 : _ticker.State.Tick;

        /// <summary>Binds the runner to a run; called by the controller that created this component.</summary>
        public void Initialize(SimulationTicker ticker)
        {
            _ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
            _accumulator = 0f;
            IsFaulted = false;
        }

        private void FixedUpdate()
        {
            if (_ticker == null || IsFaulted)
            {
                return;
            }

            _accumulator += Time.fixedDeltaTime;
            float tickSeconds = _ticker.State.Config.TickSeconds;
            try
            {
                while (_accumulator >= tickSeconds)
                {
                    _ticker.Tick();
                    _accumulator -= tickSeconds;
                }
            }
            catch (Exception exception)
            {
                IsFaulted = true;
                Log.Error(LogCategory.Sim, "Simulation tick failed; the run is stopped.", exception, this);
            }
        }
    }
}
