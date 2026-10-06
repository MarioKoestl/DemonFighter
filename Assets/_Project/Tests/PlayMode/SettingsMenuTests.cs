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
    /// Proves the settings panel is wired into the main menu (D-085): Settings opens it with the four tabs and the
    /// controls filled, Back returns to the buttons. Writing the file is covered by the EditMode tests of GameSettings.
    /// </summary>
    public sealed class SettingsMenuTests
    {
        private const int MaxFramesToWait = 600;

        [TearDown]
        public void LeaveNoRunBehind()
        {
            TestRuns.EndAll();
        }

        [UnityTest]
        public IEnumerator Settings_FromTheMainMenu_OpensThePanelAndBackReturns()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);

            yield return null;
            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneNames.MainMenu && Object.FindAnyObjectByType<MainMenuScreen>() != null,
                "the main menu did not appear");
            yield return null;

            MainMenuScreen menu = Object.FindAnyObjectByType<MainMenuScreen>();
            VisualElement root = menu.GetComponent<UIDocument>().rootVisualElement;
            VisualElement panel = root.Q("settings-panel");
            VisualElement list = root.Q("menu-list");
            Assert.That(panel, Is.Not.Null, "The main menu has no settings panel.");
            Assert.That(root.Q<Button>("quit"), Is.Not.Null, "The main menu has no Quit button.");
            Assert.That(root.Q<TextField>("seed"), Is.Not.Null, "The main menu has no seed field.");
            Assert.That(panel.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "the panel shows before Settings is clicked");

            Submit(root.Q<Button>("settings"));
            yield return null;

            Assert.That(panel.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Settings did not open the panel");
            Assert.That(list.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "the menu buttons stayed behind the panel");
            Assert.That(root.Q<DropdownField>("preset").choices.Count, Is.EqualTo(3), "the preset list is not filled from the catalog");
            Assert.That(root.Q<DropdownField>("resolution").choices.Count, Is.GreaterThan(0), "the resolution list is empty");
            Assert.That(root.Q<Slider>("master").value, Is.InRange(0f, 1f));

            Submit(root.Q<Button>("settings-tab-Audio"));
            yield return null;
            Assert.That(root.Q("settings-page-Audio").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "the Audio tab did not open");
            Assert.That(root.Q("settings-page-Graphics").resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "the Graphics tab stayed open");

            Submit(root.Q<Button>("back"));
            yield return null;

            Assert.That(panel.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "Back did not close the panel");
            Assert.That(list.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Back did not bring the menu buttons back");
        }

        private static void Submit(Button button)
        {
            Assert.That(button, Is.Not.Null, "a button is missing");
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
