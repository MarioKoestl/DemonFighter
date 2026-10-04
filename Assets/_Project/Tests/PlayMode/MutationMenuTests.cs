#nullable enable
using System.Collections;
using DemonFighter.App;
using DemonFighter.Common;
using DemonFighter.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DemonFighter.PlayMode.Tests
{
    /// <summary>
    /// Opens the mutation menu in a real run and walks its four tabs: the integration only Play Mode can prove (the
    /// menu builds from the catalog, every tab refreshes without throwing, the HUD reports it open and closed again).
    /// </summary>
    public sealed class MutationMenuTests
    {
        private const int MaxFramesToWait = 600;
        private const string NewRunButtonName = "new-run";
        private const string WorldRootName = "World";

        [TearDown]
        public void LeaveNoRunBehind()
        {
            TestRuns.EndAll();
        }

        [UnityTest]
        public IEnumerator MutationMenu_InARun_OpensEveryTabAndClosesAgain()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);

            // The load applies next frame; waiting first keeps a menu left over from an earlier test out of the way.
            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneNames.MainMenu && Object.FindAnyObjectByType<MainMenuScreen>() != null,
                "the main menu did not appear");

            MainMenuScreen menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Submit(menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>(NewRunButtonName));
            yield return WaitUntil(() => GameObject.Find(WorldRootName) != null, "the world was not built");
            yield return null;
            yield return null;

            HudScreen hud = Object.FindAnyObjectByType<HudScreen>();
            Assert.That(hud, Is.Not.Null, "The Run scene has no HUD.");
            VisualElement root = hud.GetComponent<UIDocument>().rootVisualElement;

            Assert.That(hud.ToggleMenu(MenuTab.Mutate), Is.True, "the menu did not open");
            Assert.That(hud.IsMenuOpen, Is.True);
            yield return null;
            Assert.That(root.Q("mutation-menu").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q("offer-list").childCount, Is.GreaterThan(0), "the shop lists no offers");
            Assert.That(root.Q("offer-list").Q<Button>("select"), Is.Not.Null, "an offer has no select button");
            Assert.That(root.Q<Button>("apply-mutations").enabledSelf, Is.False, "Apply is enabled with nothing selected");
            Assert.That(root.Q<Button>("reset-mutations"), Is.Not.Null, "the Mutate tab has no Reset button");
            Assert.That(root.Q("body-list").childCount, Is.GreaterThan(1), "the body shows no sockets");
            VisualElement preview = root.Q("body-preview");
            Assert.That(preview, Is.Not.Null, "the Mutate tab has no body preview");
            Assert.That(preview.resolvedStyle.backgroundImage.renderTexture, Is.Not.Null, "the body preview shows no render texture");

            Submit(root.Q<Button>("tab-Evolve"));
            yield return null;
            Assert.That(root.Q("page-Evolve").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<Label>("evolve-status").text, Does.Contain("level"));

            Submit(root.Q<Button>("tab-Stats"));
            yield return null;
            Assert.That(root.Q("stats-panel").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<Button>("apply"), Is.Not.Null, "the Stats tab has no Apply button");

            Submit(root.Q<Button>("tab-Skills"));
            yield return null;
            Assert.That(root.Q("skill-list").childCount, Is.GreaterThan(0), "the blob has no skill to show");

            Assert.That(hud.ToggleMenu(MenuTab.Stats), Is.False, "the menu did not close");
            Assert.That(hud.IsMenuOpen, Is.False);
            yield return null;
            Assert.That(root.Q("mutation-menu").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
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
