#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>Resolves which granted skill a key slot drives right now; input and HUD both ask here, so they agree.</summary>
    public static class SkillSlots
    {
        /// <summary>The granted skill in the slot with the highest priority, or null when no attached part grants one.</summary>
        public static SkillInstance? Find(Demon demon, SkillSlot slot)
        {
            SkillInstance? best = null;
            IReadOnlyList<SkillInstance> skills = demon.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillInstance skill = skills[i];
                if (skill.Spec.InputSlot != slot || !skill.IsGrantedBy(demon.Body))
                {
                    continue;
                }

                if (best == null || skill.Spec.SlotPriority > best.Spec.SlotPriority)
                {
                    best = skill;
                }
            }

            return best;
        }
    }
}
