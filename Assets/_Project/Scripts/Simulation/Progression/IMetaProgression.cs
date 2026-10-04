#nullable enable
using System;

namespace DemonFighter.Simulation.Progression
{
    /// <summary>
    /// The hook for whatever persists between runs later (GAME_DESIGN, "Meta-progression": none in v1; D-075). Every
    /// finished run is handed over once, after the player died.
    /// </summary>
    public interface IMetaProgression
    {
        void RecordRun(RunSummary summary);
    }

    /// <summary>v1: nothing persists between runs; the summary is read and dropped.</summary>
    public sealed class NoMetaProgression : IMetaProgression
    {
        /// <inheritdoc />
        public void RecordRun(RunSummary summary)
        {
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }
        }
    }
}
