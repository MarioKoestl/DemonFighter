#nullable enable
namespace DemonFighter.Common
{
    /// <summary>
    /// Plain C# object that wants one call per rendered frame, driven by whoever owns a MonoBehaviour for it. Keeps
    /// frame-bound work such as mouse look out of the fixed simulation step.
    /// </summary>
    public interface IFrameUpdatable
    {
        /// <summary>Called once per frame from Update.</summary>
        void UpdateFrame();
    }
}
