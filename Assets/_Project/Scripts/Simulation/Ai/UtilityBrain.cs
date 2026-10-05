#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Evolution;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Hazards;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Skills;

namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// Decides what one AI demon does and expresses it as the same commands a player sends (ARCHITECTURE, "AI").
    /// Goals are picked by weighted chance from the archetype weights, scored by what the demon perceives, whenever
    /// the current goal completes or something new comes into view; low health near a fight overrides everything
    /// with fleeing. In calm moments the demon grows by the preferences of its archetype: it spends stat points, takes
    /// a pending evolution and buys, regrows or upgrades parts through the same commands as the player (D-072).
    /// Demons with the same archetype still behave differently while the run stays deterministic.
    /// </summary>
    public sealed class UtilityBrain
    {
        /// <summary>Half the size of the prey counts as its radius when judging reach, like the hit rules do.</summary>
        private const float TargetRadiusPerMeter = 0.3f;
        private const float HazardLookaheadMeters = 3f;
        private const float HazardLookaheadPerMeter = 1.5f;
        private const float BodyRadiusPerMeter = 0.3f;

        private readonly GroundBounds _bounds;
        private readonly IReadOnlyList<Vector3>? _route;
        private long _restUntilTick;
        private bool _sawOpportunity;

        /// <summary>Creates a brain for one AI demon; the route is optional and only patrolling archetypes use it.</summary>
        public UtilityBrain(Demon demon, ArchetypeSpec archetype, GroundBounds bounds, IReadOnlyList<Vector3>? route)
        {
            Demon = demon ?? throw new ArgumentNullException(nameof(demon));
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            if (demon.Controller != ControllerKind.Ai)
            {
                throw new ArgumentException("Only AI demons get a brain.", nameof(demon));
            }

            _bounds = bounds;
            _route = route != null && route.Count > 0 ? route : null;
            CurrentTarget = demon.Position;
        }

        /// <summary>The demon this brain drives.</summary>
        public Demon Demon { get; }

        /// <summary>The personality this brain follows.</summary>
        public ArchetypeSpec Archetype { get; }

        /// <summary>What the demon is doing right now.</summary>
        public AiGoal CurrentGoal { get; private set; }

        /// <summary>Where the demon is heading: a wander or route point, the prey, the food, or away from a threat.</summary>
        public Vector3 CurrentTarget { get; private set; }

        /// <summary>Index of the route waypoint being walked to.</summary>
        public int RouteIndex { get; private set; }

        /// <summary>The demon being hunted; null outside the Hunt goal.</summary>
        public Demon? Prey { get; private set; }

        /// <summary>The food being walked to or eaten; None outside the Eat goal.</summary>
        public FoodId Food { get; private set; }

        /// <summary>The demon being fled from; null outside the Flee goal.</summary>
        public Demon? Threat { get; private set; }

        /// <summary>Thinks once: spends a stat point, flees if it must, turns on an attacker, grows when calm, otherwise finishes or keeps the current goal, then acts.</summary>
        public void Decide(RunState state, CommandQueue commands)
        {
            SpendStatPoint(state, commands);
            Demon? threat = FindThreatIfWeak(state);
            Demon? attacker = threat == null ? FindAttackerToPunish(state) : null;
            if (threat != null)
            {
                StartFlee(state, threat);
            }
            else if (attacker != null)
            {
                StartHunt(attacker);
            }
            else if (!TryGrow(state, commands))
            {
                ContinueGoal(state);
            }

            Act(state, commands);
        }

        /// <summary>Between decisions, keeps a meal going: eating needs a command every tick, like a held key.</summary>
        public void Hold(CommandQueue commands)
        {
            if (CurrentGoal == AiGoal.Eat && Demon.IsEating)
            {
                commands.Submit(new EatCommand(Demon.Id, Demon.EatingFoodId));
            }
        }

        private void ContinueGoal(RunState state)
        {
            switch (CurrentGoal)
            {
                case AiGoal.None:
                    ChooseGoal(state);
                    break;
                case AiGoal.Rest:
                    if (state.Tick >= _restUntilTick || NoticedOpportunity(state))
                    {
                        ChooseGoal(state);
                    }

                    break;
                case AiGoal.Wander:
                    if (HasArrived() || NoticedOpportunity(state))
                    {
                        ChooseGoal(state);
                    }

                    break;
                case AiGoal.Patrol:
                    if (HasArrived())
                    {
                        RouteIndex = (RouteIndex + 1) % _route!.Count;
                        ChooseGoal(state);
                    }
                    else if (NoticedOpportunity(state))
                    {
                        ChooseGoal(state);
                    }

                    break;
                case AiGoal.Hunt:
                    if (!PreyIsStillWorthIt())
                    {
                        ChooseGoal(state);
                    }

                    break;
                case AiGoal.Eat:
                    if (!FoodIsStillThere(state))
                    {
                        ChooseGoal(state);
                    }

                    break;
                case AiGoal.Flee:
                    // Nothing to flee from any more: pick something to do.
                    ChooseGoal(state);
                    break;
                case AiGoal.Mutate:
                case AiGoal.Evolve:
                    // The body is reshaped: pick something to do.
                    ChooseGoal(state);
                    break;
                default:
                    throw new InvalidOperationException("Unknown goal " + CurrentGoal + ".");
            }
        }

        // Growth happens in calm moments only (D-072): never while hunting, eating or fleeing, and never within the
        // combat window, so no demon stops mid-fight to become invulnerable for two seconds. One step per decision.
        private bool TryGrow(RunState state, CommandQueue commands)
        {
            if (CurrentGoal == AiGoal.Hunt || CurrentGoal == AiGoal.Eat || CurrentGoal == AiGoal.Flee)
            {
                return false;
            }

            CombatTuning tuning = state.Catalog.Tuning;
            if (Demon.IsInCombat(state.Tick, state.Config.TicksFor(tuning.InCombatSeconds)))
            {
                return false;
            }

            if (EvolutionRules.PendingStage(Demon, tuning) > 0)
            {
                EvolutionSpec? line = ChooseEvolution(EvolutionRules.Options(state, Demon));
                if (line != null)
                {
                    ClearTargets();
                    CurrentGoal = AiGoal.Evolve;
                    commands.Submit(new EvolveCommand(Demon.Id, line.Id));
                    return true;
                }
            }

            MutateCommand? mutation = ChooseMutation(state);
            if (mutation == null)
            {
                return false;
            }

            ClearTargets();
            CurrentGoal = AiGoal.Mutate;
            commands.Submit(mutation.Value);
            return true;
        }

        // The offered line whose fit stat the personality favors, otherwise the best fit the rules offer.
        private EvolutionSpec? ChooseEvolution(IReadOnlyList<EvolutionSpec> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].FitStat == Archetype.PreferredEvolutionStat)
                {
                    return options[i];
                }
            }

            return options.Count > 0 ? options[0] : null;
        }

        // The first preferred part the rules allow, then regrowing what was lost, then the cheapest upgrade it can pay.
        private MutateCommand? ChooseMutation(RunState state)
        {
            IReadOnlyList<string> preferred = Archetype.PreferredPartIds;
            for (int i = 0; i < preferred.Count; i++)
            {
                if (state.Catalog.TryGetBodyPart(preferred[i], out BodyPartSpec? spec) && MutationRules.CanAttach(Demon, spec, state, out _, out _))
                {
                    return MutateCommand.Attach(Demon.Id, spec.Id);
                }
            }

            IReadOnlyList<BodyPart> parts = Demon.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].IsLost && MutationRules.CanRegrow(Demon, parts[i], state, out _, out _))
                {
                    return MutateCommand.Regrow(Demon.Id, parts[i].Index);
                }
            }

            BodyPart? cheapest = null;
            float cheapestCost = float.MaxValue;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                if (part.Spec.IsCore || part.IsLost || !MutationRules.CanUpgrade(Demon, part, state, out _, out float cost) || cost >= cheapestCost)
                {
                    continue;
                }

                cheapest = part;
                cheapestCost = cost;
            }

            return cheapest != null ? MutateCommand.Upgrade(Demon.Id, cheapest.Index) : (MutateCommand?)null;
        }

        // One stat point per decision into the preferred stat, or the next stat with room; the rules check the cap again.
        private void SpendStatPoint(RunState state, CommandQueue commands)
        {
            if (Demon.Stats.UnspentPoints <= 0)
            {
                return;
            }

            StatId stat = Archetype.PreferredStat;
            if (!Demon.Stats.Has(stat) || Demon.Stats.Get(stat) >= Demon.StatCap(stat))
            {
                IReadOnlyList<StatSpec> stats = state.Catalog.Tuning.Stats;
                bool found = false;
                for (int i = 0; i < stats.Count && !found; i++)
                {
                    if (Demon.Stats.Get(stats[i].Id) < Demon.StatCap(stats[i].Id))
                    {
                        stat = stats[i].Id;
                        found = true;
                    }
                }

                if (!found)
                {
                    return;
                }
            }

            commands.Submit(new SpendStatPointCommand(Demon.Id, stat));
        }

        private bool HasArrived()
        {
            Vector3 toTarget = CurrentTarget - Demon.Position;
            toTarget.Y = 0f;
            return toTarget.LengthSquared() <= Archetype.ArriveDistance * Archetype.ArriveDistance;
        }

        // Idle goals are interrupted the moment prey or food comes into view, not on every tick it stays in view.
        private bool NoticedOpportunity(RunState state)
        {
            bool present = (Archetype.HuntWeight > 0f && Perception.FindPrey(state, Demon, Archetype, out _) != null)
                || (Archetype.EatWeight > 0f && Perception.FindFood(state, Demon, Archetype, out _) != null);
            bool noticed = present && !_sawOpportunity;
            _sawOpportunity = present;
            return noticed;
        }

        private bool PreyIsStillWorthIt()
        {
            Demon? prey = Prey;
            if (prey == null || !prey.IsAlive)
            {
                return false;
            }

            return Perception.PlanarDistance(Demon.Position, prey.Position) <= Archetype.PerceptionRadius * Perception.LeashFactor;
        }

        private bool FoodIsStillThere(RunState state)
        {
            if (!state.TryGetFood(Food, out FoodItem? food) || food.IsDepleted)
            {
                return false;
            }

            return Perception.PlanarDistance(Demon.Position, food.Position) <= Archetype.PerceptionRadius * Perception.LeashFactor;
        }

        private Demon? FindThreatIfWeak(RunState state)
        {
            if (Archetype.FleeHealthFraction <= 0f)
            {
                return null;
            }

            float fraction = Demon.Body.TotalHp / Demon.Body.TotalMaxHp;
            return fraction < Archetype.FleeHealthFraction ? Perception.FindThreat(state, Demon, Archetype) : null;
        }

        // Being bitten overrides every goal but fleeing: the attacker becomes the prey whatever it is worth, so a
        // provoked elder turns on a blob (GAME_DESIGN, "Elders"). Archetypes that never hunt do not retaliate.
        private Demon? FindAttackerToPunish(RunState state)
        {
            if (Archetype.HuntWeight <= 0f)
            {
                return null;
            }

            Demon? attacker = Perception.FindAttacker(state, Demon, Archetype);
            if (attacker == null || (CurrentGoal == AiGoal.Hunt && Prey == attacker))
            {
                return null;
            }

            return attacker;
        }

        private void ChooseGoal(RunState state)
        {
            Demon? prey = null;
            float preyScore = 0f;
            if (Archetype.HuntWeight > 0f)
            {
                prey = Perception.FindPrey(state, Demon, Archetype, out preyScore);
            }

            FoodItem? food = null;
            float foodScore = 0f;
            if (Archetype.EatWeight > 0f)
            {
                food = Perception.FindFood(state, Demon, Archetype, out foodScore);
            }

            float hunt = prey != null ? Archetype.HuntWeight * preyScore : 0f;
            float eat = food != null ? Archetype.EatWeight * foodScore : 0f;
            float patrol = _route != null ? Archetype.PatrolWeight : 0f;
            float total = hunt + eat + Archetype.WanderWeight + Archetype.RestWeight + patrol;
            if (total <= 0f)
            {
                StartRest(state);
                return;
            }

            float roll = state.Rng.NextFloat(0f, total);
            if (roll < hunt)
            {
                StartHunt(prey!);
            }
            else if (roll < hunt + eat)
            {
                StartEat(food!);
            }
            else if (roll < hunt + eat + Archetype.WanderWeight)
            {
                StartWander(state);
            }
            else if (roll < hunt + eat + Archetype.WanderWeight + Archetype.RestWeight)
            {
                StartRest(state);
            }
            else
            {
                StartPatrol(state);
            }
        }

        private void ClearTargets()
        {
            Prey = null;
            Food = FoodId.None;
            Threat = null;
        }

        private void StartHunt(Demon prey)
        {
            ClearTargets();
            CurrentGoal = AiGoal.Hunt;
            Prey = prey;
            CurrentTarget = prey.Position;
            _sawOpportunity = false;
        }

        private void StartEat(FoodItem food)
        {
            ClearTargets();
            CurrentGoal = AiGoal.Eat;
            Food = food.Id;
            CurrentTarget = food.Position;
            _sawOpportunity = false;
        }

        private void StartFlee(RunState state, Demon threat)
        {
            ClearTargets();
            CurrentGoal = AiGoal.Flee;
            Threat = threat;
            Vector3 away = Demon.Position - threat.Position;
            away.Y = 0f;
            if (away.LengthSquared() <= 0f)
            {
                away = new Vector3(Demon.FacingDirection.X, 0f, Demon.FacingDirection.Y);
            }

            CurrentTarget = SafeGoal(state, Demon.Position + Vector3.Normalize(away) * Archetype.WanderRadius);
        }

        private void StartWander(RunState state)
        {
            ClearTargets();
            CurrentGoal = AiGoal.Wander;
            float angle = state.Rng.NextFloat(0f, MathF.PI * 2f);
            float distance = state.Rng.NextFloat(Archetype.ArriveDistance, Archetype.WanderRadius);
            var offset = new Vector3(MathF.Sin(angle) * distance, 0f, MathF.Cos(angle) * distance);
            CurrentTarget = SafeGoal(state, Demon.Position + offset);
        }

        private void StartRest(RunState state)
        {
            ClearTargets();
            CurrentGoal = AiGoal.Rest;
            float seconds = Archetype.RestSecondsMax > Archetype.RestSecondsMin
                ? state.Rng.NextFloat(Archetype.RestSecondsMin, Archetype.RestSecondsMax)
                : Archetype.RestSecondsMin;
            _restUntilTick = state.Tick + (long)MathF.Ceiling(seconds * state.Config.TicksPerSecond);
        }

        // The route shrinks toward the player as the threat rises (D-070): elders wander closer when the run runs long.
        private void StartPatrol(RunState state)
        {
            ClearTargets();
            CurrentGoal = AiGoal.Patrol;
            Vector3 waypoint = _route![RouteIndex];
            float pull = MathF.Min(Archetype.RoutePullMax, Archetype.RoutePullPerThreat * state.ThreatLevel);
            Demon? player = pull > 0f ? Perception.FindPlayer(state) : null;
            CurrentTarget = SafeGoal(state, player != null ? Vector3.Lerp(waypoint, player.Position, pull) : waypoint);
        }

        private void Act(RunState state, CommandQueue commands)
        {
            switch (CurrentGoal)
            {
                case AiGoal.Rest:
                case AiGoal.Mutate:
                case AiGoal.Evolve:
                    commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false));
                    break;
                case AiGoal.Hunt:
                    ActHunt(state, commands);
                    break;
                case AiGoal.Eat:
                    ActEat(state, commands);
                    break;
                case AiGoal.Flee:
                    MoveToward(state, CurrentTarget, sprint: true, commands);
                    break;
                default:
                    MoveToward(state, CurrentTarget, sprint: false, commands);
                    break;
            }
        }

        private void ActHunt(RunState state, CommandQueue commands)
        {
            Demon prey = Prey!;
            CurrentTarget = prey.Position;
            Vector2 toPrey = Planar(prey.Position - Demon.Position);
            float distance = toPrey.Length();
            Vector2 facing = distance > 0f ? toPrey / distance : Demon.FacingDirection;
            SkillInstance? strike = PickStrike();
            float reach = strike != null
                ? strike.Spec.ReachPerMeter * Demon.SizeMeters + prey.SizeMeters * TargetRadiusPerMeter
                : Archetype.ArriveDistance;
            if (distance > reach)
            {
                // Out of reach: a leap closes the gap when the body has one, otherwise walk.
                SkillInstance? dash = PickDash();
                if (dash != null && distance <= reach + dash.Spec.DashMeters && CanUse(dash, state.Tick))
                {
                    commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false, facing));
                    commands.Submit(new UseSkillCommand(Demon.Id, dash.Spec.Id));
                    return;
                }

                // A hunter follows its prey into lava or onto a fissure and burns there (D-086, Mario): lava is a trap
                // to lure it into. Prey outside a hazard is reached around it as before.
                bool preyInHazard = state.Hazards.KindAt(prey.Position) != HazardKind.None;
                commands.Submit(new MoveCommand(Demon.Id, preyInHazard ? facing : AvoidHazards(state, facing), sprint: false));
                return;
            }

            // In reach: stand, face the prey and strike whenever the skill allows. The view detects the hit itself.
            commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false, facing));
            if (strike != null && CanUse(strike, state.Tick))
            {
                commands.Submit(new UseSkillCommand(Demon.Id, strike.Spec.Id));
            }
        }

        private bool CanUse(SkillInstance skill, long tick)
        {
            return Demon.CurrentSkillUse == null
                && !skill.IsOnCooldown(tick)
                && !Demon.IsStaggered(tick)
                && Demon.Stamina >= skill.StaminaCost;
        }

        private void ActEat(RunState state, CommandQueue commands)
        {
            if (!state.TryGetFood(Food, out FoodItem? food))
            {
                commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false));
                return;
            }

            CurrentTarget = food.Position;
            Vector2 toFood = Planar(food.Position - Demon.Position);
            float distance = toFood.Length();
            if (distance > state.Catalog.Tuning.EatReachPerMeter * Demon.SizeMeters)
            {
                MoveToward(state, food.Position, sprint: false, commands);
                return;
            }

            Vector2 facing = distance > 0f ? toFood / distance : Demon.FacingDirection;
            commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false, facing));
            commands.Submit(new EatCommand(Demon.Id, food.Id));
        }

        private void MoveToward(RunState state, Vector3 target, bool sprint, CommandQueue commands)
        {
            Vector2 direction = Planar(target - Demon.Position);
            float length = direction.Length();
            if (length > 0f)
            {
                direction /= length;
            }

            commands.Submit(new MoveCommand(Demon.Id, AvoidHazards(state, direction), sprint));
        }

        // AI demons do not walk into lava or fissures (D-086): the heading bends around them, a body already inside walks out.
        private Vector2 AvoidHazards(RunState state, Vector2 direction)
        {
            float lookahead = MathF.Max(HazardLookaheadMeters, Demon.SizeMeters * HazardLookaheadPerMeter);
            return state.Hazards.Steer(Demon.Position, direction, lookahead, HazardMargin(state));
        }

        // A goal inside a hazard would be circled forever; it moves to a point beside the hazard and inside the walls.
        private Vector3 SafeGoal(RunState state, Vector3 goal)
        {
            return _bounds.Clamp(state.Hazards.PushOut(_bounds.Clamp(goal), HazardMargin(state)));
        }

        private float HazardMargin(RunState state)
        {
            float margin = state.World != null ? state.World.Biome.HazardAvoidMarginMeters : 0f;
            return margin + Demon.SizeMeters * BodyRadiusPerMeter;
        }

        // The hardest granted strike that is no leap and no mere hold; Grab is a combo tool the AI leaves to the player.
        private SkillInstance? PickStrike()
        {
            SkillInstance? best = null;
            IReadOnlyList<SkillInstance> skills = Demon.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillInstance skill = skills[i];
                if (skill.Spec.IsPassive || skill.Spec.DashMeters > 0f || skill.Spec.BaseDamage <= 0f || !skill.IsGrantedBy(Demon.Body))
                {
                    continue;
                }

                if (best == null || skill.Spec.BaseDamage > best.Spec.BaseDamage)
                {
                    best = skill;
                }
            }

            return best;
        }

        private SkillInstance? PickDash()
        {
            IReadOnlyList<SkillInstance> skills = Demon.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                if (!skills[i].Spec.IsPassive && skills[i].Spec.DashMeters > 0f && skills[i].IsGrantedBy(Demon.Body))
                {
                    return skills[i];
                }
            }

            return null;
        }

        private static Vector2 Planar(Vector3 vector)
        {
            return new Vector2(vector.X, vector.Z);
        }
    }
}
