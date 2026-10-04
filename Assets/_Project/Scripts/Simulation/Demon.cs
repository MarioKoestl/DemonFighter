#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Stats;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// One demon in a run, player or AI alike (ARCHITECTURE, "Entities"). Holds where it is, what body it has, its
    /// stats and resources, and what it is trying to do; changed only by the simulation systems and by the pose the
    /// view writes back after physics. Skills and status effects join with their systems.
    /// </summary>
    public sealed class Demon
    {
        internal Demon(DemonId id, ControllerKind controller, DemonSpec spec, ContentCatalog catalog, Vector3 position, float yaw)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A demon needs an issued id.", nameof(id));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            Id = id;
            Controller = controller;
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            SizeMeters = spec.SizeMeters;
            Position = position;
            Yaw = yaw;

            Stats = new BaseStats(catalog.Tuning.Stats);
            for (int i = 0; i < spec.StartingStats.Count; i++)
            {
                Stats.Set(spec.StartingStats[i].Stat, spec.StartingStats[i].Value);
            }

            Derived = DerivedStats.From(Stats, catalog.Tuning);
            Body = new Body(catalog.GetBodyPart(spec.CoreId), Derived.HpMultiplier, catalog.Tuning.WoundedThreshold);
            Stamina = Derived.MaxStamina;
            Level = 1;

            var skills = new List<SkillInstance>();
            for (int p = 0; p < Body.Parts.Count; p++)
            {
                BodyPart part = Body.Parts[p];
                for (int s = 0; s < part.Spec.GrantedSkillIds.Count; s++)
                {
                    skills.Add(new SkillInstance(catalog.GetSkill(part.Spec.GrantedSkillIds[s]), part.Index));
                }
            }

            Skills = skills;
        }

        /// <summary>Run-wide identity, issued by the run.</summary>
        public DemonId Id { get; }

        /// <summary>Who sends this demon's commands.</summary>
        public ControllerKind Controller { get; }

        /// <summary>The kind this demon was spawned as.</summary>
        public DemonSpec Spec { get; }

        /// <summary>Current body height in meters; grows with evolutions later, so it is state, not spec.</summary>
        public float SizeMeters { get; private set; }

        /// <summary>Tier label shown in the HUD and used for reward scaling; derived from body investment later.</summary>
        public int Tier => Spec.Tier;

        /// <summary>Allocated base stat points and unspent points.</summary>
        public BaseStats Stats { get; }

        /// <summary>What the stats mean in play; recomputed when a stat changes.</summary>
        public DerivedStats Derived { get; private set; }

        /// <summary>Core and parts with their HP.</summary>
        public Body Body { get; }

        /// <summary>Current stamina; skills spend it, time refills it (D-023).</summary>
        public float Stamina { get; private set; }

        /// <summary>Biomass eaten and not yet spent; lost on death (D-012).</summary>
        public float Biomass { get; private set; }

        /// <summary>Character level; starts at 1 and rises with XP (D-037).</summary>
        public int Level { get; private set; }

        /// <summary>XP gathered toward the next level.</summary>
        public float Xp { get; private set; }

        /// <summary>The skills this demon's parts grant, in attachment order.</summary>
        public IReadOnlyList<SkillInstance> Skills { get; }

        /// <summary>The skill being carried out right now, or null when free.</summary>
        public SkillUse? CurrentSkillUse { get; private set; }

        /// <summary>The food this demon is eating right now; None when it is not eating.</summary>
        public FoodId EatingFoodId { get; private set; } = FoodId.None;

        /// <summary>Tick of the last accepted eat command; a tick without one ends the meal.</summary>
        public long EatRequestTick { get; private set; } = -1;

        /// <summary>True while a meal is in progress; AI prefers eating prey.</summary>
        public bool IsEating => EatingFoodId.IsValid;

        /// <summary>Total Biomass eaten in the run, for the summary.</summary>
        public float BiomassEaten { get; private set; }

        /// <summary>False once the core is destroyed; a dead demon is a corpse and takes no commands.</summary>
        public bool IsAlive => !Body.IsCoreDestroyed;

        /// <summary>Tick until which a Blunt hit keeps this demon from acting; -1 when never staggered.</summary>
        public long StaggeredUntilTick { get; private set; } = -1;

        /// <summary>Tick of the last damage dealt or taken; -1 before the first (D-014 out-of-combat rule).</summary>
        public long LastCombatTick { get; private set; } = -1;

        /// <summary>The demon that last hit this one; None before the first hit. Elders hunt what provokes them.</summary>
        public DemonId LastAttackedBy { get; private set; } = DemonId.None;

        /// <summary>Tick of the last hit taken; -1 before the first.</summary>
        public long LastAttackedTick { get; private set; } = -1;

        /// <summary>Demons this one killed in the run, for the summary.</summary>
        public int Kills { get; private set; }

        /// <summary>True while a stagger still runs at the given tick.</summary>
        public bool IsStaggered(long tick)
        {
            return tick < StaggeredUntilTick;
        }

        /// <summary>True when damage was dealt or taken within the window, counted in ticks.</summary>
        public bool IsInCombat(long tick, int windowTicks)
        {
            return LastCombatTick >= 0 && tick - LastCombatTick < windowTicks;
        }

        /// <summary>True when this demon took a hit within the window, counted in ticks.</summary>
        public bool WasAttackedWithin(long tick, int windowTicks)
        {
            return LastAttackedTick >= 0 && tick - LastAttackedTick < windowTicks;
        }

        /// <summary>Feet position in world space, X east, Y up, Z north.</summary>
        public Vector3 Position { get; private set; }

        /// <summary>Facing angle in radians around the up axis, zero toward north, increasing clockwise (Unity yaw).</summary>
        public float Yaw { get; private set; }

        /// <summary>What the demon is trying to do with its legs this tick.</summary>
        public MovementIntent Intent { get; private set; }

        /// <summary>
        /// True while a Unity body moves this demon. The simulation then stops integrating its position and trusts
        /// the pose the view writes back; without a body (tests, a headless server) it integrates itself.
        /// </summary>
        public bool HasBody { get; private set; }

        /// <summary>Top speed for the current intent: walking speed, the sprint factor while sprinting, Agility on top.</summary>
        public float MaxSpeed
        {
            get
            {
                float baseSpeed = Intent.Sprint ? Spec.MoveSpeed * Spec.SprintMultiplier : Spec.MoveSpeed;
                return baseSpeed * Derived.MoveSpeedMultiplier;
            }
        }

        /// <summary>Ground-plane velocity the body should move with: intent direction scaled by the top speed.</summary>
        public Vector3 Velocity
        {
            get
            {
                Vector2 planar = Intent.Direction * MaxSpeed;
                return new Vector3(planar.X, 0f, planar.Y);
            }
        }

        /// <summary>The direction the demon faces on the ground plane, derived from the yaw.</summary>
        public Vector2 FacingDirection => new Vector2(MathF.Sin(Yaw), MathF.Cos(Yaw));

        /// <summary>
        /// Overwrites position and facing with what the Unity body ended up with after collisions. The simulation
        /// trusts the view here (ARCHITECTURE, "Movement, collision and hits"); nothing else may call it.
        /// </summary>
        public void SetPose(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
        }

        /// <summary>Yaw that faces the given ground-plane direction; the current yaw when the direction is zero.</summary>
        public float YawToward(Vector2 direction)
        {
            return direction.LengthSquared() > 0f ? MathF.Atan2(direction.X, direction.Y) : Yaw;
        }

        /// <summary>Called by the view that takes over moving this demon; a second body is a wiring bug.</summary>
        public void AttachBody()
        {
            if (HasBody)
            {
                throw new InvalidOperationException(Id + " already has a body.");
            }

            HasBody = true;
        }

        /// <summary>Called by the view when it is destroyed; the simulation integrates the demon again.</summary>
        public void DetachBody()
        {
            HasBody = false;
        }

        internal void SetIntent(MovementIntent intent)
        {
            Intent = intent;
        }

        /// <summary>Records that this demon dealt or took damage at the given tick.</summary>
        internal void MarkCombat(long tick)
        {
            LastCombatTick = tick;
        }

        /// <summary>Remembers who hit this demon.</summary>
        internal void MarkAttackedBy(DemonId attacker, long tick)
        {
            LastAttackedBy = attacker;
            LastAttackedTick = tick;
        }

        /// <summary>Extends the stagger to the given tick; a shorter new stagger never cuts a running one.</summary>
        internal void Stagger(long untilTick)
        {
            StaggeredUntilTick = Math.Max(StaggeredUntilTick, untilTick);
        }

        /// <summary>Counts a kill for the run summary.</summary>
        internal void RecordKill()
        {
            Kills++;
        }

        /// <summary>Called when the core is destroyed: the body stops wanting anything.</summary>
        internal void Die()
        {
            Intent = MovementIntent.None;
            CurrentSkillUse = null;
            EatingFoodId = FoodId.None;
        }

        /// <summary>The demon's instance of a skill by content id, or null when no part grants it.</summary>
        public SkillInstance? FindSkill(string skillId)
        {
            for (int i = 0; i < Skills.Count; i++)
            {
                if (string.Equals(Skills[i].Spec.Id, skillId, StringComparison.Ordinal))
                {
                    return Skills[i];
                }
            }

            return null;
        }

        /// <summary>Begins carrying out a skill; a meal in progress ends.</summary>
        internal void StartSkillUse(SkillUse use)
        {
            CurrentSkillUse = use ?? throw new ArgumentNullException(nameof(use));
            EatingFoodId = FoodId.None;
        }

        /// <summary>Ends the current skill use, after recovery or when a stagger interrupts it.</summary>
        internal void ClearSkillUse()
        {
            CurrentSkillUse = null;
        }

        /// <summary>Records an accepted eat command for this tick; the eating stage moves the Biomass.</summary>
        internal void RequestEat(FoodId food, long tick)
        {
            EatingFoodId = food;
            EatRequestTick = tick;
        }

        /// <summary>Ends the meal: the key was released, the food is gone, or a hit interrupted it.</summary>
        internal void StopEating()
        {
            EatingFoodId = FoodId.None;
        }

        /// <summary>
        /// Advances position along the intent and turns the body toward the given facing or, without one, toward
        /// the movement; standing still without a facing changes nothing.
        /// </summary>
        internal void Integrate(float seconds)
        {
            if (Intent.HasFacing)
            {
                Yaw = YawToward(Intent.Facing);
            }

            if (!Intent.IsMoving)
            {
                return;
            }

            Position += Velocity * seconds;
            if (!Intent.HasFacing)
            {
                Yaw = YawToward(Intent.Direction);
            }
        }

        /// <summary>Recomputes the derived values after a stat change and rescales body HP and stamina with them.</summary>
        internal void RecomputeDerived(CombatTuning tuning)
        {
            Derived = DerivedStats.From(Stats, tuning);
            Body.RescaleHp(Derived.HpMultiplier);
            Stamina = MathF.Min(Stamina, Derived.MaxStamina);
        }

        /// <summary>Spends stamina; false and unchanged when there is not enough.</summary>
        internal bool TrySpendStamina(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Stamina costs are never negative.");
            }

            if (Stamina < amount)
            {
                return false;
            }

            Stamina -= amount;
            return true;
        }

        /// <summary>Refills stamina up to the maximum.</summary>
        internal void RegenerateStamina(float amount)
        {
            Stamina = MathF.Min(Derived.MaxStamina, Stamina + amount);
        }

        /// <summary>Adds eaten Biomass.</summary>
        internal void GainBiomass(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Biomass gains are never negative.");
            }

            Biomass += amount;
            BiomassEaten += amount;
        }

        /// <summary>Adds XP and returns how many levels it bought, granting the stat points for each.</summary>
        internal int GainXp(float amount, CombatTuning tuning)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "XP gains are never negative.");
            }

            Xp += amount;
            int levelsGained = 0;
            while (Xp >= tuning.LevelXpForNext(Level))
            {
                Xp -= tuning.LevelXpForNext(Level);
                Level++;
                levelsGained++;
                Stats.GrantPoints(tuning.StatPointsPerLevel);
            }

            return levelsGained;
        }
    }
}
