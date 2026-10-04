#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Skills;

namespace DemonFighter.Simulation.Combat
{
    /// <summary>
    /// Stands in for the Unity hit scan when no frame runs (D-077): for every skill use in its active window it reports
    /// a hit on a random intact part of the nearest living demon within reach and arc, through the same
    /// <see cref="ReportHitCommand"/> the presenter sends, so the rules judge it the same way (D-046). Switched on only
    /// by the autoplay harness and by tests; in the game the CombatPresenter reports what the body really touched.
    /// </summary>
    internal sealed class HeadlessHitResolver
    {
        /// <summary>Slack on the arc, like the hit rules grant a touching hitbox.</summary>
        private const float ArcToleranceDegrees = 15f;

        /// <summary>Reports one hit per active skill use whose actor has a target in reach and arc.</summary>
        public void Advance(RunState state, CommandQueue commands)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon attacker = demons[i];
                SkillUse? use = attacker.CurrentSkillUse;
                if (!attacker.IsAlive || use == null || use.HitLanded || !use.IsActive(state.Tick))
                {
                    continue;
                }

                Demon? target = Nearest(demons, attacker, use.Skill);
                if (target == null)
                {
                    continue;
                }

                commands.Submit(new ReportHitCommand(attacker.Id, target.Id, PickPart(state.Rng, target)));
            }
        }

        private static Demon? Nearest(IReadOnlyList<Demon> demons, Demon attacker, SkillInstance skill)
        {
            Demon? nearest = null;
            float nearestDistance = float.MaxValue;
            Vector2 facing = attacker.FacingDirection;
            float halfArc = skill.Spec.ArcDegrees * 0.5f + ArcToleranceDegrees;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon candidate = demons[i];
                if (candidate == attacker || !candidate.IsAlive)
                {
                    continue;
                }

                var toTarget = new Vector2(candidate.Position.X - attacker.Position.X, candidate.Position.Z - attacker.Position.Z);
                float distance = toTarget.Length();
                if (distance >= nearestDistance || distance > SkillReach.Meters(attacker, candidate, skill))
                {
                    continue;
                }

                if (distance > 0f)
                {
                    float cos = Vector2.Dot(facing, toTarget / distance);
                    float angle = MathF.Acos(Math.Clamp(cos, -1f, 1f)) * (180f / MathF.PI);
                    if (angle > halfArc)
                    {
                        continue;
                    }
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest;
        }

        // A random intact part, so headless fights wound and sever parts the way aimed bites do.
        private static int PickPart(Rng rng, Demon target)
        {
            IReadOnlyList<BodyPart> parts = target.Body.Parts;
            int intact = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsLost)
                {
                    intact++;
                }
            }

            if (intact == 0)
            {
                return target.Body.Core.Index;
            }

            int pick = rng.NextInt(0, intact);
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].IsLost)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return parts[i].Index;
                }

                pick--;
            }

            return target.Body.Core.Index;
        }
    }
}
