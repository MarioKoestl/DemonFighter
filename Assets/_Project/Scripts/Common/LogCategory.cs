#nullable enable
namespace DemonFighter.Common
{
    /// <summary>
    /// Well-known log categories, one per layer or system, so Console filters stay stable as code moves.
    /// </summary>
    public static class LogCategory
    {
        public const string App = "App";
        public const string Sim = "Sim";
        public const string Ui = "UI";
        public const string Editor = "Editor";
        public const string Content = "Content";
        public const string World = "World";
    }
}
