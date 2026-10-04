#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Skills;

namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// Decides what one AI demon does and expresses it as the same commands a player sends (ARCHITECTURE, "AI").
    /// Goals are picked by weighted chance from the archetype weights, scored by what the demon perceives, whenever
    /// the current goal completes or something new comes into view; low health near a fight overrides everything
    /// with fleeing. Demons with the same archetype still behave differently while the run stays deterministic.
    /// </summary>
    public sealed class UtilityBrain
    {
        /// <summary>Half the size of the prey counts as its radius when judging reach, like the hit rules do.</summary>
        private const float TargetRadiusPerMeter = 0.3f;

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

        /// <summary>Thinks once: flees if it must, turns on an attacker, otherwise finishes or keeps the current goal, then acts.</summary>
        public void Decide(RunState state, CommandQueue commands)
        {
            Demon? threat = FindThreatIfWeak(state);
            Demon? attacker = threat == null ? FindAttackerToPunish(state) : null;
            if (threat != null)
            {
                StartFlee(threat);
            }
            else if (attacker != null)
            {
                StartHunt(attacker);
            }
            else
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
                default:
                    throw new InvalidOperationException("Unknown goal " + CurrentGoal + ".");
            }
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
                StartPatrol();
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

        private void StartFlee(Demon threat)
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

            CurrentTarget = _bounds.Clamp(Demon.Position + Vector3.Normalize(away) * Archetype.WanderRadius);
        }

        private void StartWander(RunState state)
        {
            ClearTargets();
            CurrentGoal = AiGoal.Wander;
            float angle = state.Rng.NextFloat(0f, MathF.PI * 2f);
            float distance = state.Rng.NextFloat(Archetype.ArriveDistance, Archetype.WanderRadius);
            var offset = new Vector3(MathF.Sin(angle) * distance, 0f, MathF.Cos(angle) * distance);
            CurrentTarget = _bounds.Clamp(Demon.Position + offset);
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

        private void StartPatrol()
        {
            ClearTargets();
            CurrentGoal = AiGoal.Patrol;
            CurrentTarget = _route![RouteIndex];
        }

        private void Act(RunState state, CommandQueue commands)
        {
            switch (CurrentGoal)
            {
                case AiGoal.Rest:
                    commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false));
                    break;
                case AiGoal.Hunt:
                    ActHunt(state, commands);
                    break;
                case AiGoal.Eat:
                    ActEat(state, commands);
                    break;
                case AiGoal.Flee:
                    MoveToward(CurrentTarget, sprint: true, commands);
                    break;
                default:
                    MoveToward(CurrentTarget, sprint: false, commands);
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
            SkillInstance? skill = PickAttack();
            float reach = skill != null
                ? skill.Spec.ReachPerMeter * Demon.SizeMeters + prey.SizeMeters * TargetRadiusPerMeter
                : Archetype.ArriveDistance;
            if (distance > reach)
            {
                commands.Submit(new MoveCommand(Demon.Id, facing, sprint: false));
                return;
            }

            // In reach: stand, face the prey and bite whenever the skill allows. The view detects the hit itself.
            commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false, facing));
            if (skill != null
                && Demon.CurrentSkillUse == null
                && !skill.IsOnCooldown(state.Tick)
                && !Demon.IsStaggered(state.Tick)
                && Demon.Stamina >= skill.StaminaCost)
            {
                commands.Submit(new UseSkillCommand(Demon.Id, skill.Spec.Id));
            }
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
                MoveToward(food.Position, sprint: false, commands);
                return;
            }

            Vector2 facing = distance > 0f ? toFood / distance : Demon.FacingDirection;
            commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false, facing));
            commands.Submit(new EatCommand(Demon.Id, food.Id));
        }

        private void MoveToward(Vector3 target, bool sprint, CommandQueue commands)
        {
            Vector2 direction = Planar(target - Demon.Position);
            float length = direction.Length();
            if (length > 0f)
            {
                direction /= length;
            }

            commands.Submit(new MoveCommand(Demon.Id, direction, sprint));
        }

        // The first skill the body still grants; choosing between several skills comes with M3.
        private SkillInstance? PickAttack()
        {
            IReadOnlyList<SkillInstance> skills = Demon.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].IsGrantedBy(Demon.Body))
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
