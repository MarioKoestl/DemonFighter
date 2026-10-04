#nullable enable
using System;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Identifies one food item (corpse, severed part, bone pile) within a run. Issued by the run, never a Unity
    /// instance id, for the same reasons as <see cref="DemonId"/>.
    /// </summary>
    public readonly struct FoodId : IEquatable<FoodId>
    {
        /// <summary>The id no food item ever gets: the default value of the struct.</summary>
        public static readonly FoodId None = default;

        /// <summary>Wraps an id issued by the run; zero means <see cref="None"/>, negatives are programming errors.</summary>
        public FoodId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Food ids are never negative.");
            }

            Value = value;
        }

        /// <summary>Positive for issued ids, zero for <see cref="None"/>.</summary>
        public int Value { get; }

        /// <summary>True for an id that a run has issued.</summary>
        public bool IsValid => Value > 0;

        /// <inheritdoc />
        public bool Equals(FoodId other) => Value == other.Value;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is FoodId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value;

        /// <inheritdoc />
        public override string ToString() => "Food " + Value;

        /// <summary>Two ids are the same food item when their values match.</summary>
        public static bool operator ==(FoodId left, FoodId right) => left.Equals(right);

        /// <summary>Two ids are different food items when their values differ.</summary>
        public static bool operator !=(FoodId left, FoodId right) => !left.Equals(right);
    }
}
