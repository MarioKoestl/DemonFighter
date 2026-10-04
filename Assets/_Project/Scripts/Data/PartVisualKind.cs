#nullable enable
namespace DemonFighter.Data
{
    /// <summary>The primitive a body part is drawn with in the placeholder stage (ASSET_PIPELINE, "Placeholder standard").</summary>
    public enum PartVisualKind
    {
        /// <summary>No primitive of its own; the core is the body capsule itself.</summary>
        None,
        Capsule,
        Sphere,
        Cube,
    }
}
