#nullable enable
using System.Numerics;

namespace DemonFighter.Simulation.Tests.Builders
{
    /// <summary>
    /// Spawns a demon into a run for tests, with a Tier 0 blob as the default body so tests only state what matters.
    /// </summary>
    internal sealed class DemonBuilder
    {
        public static readonly DemonTemplate Blob = new DemonTemplate("Blob", 0, 1.2f, 4f, 1.6f);
        public static readonly DemonTemplate Elder = new DemonTemplate("Elder", 6, 15f, 3f, 1f);

        private ControllerKind _controller = ControllerKind.Ai;
        private DemonTemplate _template = Blob;
        private Vector3 _position = Vector3.Zero;
        private float _yaw;

        public DemonBuilder AsPlayer()
        {
            _controller = ControllerKind.Player;
            return this;
        }

        public DemonBuilder WithTemplate(DemonTemplate template)
        {
            _template = template;
            return this;
        }

        public DemonBuilder At(float x, float z)
        {
            _position = new Vector3(x, 0f, z);
            return this;
        }

        public DemonBuilder FacingYaw(float yaw)
        {
            _yaw = yaw;
            return this;
        }

        public Demon SpawnInto(RunState state)
        {
            return state.SpawnDemon(_controller, _template, _position, _yaw);
        }
    }
}
