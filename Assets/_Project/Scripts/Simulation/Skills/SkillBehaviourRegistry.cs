#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// Finds every class tagged with <see cref="SkillBehaviourAttribute"/> once and hands out one instance per id
    /// (ARCHITECTURE, "Content pipeline", step 3). Validated against the catalog at run start, so a skill asset naming
    /// an unknown behaviour fails before the first tick.
    /// </summary>
    internal sealed class SkillBehaviourRegistry
    {
        private readonly Dictionary<string, ISkillBehaviour> _behaviours = new Dictionary<string, ISkillBehaviour>(StringComparer.Ordinal);

        /// <summary>Scans the simulation assembly.</summary>
        public SkillBehaviourRegistry()
            : this(typeof(SkillBehaviourRegistry).Assembly)
        {
        }

        /// <summary>Scans the given assembly; tests use it to register fakes.</summary>
        public SkillBehaviourRegistry(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            foreach (Type type in assembly.GetTypes())
            {
                SkillBehaviourAttribute? tag = type.GetCustomAttribute<SkillBehaviourAttribute>();
                if (tag == null)
                {
                    continue;
                }

                if (!typeof(ISkillBehaviour).IsAssignableFrom(type) || type.IsAbstract)
                {
                    throw new ContentException("Skill behaviour " + type.Name + " must implement ISkillBehaviour and be concrete.");
                }

                var behaviour = (ISkillBehaviour)(Activator.CreateInstance(type) ?? throw new ContentException("Could not create " + type.Name + "."));
                if (!_behaviours.TryAdd(tag.Id, behaviour))
                {
                    throw new ContentException("Two skill behaviours share the id " + tag.Id + ".");
                }
            }
        }

        /// <summary>Every registered behaviour id.</summary>
        public IReadOnlyCollection<string> Ids => _behaviours.Keys;

        /// <summary>The behaviour for an id; an unknown id is a content error.</summary>
        public ISkillBehaviour Get(string id)
        {
            return _behaviours.TryGetValue(id, out ISkillBehaviour behaviour) ? behaviour : throw new ContentException("Unknown skill behaviour " + id + ".");
        }

        /// <summary>Throws when any skill in the catalog names a behaviour this registry does not have.</summary>
        public void Validate(ContentCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            foreach (SkillSpec skill in catalog.Skills)
            {
                if (skill.IsPassive)
                {
                    continue;
                }

                if (!_behaviours.ContainsKey(skill.BehaviourId))
                {
                    throw new ContentException("Skill " + skill.Id + " names unknown behaviour " + skill.BehaviourId + ".");
                }
            }
        }
    }
}
