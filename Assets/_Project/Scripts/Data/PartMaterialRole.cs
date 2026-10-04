#nullable enable
namespace DemonFighter.Data
{
    /// <summary>Which palette material a placeholder part is drawn with.</summary>
    public enum PartMaterialRole
    {
        /// <summary>The material of the demon that owns the part.</summary>
        Owner,
        Maw,
        Eye,
        Plate,

        /// <summary>Dark flesh, the corpse material.</summary>
        Dark,
    }
}
