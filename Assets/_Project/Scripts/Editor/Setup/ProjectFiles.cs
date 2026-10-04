#nullable enable
using Unity.CodeEditor;
using UnityEditor;

namespace DemonFighter.Editor.Setup
{
    /// <summary>
    /// Regenerates the solution and project files so that dotnet build sees every assembly.
    /// Public because the -executeMethod command line switch of Unity has to find it.
    /// </summary>
    public static class ProjectFiles
    {
        [MenuItem("Demon Fighter/Setup/Regenerate Project Files")]
        public static void Regenerate()
        {
            CodeEditor.CurrentEditor.SyncAll();
        }
    }
}
