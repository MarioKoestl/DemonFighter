#nullable enable
using System;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Issues increasing positive ids for one kind of entity within a run. The last issued id is part of the run
    /// state, so ids stay unique across a save and resume.
    /// </summary>
    public sealed class IdSequence
    {
        /// <summary>Starts a sequence that issues 1 first.</summary>
        public IdSequence()
            : this(0)
        {
        }

        /// <summary>Resumes a sequence after the given id, for restoring a saved run.</summary>
        public IdSequence(int lastIssued)
        {
            if (lastIssued < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lastIssued), lastIssued, "Ids are never negative.");
            }

            LastIssued = lastIssued;
        }

        /// <summary>The most recently issued id, zero before the first one.</summary>
        public int LastIssued { get; private set; }

        /// <summary>Issues the next id. A run never gets anywhere near exhausting the range; if it does, that is a bug.</summary>
        public int Next()
        {
            if (LastIssued == int.MaxValue)
            {
                throw new InvalidOperationException("Id sequence exhausted.");
            }

            LastIssued++;
            return LastIssued;
        }
    }
}
