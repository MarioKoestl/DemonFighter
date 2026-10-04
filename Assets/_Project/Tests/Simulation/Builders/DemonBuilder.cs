#nullable enable
using System.Numerics;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Content;

namespace DemonFighter.Simulation.Tests.Builders
{
    /// <summary>
    /// Spawns a demon into a run for tests, with the Tier 0 blob as the default kind so tests only state what matters.
    /// </summary>
    internal sealed class DemonBuilder
    {
        public static DemonSpec Blob => TestContent.Blob;

        public static DemonSpec Elder => TestContent.Elder;

        private ControllerKind _controller = ControllerKind.Ai;
        private DemonSpec _spec = TestContent.Blob;
        private Vector3 _position = Vector3.Zero;
        private float _yaw;

        public DemonBuilder AsPlayer()
        {
            _controller = ControllerKind.Player;
            return this;
        }

        public DemonBuilder WithSpec(DemonSpec spec)
        {
            _spec = spec;
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
            return state.SpawnDemon(_controller, _spec, _position, _yaw);
        }
    }
}
