#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Display data of one base stat: what the Stats tab shows next to the number. The effect is code keyed by id.
    /// </summary>
    public sealed record StatSpec
    {
        public StatSpec(StatId id, string name, string description)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A stat spec needs an id.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A stat spec needs a name.", nameof(name));
            }

            Id = id;
            Name = name;
            Description = description ?? string.Empty;
        }

        public StatId Id { get; }

        public string Name { get; }

        public string Description { get; }
    }
}
