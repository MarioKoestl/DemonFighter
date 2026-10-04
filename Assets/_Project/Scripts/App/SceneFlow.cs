#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DemonFighter.App
{
    /// <summary>
    /// Moves the application between scenes: Bootstrap, then MainMenu, then Run (ARCHITECTURE, "App"). Scene objects
    /// that need wiring get it here, right after their scene has loaded, instead of fetching services from a static.
    /// </summary>
    internal sealed class SceneFlow
    {
        private readonly RunController _runController;
        private MainMenuScreen? _mainMenu;

        public SceneFlow(RunController runController)
        {
            _runController = runController ?? throw new ArgumentNullException(nameof(runController));
        }

        /// <summary>Loads the main menu and listens for the New Run request.</summary>
        public async Awaitable ShowMainMenuAsync()
        {
            await LoadSceneAsync(SceneNames.MainMenu);
            _mainMenu = Object.FindAnyObjectByType<MainMenuScreen>();
            if (_mainMenu == null)
            {
                Log.Error(LogCategory.App, "The MainMenu scene contains no MainMenuScreen; regenerate the scenes.");
                return;
            }

            _mainMenu.NewRunRequested += OnNewRunRequested;
        }

        private async void OnNewRunRequested()
        {
            if (_mainMenu != null)
            {
                _mainMenu.NewRunRequested -= OnNewRunRequested;
                _mainMenu = null;
            }

            try
            {
                await LoadSceneAsync(SceneNames.Run);
                _runController.StartRun(NewSeed());
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Starting a run failed.", exception);
            }
        }

        private static async Awaitable LoadSceneAsync(string sceneName)
        {
            AsyncOperation? operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                throw new InvalidOperationException("Scene " + sceneName + " is not in the build list.");
            }

            await operation;
        }

        // The seed is the one piece of randomness outside the simulation; the run itself only uses its own Rng.
        private static int NewSeed()
        {
            return new System.Random().Next(1, int.MaxValue);
        }
    }
}
