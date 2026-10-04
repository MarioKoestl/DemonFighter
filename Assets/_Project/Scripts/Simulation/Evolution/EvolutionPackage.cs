#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Evolution
{
    /// <summary>
    /// Applies what an evolution grants (GAME_DESIGN, "Evolution"; D-059, D-067): the cap bonuses, the bound stat
    /// gains, the free points, the unlocks, the extra skills and the free parts, then the evolution itself. Shared by
    /// the Evolve command, which adds the transformation and the event, and by spawning, which hands a kind its
    /// starting evolution without either (D-070).
    /// </summary>
    internal static class EvolutionPackage
    {
        public static void Apply(Demon demon, EvolutionSpec spec, ContentCatalog catalog)
        {
            IReadOnlyList<StatValue> caps = spec.StatCapBonuses;
            for (int i = 0; i < caps.Count; i++)
            {
                demon.RaiseStatCap(caps[i].Stat, caps[i].Value);
            }

            IReadOnlyList<StatValue> bonuses = spec.StatBonuses;
            for (int i = 0; i < bonuses.Count; i++)
            {
                demon.GrantStat(bonuses[i].Stat, bonuses[i].Value);
            }

            demon.Stats.GrantPoints(spec.StatPoints);
            for (int i = 0; i < spec.UnlockedPartIds.Count; i++)
            {
                demon.UnlockPart(spec.UnlockedPartIds[i]);
            }

            for (int i = 0; i < spec.ExtraSkillIds.Count; i++)
            {
                demon.GrantSkill(catalog.GetSkill(spec.ExtraSkillIds[i]));
            }

            // Free parts need a free socket; a line whose gift the body already holds simply keeps what it has.
            for (int i = 0; i < spec.FreeMutationPartIds.Count; i++)
            {
                BodyPartSpec part = catalog.GetBodyPart(spec.FreeMutationPartIds[i]);
                if (demon.Body.CanAttach(part, out _))
                {
                    demon.AttachPart(part);
                }
            }

            demon.RecordEvolution(spec.Id);
        }
    }
}
