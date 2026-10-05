#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Hazards;

namespace DemonFighter.Simulation.Progression
{
    /// <summary>
    /// What a finished run amounts to (GAME_DESIGN, "End of run"; D-075): the death screen shows it and the
    /// meta-progression hook receives it. Plain data, built once when the player dies.
    /// </summary>
    public sealed record RunSummary
    {
        public int Seed { get; init; }

        public long TicksSurvived { get; init; }

        public float SecondsSurvived { get; init; }

        public int Kills { get; init; }

        public float BiomassEaten { get; init; }

        public int Level { get; init; }

        /// <summary>The highest tier the player reached during the run.</summary>
        public int HighestTier { get; init; }

        /// <summary>The tier at death.</summary>
        public int FinalTier { get; init; }

        /// <summary>Ids of the evolutions taken, in order.</summary>
        public IReadOnlyList<string> EvolutionIds { get; init; } = Array.Empty<string>();

        /// <summary>Ids of every part of the final body, lost or not, the core first.</summary>
        public IReadOnlyList<string> PartIds { get; init; } = Array.Empty<string>();

        /// <summary>Kind id of the killer; empty when nothing living dealt the final blow.</summary>
        public string KillerSpecId { get; init; } = string.Empty;

        /// <summary>Kind name of the killer; empty when nothing living dealt the final blow.</summary>
        public string KillerName { get; init; } = string.Empty;

        /// <summary>The hazard the player stood in when it died (D-086); None when it died outside lava and fissures.</summary>
        public HazardKind DeathHazard { get; init; }

        /// <summary>Reads the summary off the run and the dead player; the killer may be None or already gone.</summary>
        public static RunSummary From(RunState state, Demon player, DemonId killer)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            IReadOnlyList<BodyPart> parts = player.Body.Parts;
            var partIds = new string[parts.Count];
            for (int i = 0; i < partIds.Length; i++)
            {
                partIds[i] = parts[i].Spec.Id;
            }

            Demon? killerDemon = killer.IsValid && state.TryGetDemon(killer, out Demon? found) ? found : null;
            return new RunSummary
            {
                Seed = state.Seed,
                TicksSurvived = state.Tick,
                SecondsSurvived = state.Time,
                Kills = player.Kills,
                BiomassEaten = player.BiomassEaten,
                Level = player.Level,
                HighestTier = player.HighestTier,
                FinalTier = player.Tier,
                EvolutionIds = new List<string>(player.EvolutionIds),
                PartIds = partIds,
                KillerSpecId = killerDemon != null ? killerDemon.Spec.Id : string.Empty,
                KillerName = killerDemon != null ? killerDemon.Spec.Name : string.Empty,
                DeathHazard = player.Hazard,
            };
        }
    }
}
