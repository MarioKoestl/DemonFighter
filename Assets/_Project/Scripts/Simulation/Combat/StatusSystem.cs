#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Anatomy;

namespace DemonFighter.Simulation.Combat
{
    /// <summary>
    /// Stage 2 of the tick (ARCHITECTURE, "Tick"): bleeding drains, passive regeneration heals every surviving part
    /// (D-024), stamina refills. Dead demons are left alone.
    /// </summary>
    internal static class StatusSystem
    {
        /// <summary>Advances every status effect by one step.</summary>
        public static void Advance(RunState state, DamageSystem damage, float seconds)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (!demon.IsAlive)
                {
                    continue;
                }

                AdvanceBody(demon, damage, seconds);
                if (demon.IsAlive)
                {
                    demon.RegenerateStamina(demon.Derived.StaminaRegenPerSecond * seconds);
                }
            }
        }

        private static void AdvanceBody(Demon demon, DamageSystem damage, float seconds)
        {
            IReadOnlyList<BodyPart> parts = demon.Body.Parts;
            for (int p = 0; p < parts.Count; p++)
            {
                BodyPart part = parts[p];
                if (part.IsLost)
                {
                    continue;
                }

                float drain = part.AdvanceBleeding(seconds);
                if (drain > 0f && !damage.ApplyDamage(demon, part, drain, part.BleedType, DemonId.None))
                {
                    return;
                }

                if (!part.IsLost)
                {
                    part.Heal(demon.Derived.RegenPerSecond * seconds);
                }
            }
        }
    }
}
