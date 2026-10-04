#nullable enable
namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Asks a demon to start a skill. Where it aims is decided by Unity when the hit is detected; the simulation only
    /// knows the active window and validates the hit report against it.
    /// </summary>
    public readonly struct UseSkillCommand : ICommand
    {
        public UseSkillCommand(DemonId actor, string skillId)
        {
            Actor = actor;
            SkillId = skillId;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        /// <summary>Content id of the skill, for example skill.bite.</summary>
        public string SkillId { get; }
    }
}
