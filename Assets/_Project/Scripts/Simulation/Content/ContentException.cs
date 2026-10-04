#nullable enable
using System;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Thrown when content is wrong: a missing id, a duplicate, a number out of range. Content errors are programmer
    /// or designer errors and fail fast at load; they are never swallowed.
    /// </summary>
    public sealed class ContentException : Exception
    {
        public ContentException(string message)
            : base(message)
        {
        }
    }
}
