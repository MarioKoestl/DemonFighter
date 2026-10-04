#nullable enable
using System.Collections;
using DemonFighter.App;
using DemonFighter.Presentation.Demons;
using DemonFighter.Simulation.Worldgen;
using DemonFighter.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DemonFighter.PlayMode.Tests
{
    /// <summary>
    /// Drives the real flow from Bootstrap through the New Run button into a built world: the integration only
    /// Play Mode can prove (world built, every demon has a body, nothing threw).
    /// </summary>
    public sealed class RunStartTests
    {
        private const int MaxFramesToWait = 600;
        private const string NewRunButtonName = "new-run";
        private const string WorldRootName = "World";

        [UnityTest]
        public IEnumerator NewRun_FromTheMainMenu_BuildsTheWorldAndSpawnsEveryDemon()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);

            // The load applies next frame; waiting first keeps a menu left over from an earlier test out of the way.
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneNames.MainMenu && Object.FindAnyObjectByType<MainMenuScreen>() != null,
                "the main menu did not appear");

            MainMenuScreen menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Button newRun = menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>(NewRunButtonName);
            Assert.That(newRun, Is.Not.Null, "The main menu has no New Run button.");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = newRun;
                newRun.SendEvent(submit);
            }

            yield return WaitUntil(() => GameObject.Find(WorldRootName) != null, "the world was not built");
            yield return null;
            yield return null;

            DemonView[] views = Object.FindObjectsByType<DemonView>(FindObjectsSortMode.None);
            Assert.That(views.Length, Is.EqualTo(BiomeSpec.AshCavern.InitialBlobs + 2), "player, blobs and elder each need a body");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.Run));
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, string message)
        {
            for (int frame = 0; frame < MaxFramesToWait; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.That(condition(), Is.True, message);
        }
    }
}
