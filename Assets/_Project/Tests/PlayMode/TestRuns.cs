#nullable enable
using DemonFighter.App;
using DemonFighter.UI;
using UnityEngine;

namespace DemonFighter.PlayMode.Tests
{
    /// <summary>
    /// Ends every run a test left behind. Each Play Mode test boots its own persistent application, and a run still
    /// open when the test session quits would save itself (D-074) and offer Continue in the next editor session; a
    /// hint shown during a test would stay hidden in the next real run (D-076).
    /// </summary>
    internal static class TestRuns
    {
        public static void EndAll()
        {
            Bootstrapper[] bootstrappers = Object.FindObjectsByType<Bootstrapper>(FindObjectsSortMode.None);
            foreach (Bootstrapper bootstrapper in bootstrappers)
            {
                bootstrapper.RunController?.EndRun();
            }

            HintMemory.Reset();
        }
    }
}
