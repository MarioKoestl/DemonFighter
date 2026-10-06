#nullable enable
using UnityEngine;

namespace DemonFighter.Presentation
{
    /// <summary>
    /// Finds the ground under a point for gore that lies on it, viscera and blood decals: the nearest solid surface
    /// below that is no body part, no food and no demon's CharacterController. The controller capsule is solid for
    /// movement but it is no ground; a probe that stopped on it left the viscera of a death hanging in the air where
    /// the demon had stood, once the corpse switched its controller off.
    /// </summary>
    public static class GroundProbe
    {
        private const int MaxHits = 16;
        private static readonly RaycastHit[] Hits = new RaycastHit[MaxHits];

        /// <summary>The ground point under a position, probed from a height above it down to a depth below; false when there is none.</summary>
        public static bool TryFind(Vector3 position, float probeHeight, float probeDepth, out Vector3 point)
        {
            int mask = ~(Layers.DemonMask | Layers.FoodMask);
            Vector3 origin = position + (Vector3.up * probeHeight);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, Hits, probeHeight + probeDepth, mask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            point = position;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.collider is CharacterController || hit.distance >= nearest)
                {
                    continue;
                }

                nearest = hit.distance;
                point = hit.point;
            }

            return nearest < float.MaxValue;
        }
    }
}
