#nullable enable
using System;
using System.Collections.Generic;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Every spec the simulation can refer to, looked up by id (ARCHITECTURE, "Content pipeline"). Validates each
    /// spec and the references between them once, at load, so a broken asset fails before the first tick.
    /// </summary>
    public sealed class ContentCatalog
    {
        private readonly Dictionary<string, BodyPartSpec> _bodyParts = new Dictionary<string, BodyPartSpec>(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillSpec> _skills = new Dictionary<string, SkillSpec>(StringComparer.Ordinal);
        private readonly Dictionary<string, DemonSpec> _demons = new Dictionary<string, DemonSpec>(StringComparer.Ordinal);

        public ContentCatalog(
            CombatTuning tuning,
            IEnumerable<BodyPartSpec> bodyParts,
            IEnumerable<SkillSpec> skills,
            IEnumerable<DemonSpec> demons)
        {
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            tuning.Validate();

            foreach (SkillSpec skill in skills ?? throw new ArgumentNullException(nameof(skills)))
            {
                skill.Validate();
                if (!_skills.TryAdd(skill.Id, skill))
                {
                    throw new ContentException("Duplicate skill id " + skill.Id + ".");
                }
            }

            foreach (BodyPartSpec part in bodyParts ?? throw new ArgumentNullException(nameof(bodyParts)))
            {
                part.Validate();
                if (!_bodyParts.TryAdd(part.Id, part))
                {
                    throw new ContentException("Duplicate body part id " + part.Id + ".");
                }

                foreach (string skillId in part.GrantedSkillIds)
                {
                    if (!_skills.ContainsKey(skillId))
                    {
                        throw new ContentException("Body part " + part.Id + " grants unknown skill " + skillId + ".");
                    }
                }
            }

            foreach (DemonSpec demon in demons ?? throw new ArgumentNullException(nameof(demons)))
            {
                demon.Validate();
                if (!_demons.TryAdd(demon.Id, demon))
                {
                    throw new ContentException("Duplicate demon id " + demon.Id + ".");
                }

                if (!_bodyParts.TryGetValue(demon.CoreId, out BodyPartSpec core))
                {
                    throw new ContentException("Demon " + demon.Id + " is born as unknown part " + demon.CoreId + ".");
                }

                if (!core.IsCore)
                {
                    throw new ContentException("Demon " + demon.Id + " is born as " + demon.CoreId + ", which is not a core.");
                }

                foreach (StatValue starting in demon.StartingStats)
                {
                    if (!HasStat(starting.Stat))
                    {
                        throw new ContentException("Demon " + demon.Id + " starts with unknown stat " + starting.Stat + ".");
                    }
                }
            }
        }

        /// <summary>The numbers of the combat and growth rules.</summary>
        public CombatTuning Tuning { get; }

        /// <summary>Every body part kind.</summary>
        public IReadOnlyCollection<BodyPartSpec> BodyParts => _bodyParts.Values;

        /// <summary>Every skill.</summary>
        public IReadOnlyCollection<SkillSpec> Skills => _skills.Values;

        /// <summary>Every demon kind.</summary>
        public IReadOnlyCollection<DemonSpec> Demons => _demons.Values;

        /// <summary>The body part with this id; a missing id is a content error.</summary>
        public BodyPartSpec GetBodyPart(string id)
        {
            return _bodyParts.TryGetValue(id, out BodyPartSpec spec) ? spec : throw new ContentException("Unknown body part " + id + ".");
        }

        /// <summary>The skill with this id; a missing id is a content error.</summary>
        public SkillSpec GetSkill(string id)
        {
            return _skills.TryGetValue(id, out SkillSpec spec) ? spec : throw new ContentException("Unknown skill " + id + ".");
        }

        /// <summary>The demon kind with this id; a missing id is a content error.</summary>
        public DemonSpec GetDemon(string id)
        {
            return _demons.TryGetValue(id, out DemonSpec spec) ? spec : throw new ContentException("Unknown demon " + id + ".");
        }

        /// <summary>Looks a skill up without throwing.</summary>
        public bool TryGetSkill(string id, out SkillSpec? spec)
        {
            return _skills.TryGetValue(id, out spec);
        }

        /// <summary>Looks a body part up without throwing.</summary>
        public bool TryGetBodyPart(string id, out BodyPartSpec? spec)
        {
            return _bodyParts.TryGetValue(id, out spec);
        }

        private bool HasStat(StatId id)
        {
            for (int i = 0; i < Tuning.Stats.Count; i++)
            {
                if (Tuning.Stats[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
