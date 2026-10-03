#nullable enable
namespace DemonFighter.App
{
    /// <summary>
    /// Names of the three scenes. The scene flow loads by these names and the scene generator creates the files and
    /// the build list from them, so a rename happens in one place.
    /// </summary>
    public static class SceneNames
    {
        public const string Bootstrap = "Bootstrap";
        public const string MainMenu = "MainMenu";
        public const string Run = "Run";
    }
}
