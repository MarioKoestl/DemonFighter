#nullable enable
using System.Collections;
using DemonFighter.App;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DemonFighter.PlayMode.Tests
{
    /// <summary>
    /// Scene wiring that only Play Mode can prove: the generated Bootstrap scene must hand over to the main menu.
    /// </summary>
    public sealed class BootstrapFlowTests
    {
        private const int MaxFramesToWait = 600;

        [UnityTest]
        public IEnumerator LoadBootstrap_WhenPlayed_OpensTheMainMenu()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);

            for (int frame = 0; frame < MaxFramesToWait; frame++)
            {
                if (SceneManager.GetActiveScene().name == SceneNames.MainMenu)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));
        }
    }
}
