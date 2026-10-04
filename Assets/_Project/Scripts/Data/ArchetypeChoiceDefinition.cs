#nullable enable
using System;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>One personality a biome asset hands to new blobs, with its spawn weight (D-071).</summary>
    [Serializable]
    public sealed class ArchetypeChoiceDefinition
    {
        [SerializeField] private ArchetypeAsset _archetype = null!;
        [SerializeField] private float _weight = 1f;

        /// <summary>Builds the immutable choice; throws for a missing archetype or a bad weight.</summary>
        public ArchetypeChoice ToSpec(string biomeId)
        {
            if (_archetype == null)
            {
                throw new ContentException("Biome " + biomeId + " lists an archetype choice without an archetype.");
            }

            return new ArchetypeChoice(_archetype.ToSpec(), _weight);
        }

        internal void Configure(ArchetypeAsset archetype, ArchetypeChoice choice)
        {
            _archetype = archetype;
            _weight = choice.Weight;
        }
    }
}
