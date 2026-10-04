#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Progression;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>
    /// Sprint is a passive skill (GAME_DESIGN, "Skills": needs Legs, burns stamina): while a demon sprints and moves,
    /// the sprint skill drains stamina per second at its level and earns skill XP per second, one whole point at a
    /// time so the events stay quiet. Without stamina the demon walks.
    /// </summary>
    internal static class SprintSystem
    {
        public static void Advance(RunState state, SimulationEvents events, float seconds)
        {
            CombatTuning tuning = state.Catalog.Tuning;
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (!demon.IsAlive || !demon.IsSprinting || !demon.Intent.IsMoving)
                {
                    continue;
                }

                SkillInstance? sprint = demon.SprintSkill();
                if (sprint == null || !demon.TrySpendStamina(sprint.StaminaCostPerSecond * seconds))
                {
                    continue;
                }

                float whole = demon.BufferSprintXp(sprint.Spec.SkillXpPerSecond * seconds);
                if (whole <= 0f)
                {
                    continue;
                }

                int levels = sprint.GainXp(whole);
                events.Publish(new SkillXpGained(demon.Id, sprint.Spec.Id, whole));
                if (levels > 0)
                {
                    events.Publish(new SkillLevelUp(demon.Id, sprint.Spec.Id, sprint.Level));
                }

                XpSystem.Grant(demon, whole * tuning.CharacterXpPerSkillXp, XpSource.SkillUse, tuning, events);
            }
        }
    }
}
