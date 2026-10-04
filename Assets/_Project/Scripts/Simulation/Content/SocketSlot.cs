#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>A socket a part exposes and how many parts fit into it (GAME_DESIGN, "The body: parts and sockets").</summary>
    public readonly struct SocketSlot : IEquatable<SocketSlot>
    {
        public SocketSlot(SocketKind kind, int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A socket holds at least one part.");
            }

            Kind = kind;
            Capacity = capacity;
        }

        public SocketKind Kind { get; }

        /// <summary>How many parts of this socket kind fit; the limb socket of a blob holds two arms.</summary>
        public int Capacity { get; }

        public bool Equals(SocketSlot other) => Kind == other.Kind && Capacity == other.Capacity;

        public override bool Equals(object? obj) => obj is SocketSlot other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, Capacity);

        public static bool operator ==(SocketSlot left, SocketSlot right) => left.Equals(right);

        public static bool operator !=(SocketSlot left, SocketSlot right) => !left.Equals(right);
    }
}
