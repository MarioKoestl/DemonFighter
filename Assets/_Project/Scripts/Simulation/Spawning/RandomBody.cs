#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;

namespace DemonFighter.Simulation.Spawning
{
    /// <summary>
    /// The random body of a kind born mutated, such as the elder (D-095): every socket slot its starting package left
    /// free is filled by chance with a random part that fits it, unlocks, levels and Biomass aside. A part repeats only
    /// once every other part of its socket is on, so a head gets eyes and jaws before a second of either. Drawn from
    /// the run's random stream, so a seed always grows the same body and a new run a new one.
    /// </summary>
    internal static class RandomBody
    {
        /// <summary>Attaches the random parts of the demon's kind; a kind with no random part chance gets none.</summary>
        public static void Grow(Demon demon, ContentCatalog catalog, Rng rng)
        {
            if (demon == null)
            {
                throw new ArgumentNullException(nameof(demon));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            float chance = demon.Spec.RandomPartChance;
            if (chance <= 0f)
            {
                return;
            }

            List<BodyPartSpec> candidates = SortedParts(catalog);
            var done = new HashSet<SocketKind>();
            var pool = new List<BodyPartSpec>();
            IReadOnlyList<SocketSlot> sockets = demon.Body.Core.Spec.Sockets;
            for (int s = 0; s < sockets.Count; s++)
            {
                SocketKind kind = sockets[s].Kind;
                if (!done.Add(kind))
                {
                    continue;
                }

                pool.Clear();
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].Socket == kind)
                    {
                        pool.Add(candidates[i]);
                    }
                }

                if (pool.Count == 0)
                {
                    continue;
                }

                int free = demon.Body.FreeSlots(kind);
                for (int slot = 0; slot < free; slot++)
                {
                    if (!rng.NextBool(chance))
                    {
                        continue;
                    }

                    BodyPartSpec pick = LeastPresent(demon.Body, pool, rng);
                    if (demon.Body.CanAttach(pick, out _))
                    {
                        demon.AttachPart(pick);
                    }
                }
            }
        }

        // Every part but cores, in id order, so the draw does not depend on how the catalog stores them.
        private static List<BodyPartSpec> SortedParts(ContentCatalog catalog)
        {
            var parts = new List<BodyPartSpec>();
            foreach (BodyPartSpec part in catalog.BodyParts)
            {
                if (!part.IsCore)
                {
                    parts.Add(part);
                }
            }

            parts.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return parts;
        }

        // One of the parts the body has the fewest copies of, drawn evenly among them.
        private static BodyPartSpec LeastPresent(Body body, List<BodyPartSpec> pool, Rng rng)
        {
            int fewest = int.MaxValue;
            var least = new List<BodyPartSpec>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                int copies = Copies(body, pool[i]);
                if (copies < fewest)
                {
                    fewest = copies;
                    least.Clear();
                }

                if (copies == fewest)
                {
                    least.Add(pool[i]);
                }
            }

            return least[rng.NextInt(0, least.Count)];
        }

        private static int Copies(Body body, BodyPartSpec spec)
        {
            int copies = 0;
            IReadOnlyList<BodyPart> parts = body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (string.Equals(parts[i].Spec.Id, spec.Id, StringComparison.Ordinal))
                {
                    copies++;
                }
            }

            return copies;
        }
    }
}
