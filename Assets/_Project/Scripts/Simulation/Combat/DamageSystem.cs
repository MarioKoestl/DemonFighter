#nullable enable
using System;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Progression;

namespace DemonFighter.Simulation.Combat
{
    /// <summary>
    /// The damage rules (GAME_DESIGN, "Damage model", D-022): a hit lands on one part, Strength and skill level scale
    /// it, the armor the part wears multiplies it, Pierce and Cut wounds bleed, Blunt staggers, a part at zero is
    /// severed or destroyed, a core at zero is death and a corpse. Every outcome leaves as an event.
    /// </summary>
    internal sealed class DamageSystem
    {
        private readonly RunState _state;
        private readonly SimulationEvents _events;

        public DamageSystem(RunState state, SimulationEvents events)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        private CombatTuning Tuning => _state.Catalog.Tuning;

        /// <summary>A landed skill hit with everything the skill brings: damage, bleeding and stagger.</summary>
        public void ApplyHit(Demon attacker, Demon target, BodyPart part, SkillSpec skill, int skillLevel)
        {
            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            if (skillLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(skillLevel), skillLevel, "Skill levels start at 1.");
            }

            if (!target.IsAlive || part.IsLost)
            {
                return;
            }

            float levelBonus = 1f + skill.DamageBonusPerLevel * (skillLevel - 1);
            float armor = Tuning.Multiplier(skill.DamageType, target.Body.EffectiveDefense(part));
            float amount = skill.BaseDamage * levelBonus * attacker.Derived.DamageMultiplier * armor;
            bool alive = ApplyDamage(target, part, amount, skill.DamageType, attacker.Id);
            if (!alive)
            {
                return;
            }

            if (skill.BleedSeconds > 0f && !part.IsLost)
            {
                part.StartBleeding(skill.BleedSeconds * target.Derived.BleedDurationFactor, skill.BleedDamagePerSecond, skill.DamageType);
            }

            // Blunt hits interrupt actions (GAME_DESIGN, "Stagger"): the running skill is lost, the stamina stays spent.
            if (skill.StaggerSeconds > 0f)
            {
                target.Stagger(_state.Tick + _state.Config.TicksFor(skill.StaggerSeconds));
                target.ClearSkillUse();
            }
        }

        /// <summary>
        /// Damage to one part from any source. Marks both sides in combat, reports it, handles a lost part and a
        /// destroyed core. Returns true while the target still lives.
        /// </summary>
        public bool ApplyDamage(Demon target, BodyPart part, float amount, DamageType type, DemonId attacker)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            if (!target.IsAlive || part.IsLost || amount <= 0f)
            {
                return target.IsAlive;
            }

            bool wasWounded = part.Condition == PartCondition.Wounded;
            bool lost = part.ApplyDamage(amount);
            _events.Publish(new DamageApplied(target.Id, part.Index, amount, type, attacker));
            target.MarkCombat(_state.Tick);
            if (attacker.IsValid)
            {
                // A hit interrupts a meal (GAME_DESIGN, Eat is interruptible); bleeding is no new hit and does not.
                target.StopEating();
                target.MarkAttackedBy(attacker, _state.Tick);
                if (_state.TryGetDemon(attacker, out Demon? attackerDemon))
                {
                    attackerDemon.MarkCombat(_state.Tick);
                }
            }

            if (!lost)
            {
                if (!wasWounded && part.Condition == PartCondition.Wounded)
                {
                    _events.Publish(new PartWounded(target.Id, part.Index));
                }

                return true;
            }

            if (part.Spec.IsCore)
            {
                Kill(target, attacker);
                return false;
            }

            if (part.Spec.Fate == PartFate.Severed)
            {
                float biomass = part.Spec.BiomassValue * (target.Tier + 1);
                FoodItem food = _state.SpawnFood(FoodKind.SeveredPart, target.Position, biomass, target.Id, target.Tier);
                _events.Publish(new PartSevered(target.Id, part.Index, food.Id));
                _events.Publish(new FoodSpawned(food.Id));
            }
            else
            {
                _events.Publish(new PartDestroyed(target.Id, part.Index));
            }

            return true;
        }

        // The corpse carries the tier value plus whatever Biomass the dead demon had not spent (D-012). The killer
        // gets the kill XP scaled by the tier gap (GAME_DESIGN, "Reward scaling").
        private void Kill(Demon target, DemonId killer)
        {
            target.Die();
            float biomass = Tuning.CorpseBiomassPerTier * (target.Tier + 1) + target.Biomass;
            FoodItem corpse = _state.SpawnFood(FoodKind.Corpse, target.Position, biomass, target.Id, target.Tier);
            _events.Publish(new DemonDied(target.Id, killer, corpse.Id));
            _events.Publish(new FoodSpawned(corpse.Id));
            if (killer.IsValid && killer != target.Id && _state.TryGetDemon(killer, out Demon? killerDemon))
            {
                killerDemon.RecordKill();
                float xp = Tuning.KillXpBase * (target.Tier + 1) * Tuning.RewardFactor(killerDemon.Tier, target.Tier);
                XpSystem.Grant(killerDemon, xp, XpSource.Kill, Tuning, _events);
            }
        }
    }
}
