#nullable enable
using System;
using DemonFighter.Simulation.Content;
using UnityEngine;

namespace DemonFighter.Data
{
    /// <summary>
    /// The asset that references every definition (ARCHITECTURE, "Content pipeline"). Nobody edits it by hand:
    /// Demon Fighter > Rebuild Content Catalog finds all definitions and fills it. <see cref="Build"/> turns it into
    /// the immutable <see cref="ContentCatalog"/> the simulation uses; views look up the definitions themselves for
    /// their placeholder visuals.
    /// </summary>
    [CreateAssetMenu(menuName = "Demon Fighter/Content/Content Catalog", fileName = "ContentCatalog")]
    public sealed class ContentCatalogDefinition : ScriptableObject
    {
        [SerializeField] private CombatTuningDefinition _tuning = null!;
        [SerializeField] private BodyPartDefinition[] _bodyParts = Array.Empty<BodyPartDefinition>();
        [SerializeField] private SkillDefinition[] _skills = Array.Empty<SkillDefinition>();
        [SerializeField] private DemonDefinition[] _demons = Array.Empty<DemonDefinition>();
        [SerializeField] private EvolutionDefinition[] _evolutions = Array.Empty<EvolutionDefinition>();

        /// <summary>Converts every definition and validates the whole; throws for broken or missing content.</summary>
        public ContentCatalog Build()
        {
            if (_tuning == null)
            {
                throw new ContentException("The content catalog has no combat tuning; run Demon Fighter > Rebuild Content Catalog.");
            }

            var parts = new BodyPartSpec[_bodyParts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Require(_bodyParts[i], "body part", i).ToSpec();
            }

            var skills = new SkillSpec[_skills.Length];
            for (int i = 0; i < skills.Length; i++)
            {
                skills[i] = Require(_skills[i], "skill", i).ToSpec();
            }

            var demons = new DemonSpec[_demons.Length];
            for (int i = 0; i < demons.Length; i++)
            {
                demons[i] = Require(_demons[i], "demon", i).ToSpec();
            }

            var evolutions = new EvolutionSpec[_evolutions.Length];
            for (int i = 0; i < evolutions.Length; i++)
            {
                evolutions[i] = Require(_evolutions[i], "evolution", i).ToSpec();
            }

            return new ContentCatalog(_tuning.ToSpec(), parts, skills, demons, evolutions);
        }

        /// <summary>The body part definition with this id, or null; views read its placeholder visual.</summary>
        public BodyPartDefinition? FindBodyPart(string id)
        {
            for (int i = 0; i < _bodyParts.Length; i++)
            {
                if (_bodyParts[i] != null && string.Equals(_bodyParts[i].Id, id, StringComparison.Ordinal))
                {
                    return _bodyParts[i];
                }
            }

            return null;
        }

        internal void SetContent(
            CombatTuningDefinition tuning,
            BodyPartDefinition[] bodyParts,
            SkillDefinition[] skills,
            DemonDefinition[] demons,
            EvolutionDefinition[] evolutions)
        {
            _tuning = tuning;
            _bodyParts = bodyParts;
            _skills = skills;
            _demons = demons;
            _evolutions = evolutions;
        }

        private static T Require<T>(T definition, string kind, int index)
            where T : ScriptableObject
        {
            if (definition == null)
            {
                throw new ContentException("The content catalog has an empty " + kind + " slot at index " + index + ".");
            }

            return definition;
        }
    }
}
