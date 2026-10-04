#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Identifies a base stat by its content id, so stats stay data and a new one (a mind stat for magic, D-020)
    /// is an id plus the code that gives it an effect, never an enum edit.
    /// </summary>
    public readonly struct StatId : IEquatable<StatId>
    {
        public StatId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stat id needs a value.", nameof(value));
            }

            Value = value;
        }

        /// <summary>Lowercase dotted id, for example stat.strength.</summary>
        public string Value { get; }

        /// <summary>False for the default value of the struct.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Value);

        /// <inheritdoc />
        public bool Equals(StatId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is StatId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        /// <inheritdoc />
        public override string ToString() => Value ?? string.Empty;

        /// <summary>Same id, same stat.</summary>
        public static bool operator ==(StatId left, StatId right) => left.Equals(right);

        /// <summary>Different ids, different stats.</summary>
        public static bool operator !=(StatId left, StatId right) => !left.Equals(right);
    }
}
