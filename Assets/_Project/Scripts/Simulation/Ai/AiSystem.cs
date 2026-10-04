#nullable enable
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// Runs the brains of all AI demons, each every few ticks and staggered by index, so the cost stays flat as the
    /// demon count grows (ARCHITECTURE, "AI"). Stage 3 of the tick.
    /// </summary>
    public sealed class AiSystem
    {
        private readonly List<UtilityBrain> _brains = new List<UtilityBrain>();

        /// <summary>Every brain in registration order.</summary>
        public IReadOnlyList<UtilityBrain> Brains => _brains;

        /// <summary>Gives an AI demon a brain; the route is only used by archetypes that patrol.</summary>
        public UtilityBrain AddBrain(Demon demon, ArchetypeSpec archetype, GroundBounds bounds, IReadOnlyList<Vector3>? route)
        {
            var brain = new UtilityBrain(demon, archetype, bounds, route);
            _brains.Add(brain);
            return brain;
        }

        /// <summary>Lets every brain whose turn it is decide; commands land in the queue for the next tick.</summary>
        public void Think(RunState state, CommandQueue commands)
        {
            long tick = state.Tick;
            for (int i = 0; i < _brains.Count; i++)
            {
                UtilityBrain brain = _brains[i];
                if ((tick + i) % brain.Archetype.DecisionIntervalTicks == 0)
                {
                    brain.Decide(state, commands);
                }
            }
        }
    }
}
