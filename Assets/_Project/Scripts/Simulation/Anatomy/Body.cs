#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Anatomy
{
    /// <summary>
    /// The core and the parts plugged into it (ARCHITECTURE, "Entities"). Parts are never removed from the list,
    /// only marked lost, so a part index stays valid for the whole run. Armor comes from the intact hide part and
    /// protects every part that has no armor of its own.
    /// </summary>
    public sealed class Body
    {
        private readonly List<BodyPart> _parts = new List<BodyPart>();
        private readonly float _woundedThreshold;

        /// <summary>Builds a body from its core with HP scaled by the owner's Constitution.</summary>
        public Body(BodyPartSpec coreSpec, float hpMultiplier, float woundedThreshold)
        {
            if (coreSpec == null)
            {
                throw new ArgumentNullException(nameof(coreSpec));
            }

            if (!coreSpec.IsCore)
            {
                throw new ArgumentException("A body starts from a core part, not " + coreSpec.Id + ".", nameof(coreSpec));
            }

            _woundedThreshold = woundedThreshold;
            Core = AddPart(coreSpec, hpMultiplier);
        }

        /// <summary>The root part; losing it is death.</summary>
        public BodyPart Core { get; }

        /// <summary>Every part including the core, in attachment order.</summary>
        public IReadOnlyList<BodyPart> Parts => _parts;

        /// <summary>True once the core is gone.</summary>
        public bool IsCoreDestroyed => Core.IsLost;

        /// <summary>Current HP of all parts that are not lost; what the HUD shows as Health.</summary>
        public float TotalHp
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _parts.Count; i++)
                {
                    total += _parts[i].Hp;
                }

                return total;
            }
        }

        /// <summary>Maximum HP of all parts that are not lost.</summary>
        public float TotalMaxHp
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _parts.Count; i++)
                {
                    if (!_parts[i].IsLost)
                    {
                        total += _parts[i].MaxHp;
                    }
                }

                return total;
            }
        }

        /// <summary>Attaches a new part; returns it with its stable index.</summary>
        public BodyPart AddPart(BodyPartSpec spec, float hpMultiplier)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            if (_parts.Count > 0 && spec.IsCore)
            {
                throw new ArgumentException("A body has exactly one core.", nameof(spec));
            }

            var part = new BodyPart(_parts.Count, spec, spec.MaxHp * hpMultiplier, _woundedThreshold);
            _parts.Add(part);
            return part;
        }

        /// <summary>The part at this index; an index the body never issued is a programming error.</summary>
        public BodyPart GetPart(int index)
        {
            if (index < 0 || index >= _parts.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "No part with this index.");
            }

            return _parts[index];
        }

        /// <summary>True when a part with this index exists; the hit handler uses it to reject bad reports.</summary>
        public bool HasPart(int index)
        {
            return index >= 0 && index < _parts.Count;
        }

        /// <summary>
        /// The armor a hit on this part meets: the part's own defense, else the intact hide part's, else none
        /// (GAME_DESIGN, "Damage model").
        /// </summary>
        public DefenseType EffectiveDefense(BodyPart target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.Spec.Defense != DefenseType.None)
            {
                return target.Spec.Defense;
            }

            for (int i = 0; i < _parts.Count; i++)
            {
                BodyPart part = _parts[i];
                if (part.Spec.Socket == SocketKind.Hide && !part.IsLost && part.Spec.Defense != DefenseType.None)
                {
                    return part.Spec.Defense;
                }
            }

            return DefenseType.None;
        }

        /// <summary>Re-applies a changed Constitution multiplier to every part, keeping health fractions.</summary>
        internal void RescaleHp(float hpMultiplier)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                _parts[i].Rescale(_parts[i].Spec.MaxHp * hpMultiplier);
            }
        }
    }
}
