#nullable enable
using System.Collections.Generic;

namespace DemonFighter.Simulation.Skills
{
    /// <summary>Ends skill uses whose recovery is over, so the demon can act again.</summary>
    internal static class SkillSystem
    {
        /// <summary>Clears finished uses; part of the status stage.</summary>
        public static void Advance(RunState state)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (demon.CurrentSkillUse != null && demon.CurrentSkillUse.IsOver(state.Tick))
                {
                    demon.ClearSkillUse();
                }
            }
        }
    }
}
