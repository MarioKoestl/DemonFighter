#nullable enable
using System;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// Tags a behaviour class with the id a skill asset names in its BehaviourId. The registry finds tagged classes
    /// by scanning, so adding a skill behaviour never touches a list (CLAUDE.md rule 3).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class SkillBehaviourAttribute : Attribute
    {
        public SkillBehaviourAttribute(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A behaviour id is required.", nameof(id));
            }

            Id = id;
        }

        public string Id { get; }
    }
}
