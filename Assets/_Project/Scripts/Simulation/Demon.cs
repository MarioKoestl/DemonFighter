#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Evolution;
using DemonFighter.Simulation.Persistence;
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
        private readonly ContentCatalog _catalog;
        private readonly CombatTuning _tuning;
        private readonly List<SkillInstance> _skills = new List<SkillInstance>();
        private readonly HashSet<string> _unlockedPartIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<StatId, int> _capBonuses = new Dictionary<StatId, int>();
        private float _sprintXpBuffer;
        private readonly List<string> _evolutionIds = new List<string>();

        /// <summary>Below this stamina a sprint turns into a walk, so an empty bar never sprints for free.</summary>
        private const float MinSprintStamina = 1f;

        /// <summary>A held demon is pulled in until the bodies touch: half the sum of the two sizes (D-061).</summary>
        private const float HoldGapPerSize = 0.5f;

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

            _catalog = catalog;
            _tuning = catalog.Tuning;
            Derived = DerivedStats.From(Stats, catalog.Tuning);
            Body = new Body(catalog.GetBodyPart(spec.CoreId), Derived.HpMultiplier, BodyRules.From(catalog.Tuning));
            Stamina = Derived.MaxStamina;
            Level = 1;
            AddSkillsFrom(Body.Core);
            RecomputeSize();
        }

        /// <summary>Run-wide identity, issued by the run.</summary>
        public DemonId Id { get; }

        /// <summary>Who sends this demon's commands.</summary>
        public ControllerKind Controller { get; }

        /// <summary>The kind this demon was spawned as.</summary>
        public DemonSpec Spec { get; }

        /// <summary>Current body height in meters: the spawn size grown by a step per tier gained (D-018).</summary>
        public float SizeMeters { get; private set; }

        /// <summary>Tier label (GAME_DESIGN, "Derived values"): the spawn tier plus evolutions plus one per few points of body investment.</summary>
        public int Tier => TierFor(Spec, Evolutions, Body.InvestmentPoints, _tuning);

        /// <summary>Factor on the reach of hits and eating: sensory parts such as Eyes let the demon use the far end of its range (D-058).</summary>
        public float ReachMultiplier => 1f + Body.PerceptionBonus;

        /// <summary>How much the demon learns about what it aims at (D-066): one level per PerceptionPerSenseLevel of perception bonus.</summary>
        public int SenseLevel => _tuning.PerceptionPerSenseLevel > 0f ? (int)MathF.Floor(Body.PerceptionBonus / _tuning.PerceptionPerSenseLevel + 0.0001f) : 0;

        /// <summary>The tier a demon of this kind has with the given evolutions and body investment (D-054); the menu previews with it.</summary>
        public static int TierFor(DemonSpec spec, int evolutions, int investmentPoints, CombatTuning tuning)
        {
            return spec.Tier + evolutions + investmentPoints / tuning.TierInvestmentStep;
        }

        /// <summary>Body height in meters of a demon of this kind at the given tier (D-018).</summary>
        public static float SizeFor(DemonSpec spec, int tier, CombatTuning tuning)
        {
            return spec.SizeMeters * (1f + tuning.SizeStepPerTier * (tier - spec.Tier));
        }

        /// <summary>Evolutions taken in this run; each raises the tier by one.</summary>
        public int Evolutions { get; private set; }

        /// <summary>The highest tier reached in this run (D-075); the run summary reports it.</summary>
        public int HighestTier { get; private set; }

        /// <summary>Ids of the evolutions taken, in order.</summary>
        public IReadOnlyList<string> EvolutionIds => _evolutionIds;

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

        /// <summary>Every skill a part ever granted, in arrival order; a lost part leaves its skill in place, unusable until regrown.</summary>
        public IReadOnlyList<SkillInstance> Skills => _skills;

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

        /// <summary>First tick the demon acts and can be hurt again after a mutation; -1 when not transforming (D-014).</summary>
        public long TransformingUntilTick { get; private set; } = -1;

        /// <summary>Tick the current transformation began; -1 when not transforming. Mutations of one tick share it (D-064).</summary>
        public long TransformationStartedTick { get; private set; } = -1;

        /// <summary>First tick the demon moves again after a grab; -1 when not held.</summary>
        public long HeldUntilTick { get; private set; } = -1;

        /// <summary>The demon whose grab holds this one; None when not held. The holder drags it along (D-061).</summary>
        public DemonId HeldBy { get; private set; } = DemonId.None;

        /// <summary>Where the held demon rides in the frame of its holder: meters to the right and ahead.</summary>
        public Vector2 HeldOffset { get; private set; }

        /// <summary>Velocity a dash or knockback adds on top of the intent until the tick below.</summary>
        public Vector2 ExternalVelocity { get; private set; }

        /// <summary>First tick the push is over; -1 when nothing pushes.</summary>
        public long ExternalVelocityUntilTick { get; private set; } = -1;

        /// <summary>True while a grab holds this demon; its holder drags it along (D-061).</summary>
        public bool IsHeld(long tick)
        {
            return HeldUntilTick >= 0 && tick < HeldUntilTick;
        }

        /// <summary>True when an attached part grants a sprint skill (Legs).</summary>
        public bool CanSprint => SprintSkill() != null;

        /// <summary>True while the sprint key is held, a sprint skill is granted and stamina is left for it.</summary>
        public bool IsSprinting => Intent.Sprint && Stamina >= MinSprintStamina && CanSprint;

        /// <summary>The granted passive skill that enables sprinting, or null.</summary>
        public SkillInstance? SprintSkill()
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].Spec.EnablesSprint && _skills[i].IsGrantedBy(Body))
                {
                    return _skills[i];
                }
            }

            return null;
        }

        /// <summary>Part kinds an evolution unlocked for buying.</summary>
        public IReadOnlyCollection<string> UnlockedPartIds => _unlockedPartIds;

        /// <summary>True while a mutation or evolution reshapes the body: invulnerable and unable to act.</summary>
        public bool IsTransforming(long tick)
        {
            return TransformingUntilTick >= 0 && tick < TransformingUntilTick;
        }

        /// <summary>True when the part may be bought: it needs no unlock, or an evolution gave it.</summary>
        public bool IsUnlocked(BodyPartSpec spec)
        {
            return !spec.RequiresUnlock || _unlockedPartIds.Contains(spec.Id);
        }

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

        /// <summary>Top speed for the current intent: walking speed, the sprint factor while sprinting, Agility and legs on top.</summary>
        public float MaxSpeed
        {
            get
            {
                float baseSpeed = IsSprinting ? Spec.MoveSpeed * Spec.SprintMultiplier : Spec.MoveSpeed;
                return baseSpeed * Derived.MoveSpeedMultiplier * (1f + Body.MoveSpeedBonus);
            }
        }

        /// <summary>Ground-plane velocity the body should move with: intent direction scaled by the top speed.</summary>
        public Vector3 Velocity
        {
            get
            {
                Vector2 planar = Intent.Direction * MaxSpeed + ExternalVelocity;
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

        /// <summary>Makes a locked part kind buyable; evolutions call it.</summary>
        internal void UnlockPart(string partId)
        {
            _unlockedPartIds.Add(partId);
        }

        /// <summary>Highest allocation a stat may reach: the base cap plus what evolutions raised it by.</summary>
        public int StatCap(StatId stat)
        {
            return _tuning.BaseStatCap + (_capBonuses.TryGetValue(stat, out int bonus) ? bonus : 0);
        }

        /// <summary>Raises the allocation cap of a stat; evolutions call it.</summary>
        internal void RaiseStatCap(StatId stat, int by)
        {
            _capBonuses[stat] = (_capBonuses.TryGetValue(stat, out int bonus) ? bonus : 0) + by;
        }

        /// <summary>Gives the bound stat points of an evolution package (D-067): the stat and its cap rise together, so the gain always fits.</summary>
        internal void GrantStat(StatId stat, int points)
        {
            if (points <= 0 || !Stats.Has(stat))
            {
                return;
            }

            RaiseStatCap(stat, points);
            Stats.Set(stat, Stats.Get(stat) + points);
            RecomputeDerived(_tuning);
        }

        /// <summary>Grants a skill no part carries; an evolution gives it and nothing takes it away.</summary>
        internal void GrantSkill(SkillSpec spec)
        {
            if (FindSkill(spec.Id) == null)
            {
                _skills.Add(new SkillInstance(spec, grantedByEvolution: true));
            }
        }

        /// <summary>Begins a transformation at this tick: nothing moves, eats or attacks until the until tick, and no damage lands.</summary>
        internal void StartTransformation(long startTick, long untilTick)
        {
            TransformationStartedTick = startTick;
            TransformingUntilTick = untilTick;
            Intent = MovementIntent.None;
            CurrentSkillUse = null;
            EatingFoodId = FoodId.None;
        }

        internal void EndTransformation()
        {
            TransformingUntilTick = -1;
            TransformationStartedTick = -1;
        }

        /// <summary>Pays Biomass for a mutation; paying more than there is would be a rules bug.</summary>
        internal void SpendBiomass(float amount)
        {
            if (amount < 0f || amount > Biomass)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cannot spend more Biomass than there is.");
            }

            Biomass -= amount;
        }

        /// <summary>
        /// Grabbed: nothing moves, attacks or eats on its own until the tick, and the holder drags the demon along at
        /// the spot it was grabbed at, pulled in until the two bodies touch (D-061). A longer hold wins.
        /// </summary>
        internal void Hold(long untilTick, Demon holder)
        {
            if (holder == null)
            {
                throw new ArgumentNullException(nameof(holder));
            }

            HeldUntilTick = Math.Max(HeldUntilTick, untilTick);
            HeldBy = holder.Id;
            Intent = MovementIntent.None;
            CurrentSkillUse = null;
            EatingFoodId = FoodId.None;

            Vector2 facing = holder.FacingDirection;
            var right = new Vector2(facing.Y, -facing.X);
            var offset = new Vector2(Position.X - holder.Position.X, Position.Z - holder.Position.Z);
            var local = new Vector2(Vector2.Dot(offset, right), Vector2.Dot(offset, facing));
            float maxLength = (holder.SizeMeters + SizeMeters) * HoldGapPerSize;
            float length = local.Length();
            if (length <= 0.001f)
            {
                local = new Vector2(0f, maxLength);
            }
            else if (length > maxLength)
            {
                local *= maxLength / length;
            }

            HeldOffset = local;
        }

        /// <summary>Ends a grab, on time or early: the demon stops following and moves on its own again.</summary>
        internal void ReleaseHold()
        {
            HeldUntilTick = -1;
            HeldBy = DemonId.None;
            HeldOffset = Vector2.Zero;
            ClearExternalVelocity();
        }

        /// <summary>Pushes the demon with a velocity until the tick: a dash of its own or a knockback.</summary>
        internal void Push(Vector2 velocity, long untilTick)
        {
            ExternalVelocity = velocity;
            ExternalVelocityUntilTick = untilTick;
        }

        internal void ClearExternalVelocity()
        {
            ExternalVelocity = Vector2.Zero;
            ExternalVelocityUntilTick = -1;
        }

        /// <summary>Collects sprint XP and hands out whole points, so the XP events stay quiet.</summary>
        internal float BufferSprintXp(float amount)
        {
            _sprintXpBuffer += amount;
            float whole = MathF.Floor(_sprintXpBuffer);
            _sprintXpBuffer -= whole;
            return whole;
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
            TransformingUntilTick = -1;
            HeldUntilTick = -1;
            ClearExternalVelocity();
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

            bool pushed = ExternalVelocity.LengthSquared() > 0f;
            if (!Intent.IsMoving && !pushed)
            {
                return;
            }

            Position += Velocity * seconds;
            if (Intent.IsMoving && !Intent.HasFacing)
            {
                Yaw = YawToward(Intent.Direction);
            }
        }

        /// <summary>Recomputes the derived values after a stat or body change and rescales body HP, stamina and size with them.</summary>
        internal void RecomputeDerived(CombatTuning tuning)
        {
            Derived = DerivedStats.From(EffectiveStats(), tuning);
            Body.RescaleHp(Derived.HpMultiplier);
            Stamina = MathF.Min(Stamina, Derived.MaxStamina);
            RecomputeSize();
        }

        /// <summary>Allocated points plus what the attached parts contribute; the derived values come from these.</summary>
        public BaseStats EffectiveStats()
        {
            BaseStats effective = Stats;
            IReadOnlyList<StatId> ids = Stats.Ids;
            for (int i = 0; i < ids.Count; i++)
            {
                int bonus = Body.StatBonus(ids[i]);
                if (bonus > 0)
                {
                    effective = effective.WithAdded(ids[i], bonus);
                }
            }

            return effective;
        }

        /// <summary>Plugs a part into a free socket, takes over its skills and recomputes what depends on the body.</summary>
        internal BodyPart AttachPart(BodyPartSpec spec)
        {
            BodyPart part = Body.Attach(spec);
            AddSkillsFrom(part);
            RecomputeDerived(_tuning);
            return part;
        }

        /// <summary>Raises a part one upgrade level and recomputes what depends on it.</summary>
        internal void UpgradePart(BodyPart part)
        {
            Body.Upgrade(part);
            RecomputeDerived(_tuning);
        }

        /// <summary>Brings a lost part back at full health; its skills become usable again at their old levels.</summary>
        internal void RegrowPart(BodyPart part)
        {
            Body.Regrow(part);
            RecomputeDerived(_tuning);
        }

        /// <summary>Counts an evolution; tier and size follow.</summary>
        internal void RecordEvolution()
        {
            Evolutions++;
            RecomputeDerived(_tuning);
        }

        /// <summary>Records a taken evolution by id, for the run summary (D-075), and counts it.</summary>
        internal void RecordEvolution(string evolutionId)
        {
            _evolutionIds.Add(evolutionId ?? throw new ArgumentNullException(nameof(evolutionId)));
            RecordEvolution();
        }

        private void AddSkillsFrom(BodyPart part)
        {
            IReadOnlyList<string> granted = part.Spec.GrantedSkillIds;
            for (int i = 0; i < granted.Count; i++)
            {
                if (FindSkill(granted[i]) == null)
                {
                    _skills.Add(new SkillInstance(_catalog.GetSkill(granted[i])));
                }
            }
        }

        private void RecomputeSize()
        {
            SizeMeters = SizeFor(Spec, Tier, _tuning);
            HighestTier = Math.Max(HighestTier, Tier);
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

        /// <summary>
        /// Gives a freshly spawned demon what its kind is born with (D-070): parts, Biomass, levels with their stat
        /// points and an evolution package, all free and without a transformation. A part without a free socket is skipped.
        /// </summary>
        internal void ApplyStartingPackage()
        {
            IReadOnlyList<string> parts = Spec.StartingPartIds;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPartSpec part = _catalog.GetBodyPart(parts[i]);
                if (Body.CanAttach(part, out _))
                {
                    AttachPart(part);
                }
            }

            if (Spec.StartingBiomass > 0f)
            {
                GainBiomass(Spec.StartingBiomass);
            }

            while (Level < Spec.StartingLevel)
            {
                Level++;
                Stats.GrantPoints(_tuning.StatPointsPerLevel);
            }

            if (!string.IsNullOrEmpty(Spec.StartingEvolutionId))
            {
                EvolutionPackage.Apply(this, _catalog.GetEvolution(Spec.StartingEvolutionId), _catalog);
            }

            RecomputeDerived(_tuning);
        }

        /// <summary>Sprint XP waiting to become a whole point; saved with the run.</summary>
        internal float SprintXpBuffer => _sprintXpBuffer;

        /// <summary>
        /// Overwrites this fresh demon with saved state (D-073); the caller created it from the kind, position and yaw
        /// of the snapshot. Stats and parts come first so the derived values rescale the body once, then the exact
        /// saved numbers go back on, then the status timers. A skill use in progress and the movement intent are not
        /// saved; the next commands set them.
        /// </summary>
        internal void Restore(DemonSnapshot saved)
        {
            if (saved == null)
            {
                throw new ArgumentNullException(nameof(saved));
            }

            for (int i = 0; i < saved.Stats.Count; i++)
            {
                var stat = new StatId(saved.Stats[i].StatId);
                if (Stats.Has(stat))
                {
                    Stats.Set(stat, saved.Stats[i].Value);
                }
            }

            Stats.GrantPoints(saved.UnspentPoints);
            for (int i = 0; i < saved.CapBonuses.Count; i++)
            {
                RaiseStatCap(new StatId(saved.CapBonuses[i].StatId), saved.CapBonuses[i].Value);
            }

            for (int i = 0; i < saved.UnlockedPartIds.Count; i++)
            {
                UnlockPart(saved.UnlockedPartIds[i]);
            }

            Evolutions = saved.Evolutions;
            _evolutionIds.Clear();
            _evolutionIds.AddRange(saved.EvolutionIds);
            Level = saved.Level;
            Xp = saved.Xp;
            for (int i = 1; i < saved.Parts.Count; i++)
            {
                Body.AddRestored(_catalog.GetBodyPart(saved.Parts[i].SpecId));
            }

            _skills.Clear();
            for (int i = 0; i < saved.Skills.Count; i++)
            {
                SkillSnapshot skill = saved.Skills[i];
                var instance = new SkillInstance(_catalog.GetSkill(skill.SpecId), skill.GrantedByEvolution);
                instance.Restore(skill.Level, skill.Xp, skill.CooldownUntilTick);
                _skills.Add(instance);
            }

            RecomputeDerived(_tuning);
            IReadOnlyList<BodyPart> parts = Body.Parts;
            for (int i = 0; i < saved.Parts.Count && i < parts.Count; i++)
            {
                PartSnapshot part = saved.Parts[i];
                parts[i].Restore(part.UpgradeLevel, part.MaxHp, part.Hp, part.IsLost, part.BleedSecondsLeft, part.BleedDamagePerSecond, (DamageType)part.BleedType);
            }

            Biomass = saved.Biomass;
            BiomassEaten = saved.BiomassEaten;
            Stamina = saved.Stamina;
            Kills = saved.Kills;
            StaggeredUntilTick = saved.StaggeredUntilTick;
            LastCombatTick = saved.LastCombatTick;
            LastAttackedBy = new DemonId(saved.LastAttackedBy);
            LastAttackedTick = saved.LastAttackedTick;
            TransformingUntilTick = saved.TransformingUntilTick;
            TransformationStartedTick = saved.TransformationStartedTick;
            HeldUntilTick = saved.HeldUntilTick;
            HeldBy = new DemonId(saved.HeldBy);
            HeldOffset = new Vector2(saved.HeldOffsetX, saved.HeldOffsetY);
            ExternalVelocity = new Vector2(saved.ExternalVelocityX, saved.ExternalVelocityY);
            ExternalVelocityUntilTick = saved.ExternalVelocityUntilTick;
            EatingFoodId = new FoodId(saved.EatingFoodId);
            EatRequestTick = saved.EatRequestTick;
            _sprintXpBuffer = saved.SprintXpBuffer;
            RecomputeSize();
            HighestTier = Math.Max(HighestTier, saved.HighestTier);
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
