#nullable enable
namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// What a hide part is made of (D-022). None means unarmored flesh, which takes every damage type at face value.
    /// </summary>
    public enum DefenseType
    {
        None,
        ThickHide,
        Plates,
        ElasticTissue,
    }
}
