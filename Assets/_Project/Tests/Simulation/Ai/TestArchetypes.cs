#nullable enable
using DemonFighter.Simulation.Ai;

namespace DemonFighter.Simulation.Tests.Ai
{
    /// <summary>Single-minded archetypes that decide every tick, so tests observe one behaviour at a time.</summary>
    internal static class TestArchetypes
    {
        public static readonly ArchetypeSpec Wanderer = new ArchetypeSpec(
            "Wanderer", wanderWeight: 1f, restWeight: 0f, patrolWeight: 0f, wanderRadius: 10f,
            restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: 1, arriveDistance: 0.5f);

        public static readonly ArchetypeSpec Rester = new ArchetypeSpec(
            "Rester", wanderWeight: 0f, restWeight: 1f, patrolWeight: 0f, wanderRadius: 10f,
            restSecondsMin: 1f, restSecondsMax: 1f, decisionIntervalTicks: 1, arriveDistance: 0.5f);

        public static readonly ArchetypeSpec Patroller = new ArchetypeSpec(
            "Patroller", wanderWeight: 0f, restWeight: 0f, patrolWeight: 1f, wanderRadius: 10f,
            restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: 1, arriveDistance: 0.5f);

        public static ArchetypeSpec WandererEvery(int ticks)
        {
            return new ArchetypeSpec(
                "Wanderer", wanderWeight: 1f, restWeight: 0f, patrolWeight: 0f, wanderRadius: 10f,
                restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: ticks, arriveDistance: 0.5f);
        }
    }
}
