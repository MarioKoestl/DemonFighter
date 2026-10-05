#nullable enable
namespace DemonFighter.Editor.Art
{
    /// <summary>The damage state a model or one of its meshes carries in its name (ASSET_PIPELINE, "Modular body parts").</summary>
    internal enum MeshState
    {
        None,
        Intact,
        Wounded,
        Mangled,
        Stump,
    }
}
