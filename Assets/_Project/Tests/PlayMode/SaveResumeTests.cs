#nullable enable
using System.Collections;
using DemonFighter.App;
using DemonFighter.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DemonFighter.PlayMode.Tests
{
    /// <summary>
    /// Saves a running run through the controller, returns to the menu, finds Continue and resumes: the one loop that
    /// only Play Mode can prove, since it crosses scenes, the save file and the rebuilt views (D-074).
    /// </summary>
    public sealed class SaveResumeTests
    {
        private const int MaxFramesToWait = 600;
        private const string NewRunButtonName = "new-run";
        private const string ContinueButtonName = "continue";
        private const string WorldRootName = "World";

        [SetUp]
        public void EmptyTheSlot()
        {
            new RunSaveService().Delete();
        }

        [TearDown]
        public void LeaveNoRunAndNoSaveBehind()
        {
            TestRuns.EndAll();
            new RunSaveService().Delete();
        }

        [UnityTest]
        public IEnumerator SaveAndQuit_ThenContinue_ResumesTheSameRun()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneNames.MainMenu && Object.FindAnyObjectByType<MainMenuScreen>() != null,
                "the main menu did not appear");
            MainMenuScreen menu = Object.FindAnyObjectByType<MainMenuScreen>();
            yield return null;
            Assert.That(menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>(ContinueButtonName).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "Continue shows without a save");
            Submit(menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>(NewRunButtonName));
            yield return WaitUntil(() => GameObject.Find(WorldRootName) != null, "the world was not built");
            yield return null;
            RunController controller = RunningController();
            yield return WaitUntil(() => controller.CurrentRun != null && controller.CurrentRun.Tick >= 5, "the run did not tick");
            int seed = controller.CurrentRun!.Seed;
            long tickBefore = controller.CurrentRun.Tick;
            int demonsBefore = controller.CurrentRun.Demons.Count;

            controller.SaveAndQuit();
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneNames.MainMenu && Object.FindAnyObjectByType<MainMenuScreen>() != null,
                "the main menu did not return after Save and Quit");
            Assert.That(controller.HasSavedRun, Is.True, "Save and Quit left no save");
            menu = Object.FindAnyObjectByType<MainMenuScreen>();
            yield return null;
            Button resume = menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>(ContinueButtonName);
            Assert.That(resume.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Continue is hidden although a save exists");

            Submit(resume);
            yield return WaitUntil(() => GameObject.Find(WorldRootName) != null, "the saved world was not rebuilt");
            yield return null;
            yield return null;

            Assert.That(controller.CurrentRun, Is.Not.Null, "no run after Continue");
            Assert.That(controller.CurrentRun!.Seed, Is.EqualTo(seed));
            Assert.That(controller.CurrentRun.Tick, Is.GreaterThanOrEqualTo(tickBefore));
            Assert.That(controller.CurrentRun.Demons.Count, Is.EqualTo(demonsBefore));
            Assert.That(controller.HasSavedRun, Is.False, "resuming did not empty the slot");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.Run));
        }

        // Earlier tests leave their own persistent bootstrappers behind; the one with a run is ours.
        private static RunController RunningController()
        {
            Bootstrapper[] bootstrappers = Object.FindObjectsByType<Bootstrapper>(FindObjectsSortMode.None);
            foreach (Bootstrapper bootstrapper in bootstrappers)
            {
                if (bootstrapper.RunController != null && bootstrapper.RunController.CurrentRun != null)
                {
                    return bootstrapper.RunController;
                }
            }

            Assert.Fail("No bootstrapper owns a running run.");
            return null!;
        }

        private static void Submit(Button button)
        {
            Assert.That(button, Is.Not.Null, "A button the test needs is missing.");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
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
