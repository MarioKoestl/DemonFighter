#nullable enable
using System;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Combat;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Evolution;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Movement;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Spawning;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Advances a run by one fixed step at a time, in the order from ARCHITECTURE "Tick": commands, movement, status
    /// effects, AI, threat and spawning, food decay, then the event flush. Stages that no milestone needs yet are
    /// simply absent.
    /// </summary>
    public sealed class SimulationTicker
    {
        private HeadlessHitResolver? _headlessHits;

        /// <summary>Binds a run to the event bus its observers listen on and installs the systems and handlers.</summary>
        public SimulationTicker(RunState state, SimulationEvents events)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Damage = new DamageSystem(state, events);
            Behaviours = new SkillBehaviourRegistry();
            Behaviours.Validate(state.Catalog);
            Commands = new CommandQueue();
            Commands.RegisterHandler(new MoveCommandHandler());
            Commands.RegisterHandler(new UseSkillCommandHandler(events, Behaviours));
            Commands.RegisterHandler(new ReportHitCommandHandler(Damage, Behaviours, events));
            Commands.RegisterHandler(new EatCommandHandler());
            Commands.RegisterHandler(new SpendStatPointCommandHandler(events));
            Commands.RegisterHandler(new MutateCommandHandler(events));
            Commands.RegisterHandler(new EvolveCommandHandler(events));
            Ai = new AiSystem();
            Threat = new ThreatSystem(events);
            Spawning = new SpawnSystem(events, Ai);
        }

        /// <summary>The run being advanced.</summary>
        public RunState State { get; }

        /// <summary>Where every change of this run is announced.</summary>
        public SimulationEvents Events { get; }

        /// <summary>Where input and AI drop their commands for the next step.</summary>
        public CommandQueue Commands { get; }

        /// <summary>The brains of the AI demons; register one per AI demon after spawning it.</summary>
        public AiSystem Ai { get; }

        /// <summary>The damage rules of this run; skills and status effects route all harm through it.</summary>
        internal DamageSystem Damage { get; }

        /// <summary>Announces the threat level as it rises (D-069).</summary>
        internal ThreatSystem Threat { get; }

        /// <summary>Tops the Tier 0 population up over time (D-053).</summary>
        internal SpawnSystem Spawning { get; }

        /// <summary>The skill behaviours found by attribute, validated against the content at start.</summary>
        internal SkillBehaviourRegistry Behaviours { get; }

        /// <summary>
        /// Lets the simulation report hits itself for runs without a frame (D-077): the autoplay harness and tests. The
        /// game never calls this; there the CombatPresenter reports what the bodies touched (D-046).
        /// </summary>
        public void EnableHeadlessHits()
        {
            _headlessHits = new HeadlessHitResolver();
        }

        /// <summary>Applies one fixed step. The caller invokes it TicksPerSecond times per simulated second.</summary>
        public void Tick()
        {
            float seconds = State.Config.TickSeconds;
            Commands.ApplyAll(State, Events);
            HoldSystem.Advance(State, seconds);
            MovementSystem.Advance(State, seconds);
            SprintSystem.Advance(State, Events, seconds);
            StatusSystem.Advance(State, Damage, seconds);
            SkillSystem.Advance(State);
            TransformationSystem.Advance(State, Events);
            EatingSystem.Advance(State, Events, seconds);
            Ai.Think(State, Commands);
            _headlessHits?.Advance(State, Commands);
            Threat.Advance(State);
            Spawning.Advance(State);
            FoodDecaySystem.Advance(State, Events);
            State.AdvanceTick();
            Events.Flush();
        }

        /// <summary>
        /// Applies the queued commands without advancing time, for menu commands while the run is paused (the Stats
        /// tab spends points through the same command path as everything else).
        /// </summary>
        public void ApplyPendingCommands()
        {
            Commands.ApplyAll(State, Events);
            Events.Flush();
        }
    }
}
