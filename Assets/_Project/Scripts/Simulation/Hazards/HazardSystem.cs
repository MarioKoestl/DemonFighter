#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Combat;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation.Hazards
{
    /// <summary>
    /// Burns every demon that stands in lava or a glowing fissure (GAME_DESIGN, "World": damage on contact; D-086).
    /// The fire eats the part touching the ground first, the legs while they last, then the core; it takes a share of
    /// that part's health per second, so a 15 m elder burns as surely as a blob, and armor does not help. Fire leaves no
    /// bleeding wound and nobody gets the kill. A transforming body is spared like from any damage (D-014).
    /// </summary>
    internal static class HazardSystem
    {
        public static void Advance(RunState state, DamageSystem damage, float seconds)
        {
            HazardMap map = state.Hazards;
            WorldLayout? world = state.World;
            if (world == null || map.Zones.Count == 0)
            {
                return;
            }

            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (!demon.IsAlive)
                {
                    continue;
                }

                HazardKind kind = map.KindAt(demon.Position);
                demon.SetHazard(kind);
                if (kind == HazardKind.None || demon.IsTransforming(state.Tick))
                {
                    continue;
                }

                float fraction = kind == HazardKind.Lava ? world.Biome.LavaBurnFractionPerSecond : world.Biome.FissureBurnFractionPerSecond;
                BodyPart part = BurningPart(demon);
                damage.ApplyDamage(demon, part, part.MaxHp * fraction * seconds, DamageType.Fire, DemonId.None);
            }
        }

        // Feet first: the legs burn while they stand, then the body itself.
        private static BodyPart BurningPart(Demon demon)
        {
            IReadOnlyList<BodyPart> parts = demon.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsLost && parts[i].Spec.Socket == SocketKind.Locomotion)
                {
                    return parts[i];
                }
            }

            return demon.Body.Core;
        }
    }
}
