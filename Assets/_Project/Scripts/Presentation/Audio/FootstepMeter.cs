#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation;
using UnityEngine;

namespace DemonFighter.Presentation.Audio
{
    /// <summary>Counts the ground each body covers and says when it completed a stride, so footsteps follow the real movement (D-084).</summary>
    public sealed class FootstepMeter
    {
        private readonly Dictionary<DemonId, float> _travelled = new Dictionary<DemonId, float>();

        /// <summary>Adds the distance a body moved; true when that completed a stride and a step sounds now. One step per call at most.</summary>
        public bool Advance(DemonId demon, float distance, float stride)
        {
            if (stride <= 0f)
            {
                return false;
            }

            _travelled.TryGetValue(demon, out float travelled);
            travelled += Mathf.Max(0f, distance);
            bool step = travelled >= stride;
            if (step)
            {
                travelled -= stride;
            }

            _travelled[demon] = travelled;
            return step;
        }

        /// <summary>Drops the count of a body that died or left.</summary>
        public void Forget(DemonId demon)
        {
            _travelled.Remove(demon);
        }
    }
}
