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
    /// Moves the application between scenes: Bootstrap, then MainMenu, then Run, and back to the menu when a run is
    /// over or saved (ARCHITECTURE, "App"). Scene objects that need wiring get it here, right after their scene has
    /// loaded, instead of fetching services from a static.
    /// </summary>
    internal sealed class SceneFlow
    {
        private readonly RunController _runController;
        private readonly GameSettings _settings;
        private MainMenuScreen? _mainMenu;

        public SceneFlow(RunController runController, GameSettings settings)
        {
            _runController = runController ?? throw new ArgumentNullException(nameof(runController));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _runController.ReturnToMenuRequested += OnReturnToMenuRequested;
        }

        /// <summary>Loads the main menu, shows Continue when a save exists and listens for New Run and Continue.</summary>
        public async Awaitable ShowMainMenuAsync()
        {
            await LoadSceneAsync(SceneNames.MainMenu);
            _mainMenu = Object.FindAnyObjectByType<MainMenuScreen>();
            if (_mainMenu == null)
            {
                Log.Error(LogCategory.App, "The MainMenu scene contains no MainMenuScreen; regenerate the scenes.");
                return;
            }

            _mainMenu.ShowContinue(_runController.HasSavedRun);
            _mainMenu.ShowSettings(_settings.RandomOffers);
            _mainMenu.NewRunRequested += OnNewRunRequested;
            _mainMenu.ContinueRequested += OnContinueRequested;
            _mainMenu.RandomOffersToggled += OnRandomOffersToggled;
            _mainMenu.ResetHintsRequested += OnResetHintsRequested;
        }

        private async void OnNewRunRequested()
        {
            DetachMenu();
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

        // Continue rebuilds the saved run in the Run scene (D-074); an unreadable save starts nothing and the menu returns.
        private async void OnContinueRequested()
        {
            DetachMenu();
            try
            {
                await LoadSceneAsync(SceneNames.Run);
                if (!_runController.ResumeSavedRun())
                {
                    Log.Warn(LogCategory.App, "No usable saved run; back to the main menu.");
                    await ShowMainMenuAsync();
                }
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Resuming the run failed.", exception);
            }
        }

        // The death screen or Save and Quit asked for the menu: the run is torn down first, then the menu scene replaces the Run scene.
        private async void OnReturnToMenuRequested()
        {
            try
            {
                _runController.EndRun();
                await ShowMainMenuAsync();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Returning to the main menu failed.", exception);
            }
        }

        private void DetachMenu()
        {
            if (_mainMenu != null)
            {
                _mainMenu.NewRunRequested -= OnNewRunRequested;
                _mainMenu.ContinueRequested -= OnContinueRequested;
                _mainMenu.RandomOffersToggled -= OnRandomOffersToggled;
                _mainMenu.ResetHintsRequested -= OnResetHintsRequested;
                _mainMenu = null;
            }
        }

        // The settings box of the menu (D-076): the offers policy of the next run, and the first-run hints.
        private void OnRandomOffersToggled(bool randomOffers)
        {
            _settings.RandomOffers = randomOffers;
        }

        private static void OnResetHintsRequested()
        {
            HintMemory.Reset();
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
