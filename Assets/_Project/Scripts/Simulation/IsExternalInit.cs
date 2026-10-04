#nullable enable
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Marker type the C# 9 compiler needs for init accessors. .NET Standard 2.1 does not ship it and the Simulation
    /// assembly cannot borrow Unity's copy, so this polyfill lets specs use init-only properties.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
