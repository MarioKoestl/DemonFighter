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

        public static readonly ArchetypeSpec Hunter = new ArchetypeSpec(
            "Hunter", wanderWeight: 0f, restWeight: 0f, patrolWeight: 0f, wanderRadius: 10f,
            restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: 1, arriveDistance: 0.5f,
            huntWeight: 1f, perceptionRadius: 50f);

        public static readonly ArchetypeSpec Eater = EaterEvery(1);

        public static readonly ArchetypeSpec Coward = new ArchetypeSpec(
            "Coward", wanderWeight: 1f, restWeight: 0f, patrolWeight: 0f, wanderRadius: 10f,
            restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: 1, arriveDistance: 0.5f,
            fleeHealthFraction: 0.5f, perceptionRadius: 50f);

        public static readonly ArchetypeSpec Villager = new ArchetypeSpec(
            "Villager", wanderWeight: 1f, restWeight: 1f, patrolWeight: 0f, wanderRadius: 10f,
            restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: 1, arriveDistance: 0.5f,
            huntWeight: 0.1f, perceptionRadius: 50f);

        public static readonly ArchetypeSpec Sentinel = new ArchetypeSpec(
            "Sentinel", wanderWeight: 0f, restWeight: 0.15f, patrolWeight: 1f, wanderRadius: 30f,
            restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: 1, arriveDistance: 4f,
            huntWeight: 1.5f, perceptionRadius: 60f);

        public static ArchetypeSpec WandererEvery(int ticks)
        {
            return new ArchetypeSpec(
                "Wanderer", wanderWeight: 1f, restWeight: 0f, patrolWeight: 0f, wanderRadius: 10f,
                restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: ticks, arriveDistance: 0.5f);
        }

        public static ArchetypeSpec EaterEvery(int ticks)
        {
            return new ArchetypeSpec(
                "Eater", wanderWeight: 0f, restWeight: 0f, patrolWeight: 0f, wanderRadius: 10f,
                restSecondsMin: 1f, restSecondsMax: 2f, decisionIntervalTicks: ticks, arriveDistance: 0.5f,
                eatWeight: 1f, perceptionRadius: 50f);
        }
    }
}
