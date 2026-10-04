#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation.Ai;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// An AI personality as its own asset (GAME_DESIGN, "AI demons"; D-071): goal weights, flee threshold, perception,
    /// and the parts, evolution line and stat it prefers, converted to an <see cref="ArchetypeSpec"/> at load. The biome
    /// asset lists these with spawn weights; the elder keeps the nested personality it always had.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Archetype", fileName = "AR_NewArchetype")]
    public sealed class ArchetypeAsset : ScriptableObject
    {
        [SerializeField] private ArchetypeDefinition _archetype = new ArchetypeDefinition();

        /// <summary>Content name, for logs and the generator.</summary>
        public string ArchetypeName => _archetype.Name;

        /// <summary>Builds the immutable spec; throws for invalid content or a missing part reference.</summary>
        public ArchetypeSpec ToSpec()
        {
            return _archetype.ToSpec();
        }

        internal void Configure(ArchetypeSpec spec, BodyPartDefinition[] preferredParts)
        {
            _archetype.ApplyDefaults(spec);
            _archetype.SetPreferredParts(preferredParts);
        }

        private void OnValidate()
        {
            try
            {
                ToSpec();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.Content, "Archetype asset " + name + " is invalid: " + exception.Message, this);
            }
        }
    }
}
