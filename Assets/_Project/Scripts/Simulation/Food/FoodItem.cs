#nullable enable
using System;
using System.Numerics;

namespace DemonFighter.Simulation.Food
{
    /// <summary>
    /// Something edible lying in the world (ARCHITECTURE, "Entities"): a corpse or a severed part with the Biomass
    /// still in it. Eating drains it, time decays it, and it disappears when either reaches zero.
    /// </summary>
    public sealed class FoodItem
    {
        internal FoodItem(FoodId id, FoodKind kind, Vector3 position, float biomass, DemonId source, int sourceTier, long decayTicks)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Food needs an issued id.", nameof(id));
            }

            if (biomass < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(biomass), biomass, "Biomass is never negative.");
            }

            if (decayTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decayTicks), decayTicks, "Food needs a positive decay time.");
            }

            Id = id;
            Kind = kind;
            Position = position;
            BiomassRemaining = biomass;
            Source = source;
            SourceTier = sourceTier;
            DecayTicksLeft = decayTicks;
        }

        public FoodId Id { get; }

        public FoodKind Kind { get; }

        /// <summary>Where it lies; the view may update it after physics settle a severed part.</summary>
        public Vector3 Position { get; private set; }

        /// <summary>Biomass an eater can still take out of it.</summary>
        public float BiomassRemaining { get; private set; }

        /// <summary>The demon it came from.</summary>
        public DemonId Source { get; }

        /// <summary>Tier of the source demon; eating scales by tier difference (D-028).</summary>
        public int SourceTier { get; }

        /// <summary>Ticks until it rots away.</summary>
        public long DecayTicksLeft { get; private set; }

        /// <summary>True when nothing edible is left.</summary>
        public bool IsDepleted => BiomassRemaining <= 0f;

        /// <summary>True when it has rotted away.</summary>
        public bool IsDecayed => DecayTicksLeft <= 0;

        /// <summary>Overwrites the resting position after the Unity body settled; the simulation trusts the view.</summary>
        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        /// <summary>Takes up to the requested Biomass out; returns what was actually taken.</summary>
        internal float Take(float requested)
        {
            if (requested < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(requested), requested, "Cannot take negative Biomass.");
            }

            float taken = MathF.Min(requested, BiomassRemaining);
            BiomassRemaining -= taken;
            return taken;
        }

        /// <summary>Advances rotting by one tick.</summary>
        internal void Decay()
        {
            if (DecayTicksLeft > 0)
            {
                DecayTicksLeft--;
            }
        }
    }
}
