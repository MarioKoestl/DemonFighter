#nullable enable
using System;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Anatomy
{
    /// <summary>
    /// One part of a body with its own hit points (GAME_DESIGN, "The body: parts and sockets"). Attacks hit parts,
    /// not the demon; a part at zero HP is lost for good, and the core being lost is death.
    /// </summary>
    public sealed class BodyPart
    {
        private readonly float _woundedThreshold;

        internal BodyPart(int index, BodyPartSpec spec, float maxHp, float woundedThreshold)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Part indices are never negative.");
            }

            if (maxHp <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), maxHp, "A part needs positive HP.");
            }

            Index = index;
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            MaxHp = maxHp;
            Hp = maxHp;
            _woundedThreshold = woundedThreshold;
        }

        /// <summary>Position in the body's part list; stable for the life of the demon, so hits can name a part.</summary>
        public int Index { get; }

        public BodyPartSpec Spec { get; }

        /// <summary>Hit points after Constitution scaling.</summary>
        public float MaxHp { get; private set; }

        public float Hp { get; private set; }

        /// <summary>True once the part reached zero HP; it stays lost until a regrow mutation replaces it.</summary>
        public bool IsLost { get; private set; }

        /// <summary>Seconds this part keeps bleeding; zero when it does not.</summary>
        public float BleedSecondsLeft { get; private set; }

        /// <summary>HP per second the bleeding drains.</summary>
        public float BleedDamagePerSecond { get; private set; }

        /// <summary>The damage type of the wound that bleeds, reported with each drain.</summary>
        public DamageType BleedType { get; private set; }

        /// <summary>True while the part loses HP to a wound.</summary>
        public bool IsBleeding => !IsLost && BleedSecondsLeft > 0f && BleedDamagePerSecond > 0f;

        /// <summary>Healthy above the wounded threshold, Wounded below it, Lost at zero.</summary>
        public PartCondition Condition
        {
            get
            {
                if (IsLost)
                {
                    return PartCondition.Lost;
                }

                return Hp < MaxHp * _woundedThreshold ? PartCondition.Wounded : PartCondition.Healthy;
            }
        }

        /// <summary>Takes damage; returns true when this blow lost the part.</summary>
        internal bool ApplyDamage(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Damage is never negative.");
            }

            if (IsLost)
            {
                return false;
            }

            Hp = MathF.Max(0f, Hp - amount);
            if (Hp > 0f)
            {
                return false;
            }

            IsLost = true;
            return true;
        }

        /// <summary>Regenerates HP up to the maximum; lost parts do not heal (D-024).</summary>
        internal void Heal(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Healing is never negative.");
            }

            if (!IsLost)
            {
                Hp = MathF.Min(MaxHp, Hp + amount);
            }
        }

        /// <summary>
        /// Opens or refreshes a bleeding wound: a new wound never shortens the bleeding or weakens the drain
        /// (GAME_DESIGN, "Bleeding").
        /// </summary>
        internal void StartBleeding(float seconds, float damagePerSecond, DamageType type)
        {
            if (seconds <= 0f || damagePerSecond <= 0f || IsLost)
            {
                return;
            }

            BleedSecondsLeft = MathF.Max(BleedSecondsLeft, seconds);
            BleedDamagePerSecond = MathF.Max(BleedDamagePerSecond, damagePerSecond);
            BleedType = type;
        }

        /// <summary>Advances the bleeding clock and returns the HP this step drains; zero when not bleeding.</summary>
        internal float AdvanceBleeding(float seconds)
        {
            if (!IsBleeding)
            {
                return 0f;
            }

            float step = MathF.Min(seconds, BleedSecondsLeft);
            float drain = BleedDamagePerSecond * step;
            BleedSecondsLeft -= step;
            if (BleedSecondsLeft <= 0f)
            {
                BleedSecondsLeft = 0f;
                BleedDamagePerSecond = 0f;
            }

            return drain;
        }

        /// <summary>Changes the maximum after a Constitution change, keeping the same fraction of health.</summary>
        internal void Rescale(float newMaxHp)
        {
            if (newMaxHp <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(newMaxHp), newMaxHp, "A part needs positive HP.");
            }

            float fraction = Hp / MaxHp;
            MaxHp = newMaxHp;
            Hp = IsLost ? 0f : fraction * newMaxHp;
        }
    }
}
