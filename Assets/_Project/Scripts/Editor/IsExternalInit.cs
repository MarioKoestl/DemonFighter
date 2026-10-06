#nullable enable
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Marker type the C# 9 compiler needs for init accessors. .NET Standard 2.1 does not ship it and the type has to
    /// be internal to every assembly that uses init-only properties, so the editor carries its own copy for the
    /// validation facts.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
