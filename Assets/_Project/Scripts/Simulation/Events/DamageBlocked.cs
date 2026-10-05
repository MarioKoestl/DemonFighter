#nullable enable
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Events
{
    /// <summary>
    /// Damage a demon in test mode did not take (D-089): what the hit, the bleeding or the fire would have done. Only
    /// the damage numbers read it, so a playtest still shows what an enemy deals (D-092). The attacker is
    /// <see cref="DemonId.None"/> for damage without a culprit, such as fire.
    /// </summary>
    public readonly struct DamageBlocked : ISimulationEvent
    {
        public DamageBlocked(DemonId target, int partIndex, float amount, DamageType damageType, DemonId attacker)
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
