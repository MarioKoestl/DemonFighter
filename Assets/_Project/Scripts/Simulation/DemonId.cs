#nullable enable
using System;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Identifies one demon within a run. Issued by the run and never a Unity instance id, so saves, replays and a
    /// future server all refer to the same demon.
    /// </summary>
    public readonly struct DemonId : IEquatable<DemonId>
    {
        /// <summary>The id no demon ever gets: the default value of the struct.</summary>
        public static readonly DemonId None = default;

        /// <summary>Wraps an id issued by the run; zero means <see cref="None"/>, negatives are programming errors.</summary>
        public DemonId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Demon ids are never negative.");
            }

            Value = value;
        }

        /// <summary>Positive for issued ids, zero for <see cref="None"/>.</summary>
        public int Value { get; }

        /// <summary>True for an id that a run has issued.</summary>
        public bool IsValid => Value > 0;

        /// <inheritdoc />
        public bool Equals(DemonId other) => Value == other.Value;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is DemonId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value;

        /// <inheritdoc />
        public override string ToString() => "Demon " + Value;

        /// <summary>Two ids are the same demon when their values match.</summary>
        public static bool operator ==(DemonId left, DemonId right) => left.Equals(right);

        /// <summary>Two ids are different demons when their values differ.</summary>
        public static bool operator !=(DemonId left, DemonId right) => !left.Equals(right);
    }
}
