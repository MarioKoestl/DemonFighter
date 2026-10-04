#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Anatomy
{
    /// <summary>
    /// The core and the parts plugged into its sockets (ARCHITECTURE, "Entities"; GAME_DESIGN, "The body: parts and
    /// sockets"). Parts are never removed from the list, only marked lost, so a part index stays valid for the whole
    /// run and a lost part keeps its socket until it is regrown. Armor comes from the intact hide part and protects
    /// every part that has no armor of its own. Attached parts contribute stat points, skill bonuses, speed and
    /// perception; everything a part contributes stops while it is lost.
    /// </summary>
    public sealed class Body
    {
        private readonly List<BodyPart> _parts = new List<BodyPart>();
        private readonly BodyRules _rules;
        private float _hpMultiplier;

        /// <summary>Builds a body from its core with HP scaled by the Constitution of the owner.</summary>
        public Body(BodyPartSpec coreSpec, float hpMultiplier, BodyRules rules)
        {
            if (coreSpec == null)
            {
                throw new ArgumentNullException(nameof(coreSpec));
            }

            if (!coreSpec.IsCore)
            {
                throw new ArgumentException("A body starts from a core part, not " + coreSpec.Id + ".", nameof(coreSpec));
            }

            if (hpMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hpMultiplier), hpMultiplier, "The HP multiplier must be positive.");
            }

            _rules = rules;
            _hpMultiplier = hpMultiplier;
            Core = Add(coreSpec);
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

        /// <summary>Body investment for the tier: every part beyond the core, lost or not, plus every upgrade level.</summary>
        public int InvestmentPoints
        {
            get
            {
                int points = 0;
                for (int i = 1; i < _parts.Count; i++)
                {
                    points += 1 + _parts[i].UpgradeLevel;
                }

                return points;
            }
        }

        /// <summary>Fraction added to movement speed by the attached parts; legs replace crawling with walking.</summary>
        public float MoveSpeedBonus
        {
            get
            {
                float bonus = 0f;
                for (int i = 0; i < _parts.Count; i++)
                {
                    if (!_parts[i].IsLost)
                    {
                        bonus += _parts[i].Spec.MoveSpeedBonus;
                    }
                }

                return bonus;
            }
        }

        /// <summary>Fraction added to how far the owner perceives, from sensory parts.</summary>
        public float PerceptionBonus
        {
            get
            {
                float bonus = 0f;
                for (int i = 0; i < _parts.Count; i++)
                {
                    if (!_parts[i].IsLost)
                    {
                        bonus += _parts[i].Spec.PerceptionBonus;
                    }
                }

                return bonus;
            }
        }

        /// <summary>Share of melee damage taken that the attached parts give back to the attacker; spines.</summary>
        public float ReturnDamageFraction
        {
            get
            {
                float fraction = 0f;
                for (int i = 0; i < _parts.Count; i++)
                {
                    if (!_parts[i].IsLost)
                    {
                        fraction += _parts[i].Spec.ReturnDamageFraction;
                    }
                }

                return fraction;
            }
        }

        /// <summary>How many more parts of this socket kind fit; lost parts keep their slot.</summary>
        public int FreeSlots(SocketKind kind)
        {
            int capacity = 0;
            int used = 0;
            for (int i = 0; i < _parts.Count; i++)
            {
                BodyPart part = _parts[i];
                if (part.Spec.Socket == kind)
                {
                    used++;
                }

                if (part.IsLost)
                {
                    continue;
                }

                IReadOnlyList<SocketSlot> sockets = part.Spec.Sockets;
                for (int s = 0; s < sockets.Count; s++)
                {
                    if (sockets[s].Kind == kind)
                    {
                        capacity += sockets[s].Capacity;
                    }
                }
            }

            return Math.Max(0, capacity - used);
        }

        /// <summary>True when the part could be attached now; the reason explains a false.</summary>
        public bool CanAttach(BodyPartSpec spec, out string reason)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            if (spec.IsCore)
            {
                reason = "A body has exactly one core.";
                return false;
            }

            if (FreeSlots(spec.Socket) <= 0)
            {
                reason = "No free " + spec.Socket + " socket.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>Plugs a part into a free socket of its kind; returns it with its stable index.</summary>
        public BodyPart Attach(BodyPartSpec spec)
        {
            if (!CanAttach(spec, out string reason))
            {
                throw new InvalidOperationException("Cannot attach " + spec.Id + ": " + reason);
            }

            return Add(spec);
        }

        /// <summary>Raises a part one upgrade level; its max HP grows and health keeps its fraction.</summary>
        public void Upgrade(BodyPart part)
        {
            Require(part);
            if (part.UpgradeLevel >= part.Spec.MaxUpgrade)
            {
                throw new InvalidOperationException(part.Spec.Id + " is already at upgrade +" + part.UpgradeLevel + ".");
            }

            if (part.IsLost)
            {
                throw new InvalidOperationException("A lost part cannot be upgraded; regrow it first.");
            }

            int level = part.UpgradeLevel + 1;
            part.SetUpgrade(level, MaxHpFor(part.Spec, level));
        }

        /// <summary>Brings a lost part back at full health in its old socket.</summary>
        public void Regrow(BodyPart part)
        {
            Require(part);
            if (!part.IsLost)
            {
                throw new InvalidOperationException(part.Spec.Id + " is not lost.");
            }

            if (part.Spec.IsCore)
            {
                throw new InvalidOperationException("A destroyed core is death, not a lost part.");
            }

            part.Regrow(MaxHpFor(part.Spec, part.UpgradeLevel));
        }

        /// <summary>True while an attached part grants the skill; a severed arm takes Claw with it unless another arm has it.</summary>
        public bool Grants(string skillId)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                BodyPart part = _parts[i];
                if (part.IsLost)
                {
                    continue;
                }

                IReadOnlyList<string> granted = part.Spec.GrantedSkillIds;
                for (int s = 0; s < granted.Count; s++)
                {
                    if (string.Equals(granted[s], skillId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Stat points the attached parts add to one stat; a part gives its per-level value times its level.</summary>
        public int StatBonus(StatId stat)
        {
            int bonus = 0;
            for (int i = 0; i < _parts.Count; i++)
            {
                BodyPart part = _parts[i];
                if (part.IsLost)
                {
                    continue;
                }

                IReadOnlyList<StatValue> bonuses = part.Spec.StatBonusesPerLevel;
                for (int b = 0; b < bonuses.Count; b++)
                {
                    if (bonuses[b].Stat == stat)
                    {
                        bonus += bonuses[b].Value * (part.UpgradeLevel + 1);
                    }
                }
            }

            return bonus;
        }

        /// <summary>Fraction added to the damage of one skill by the attached parts, per level each.</summary>
        public float SkillDamageBonus(string skillId)
        {
            float bonus = 0f;
            for (int i = 0; i < _parts.Count; i++)
            {
                BodyPart part = _parts[i];
                if (part.IsLost)
                {
                    continue;
                }

                IReadOnlyList<SkillBonus> bonuses = part.Spec.SkillDamageBonusesPerLevel;
                for (int b = 0; b < bonuses.Count; b++)
                {
                    if (string.Equals(bonuses[b].SkillId, skillId, StringComparison.Ordinal))
                    {
                        bonus += bonuses[b].DamagePerLevel * (part.UpgradeLevel + 1);
                    }
                }
            }

            return bonus;
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
            if (hpMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hpMultiplier), hpMultiplier, "The HP multiplier must be positive.");
            }

            _hpMultiplier = hpMultiplier;
            for (int i = 0; i < _parts.Count; i++)
            {
                _parts[i].Rescale(MaxHpFor(_parts[i].Spec, _parts[i].UpgradeLevel));
            }
        }

        /// <summary>Adds a saved part at the next index without the socket check a purchase needs (D-073).</summary>
        internal BodyPart AddRestored(BodyPartSpec spec)
        {
            return Add(spec ?? throw new ArgumentNullException(nameof(spec)));
        }

        private BodyPart Add(BodyPartSpec spec)
        {
            var part = new BodyPart(_parts.Count, spec, MaxHpFor(spec, 0), _rules.WoundedThreshold);
            _parts.Add(part);
            return part;
        }

        private float MaxHpFor(BodyPartSpec spec, int upgradeLevel)
        {
            return spec.MaxHp * _hpMultiplier * (1f + _rules.UpgradeHpPerLevel * upgradeLevel);
        }

        private void Require(BodyPart part)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (!HasPart(part.Index) || _parts[part.Index] != part)
            {
                throw new ArgumentException("The part belongs to another body.", nameof(part));
            }
        }
    }
}
