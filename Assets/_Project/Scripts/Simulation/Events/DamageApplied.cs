#nullable enable
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// A part took damage. Presentation draws the wound and the hit reaction, the HUD updates Health. The attacker is
    /// <see cref="DemonId.None"/> for damage without a culprit, such as bleeding.
    /// </summary>
    public readonly struct DamageApplied : ISimulationEvent
    {
        public DamageApplied(DemonId target, int partIndex, float amount, DamageType damageType, DemonId attacker)
        {
            Target = target;
            PartIndex = partIndex;
            Amount = amount;
            DamageType = damageType;
            Attacker = attacker;
        }

        public DemonId Target { get; }

        public int PartIndex { get; }

        public float Amount { get; }

        public DamageType DamageType { get; }

        public DemonId Attacker { get; }
    }
}
