#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// Personality of an AI demon as data: how much it wants each goal, how it carries them out and how far it
    /// perceives (GAME_DESIGN, "AI demons"). Weights are relative; a goal with weight zero is never chosen.
    /// </summary>
    public sealed record ArchetypeSpec
    {
        public ArchetypeSpec(
            string name,
            float wanderWeight,
            float restWeight,
            float patrolWeight,
            float wanderRadius,
            float restSecondsMin,
            float restSecondsMax,
            int decisionIntervalTicks,
            float arriveDistance,
            float huntWeight = 0f,
            float eatWeight = 0f,
            float fleeHealthFraction = 0f,
            float perceptionRadius = 25f,
            float routePullPerThreat = 0f,
            float routePullMax = 0f)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("An archetype needs a name.", nameof(name));
            }

            if (wanderWeight < 0f || restWeight < 0f || patrolWeight < 0f || huntWeight < 0f || eatWeight < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(wanderWeight), "Goal weights are never negative.");
            }

            if (wanderWeight + restWeight + patrolWeight + huntWeight + eatWeight <= 0f)
            {
                throw new ArgumentException("At least one goal needs a positive weight.", nameof(wanderWeight));
            }

            if (wanderRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(wanderRadius), wanderRadius, "Wander radius must be positive.");
            }

            if (restSecondsMin < 0f || restSecondsMax < restSecondsMin)
            {
                throw new ArgumentOutOfRangeException(nameof(restSecondsMax), "Rest range must be ordered and non-negative.");
            }

            if (decisionIntervalTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(decisionIntervalTicks), decisionIntervalTicks, "Decide at least every tick.");
            }

            if (arriveDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arriveDistance), arriveDistance, "Arrive distance must be positive.");
            }

            if (wanderRadius <= arriveDistance)
            {
                throw new ArgumentOutOfRangeException(nameof(wanderRadius), wanderRadius, "Wander radius must exceed the arrive distance.");
            }

            if (fleeHealthFraction < 0f || fleeHealthFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fleeHealthFraction), fleeHealthFraction, "Flee health fraction must be in [0, 1].");
            }

            if (perceptionRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(perceptionRadius), perceptionRadius, "Perception radius must be positive.");
            }

            if (routePullPerThreat < 0f || routePullMax < 0f || routePullMax > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(routePullMax), routePullMax, "Route pull must be non-negative and the max at most 1.");
            }

            Name = name;
            WanderWeight = wanderWeight;
            RestWeight = restWeight;
            PatrolWeight = patrolWeight;
            WanderRadius = wanderRadius;
            RestSecondsMin = restSecondsMin;
            RestSecondsMax = restSecondsMax;
            DecisionIntervalTicks = decisionIntervalTicks;
            ArriveDistance = arriveDistance;
            HuntWeight = huntWeight;
            EatWeight = eatWeight;
            FleeHealthFraction = fleeHealthFraction;
            PerceptionRadius = perceptionRadius;
            RoutePullPerThreat = routePullPerThreat;
            RoutePullMax = routePullMax;
        }

        /// <summary>Content name for logs.</summary>
        public string Name { get; }

        /// <summary>Relative desire to pick a random nearby point and walk there.</summary>
        public float WanderWeight { get; }

        /// <summary>Relative desire to stand still for a while.</summary>
        public float RestWeight { get; }

        /// <summary>Relative desire to walk the route the world gave this demon; ignored without a route.</summary>
        public float PatrolWeight { get; }

        /// <summary>How far a wander target may be from the current position, in meters; also the flee distance.</summary>
        public float WanderRadius { get; }

        /// <summary>Shortest rest, in seconds.</summary>
        public float RestSecondsMin { get; }

        /// <summary>Longest rest, in seconds.</summary>
        public float RestSecondsMax { get; }

        /// <summary>Ticks between decisions; brains are staggered so not all think on the same tick.</summary>
        public int DecisionIntervalTicks { get; }

        /// <summary>Distance at which a target counts as reached, in meters.</summary>
        public float ArriveDistance { get; }

        /// <summary>Relative desire to hunt perceived prey, scaled by the reward factor and the state of the prey.</summary>
        public float HuntWeight { get; }

        /// <summary>Relative desire to walk to perceived food and eat it, scaled by the reward factor.</summary>
        public float EatWeight { get; }

        /// <summary>Health fraction below which the demon flees from a nearby fight; zero never flees.</summary>
        public float FleeHealthFraction { get; }

        /// <summary>How far the demon perceives other demons and food, in meters.</summary>
        public float PerceptionRadius { get; }

        /// <summary>Fraction of the way from a route waypoint toward the player per threat level (D-070); elders wander closer as the threat rises.</summary>
        public float RoutePullPerThreat { get; }

        /// <summary>The pull never exceeds this fraction, so a route keeps some of its shape.</summary>
        public float RoutePullMax { get; }

        /// <summary>Parts this personality buys in order when it has the Biomass (D-071); the AI mutation goal reads it.</summary>
        public IReadOnlyList<string> PreferredPartIds { get; init; } = Array.Empty<string>();

        /// <summary>The evolution line it takes: the offered option whose fit stat is this one.</summary>
        public StatId PreferredEvolutionStat { get; init; } = StatIds.Strength;

        /// <summary>The stat it puts its points into.</summary>
        public StatId PreferredStat { get; init; } = StatIds.Strength;
    }
}
