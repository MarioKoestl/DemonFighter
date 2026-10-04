#nullable enable
using System;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Movement;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Advances a run by one fixed step at a time, in the order from ARCHITECTURE "Tick": commands, movement, status
    /// effects, AI, threat and spawning, food decay, then the event flush. Stages that no milestone needs yet are
    /// simply absent.
    /// </summary>
    public sealed class SimulationTicker
    {
        /// <summary>Binds a run to the event bus its observers listen on and installs the command handlers.</summary>
        public SimulationTicker(RunState state, SimulationEvents events)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Commands = new CommandQueue();
            Commands.RegisterHandler(new MoveCommandHandler());
            Ai = new AiSystem();
        }

        /// <summary>The run being advanced.</summary>
        public RunState State { get; }

        /// <summary>Where every change of this run is announced.</summary>
        public SimulationEvents Events { get; }

        /// <summary>Where input and AI drop their commands for the next step.</summary>
        public CommandQueue Commands { get; }

        /// <summary>The brains of the AI demons; register one per AI demon after spawning it.</summary>
        public AiSystem Ai { get; }

        /// <summary>Applies one fixed step. The caller invokes it TicksPerSecond times per simulated second.</summary>
        public void Tick()
        {
            Commands.ApplyAll(State, Events);
            MovementSystem.Advance(State, State.Config.TickSeconds);
            Ai.Think(State, Commands);
            State.AdvanceTick();
            Events.Flush();
        }
    }
}
