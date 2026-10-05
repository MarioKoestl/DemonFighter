#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Presentation.Audio;
using DemonFighter.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DemonFighter.App
{
    /// <summary>
    /// Moves the application between scenes: Bootstrap, then MainMenu, then Run, and back to the menu when a run is
    /// over or saved (ARCHITECTURE, "App"). Scene objects that need wiring get it here, right after their scene has
    /// loaded, instead of fetching services from a static. The main menu's settings go through the applier (D-085).
    /// </summary>
    internal sealed class SceneFlow
    {
        private readonly RunController _runController;
        private readonly SettingsApplier _applier;
        private readonly MusicPlayer _music;
        private readonly AudioClip? _menuTrack;
        private MainMenuScreen? _mainMenu;

        public SceneFlow(RunController runController, SettingsApplier applier, MusicPlayer music, AudioClip? menuTrack)
        {
            _runController = runController ?? throw new ArgumentNullException(nameof(runController));
            _applier = applier ?? throw new ArgumentNullException(nameof(applier));
            _music = music != null ? music : throw new ArgumentNullException(nameof(music));
            _menuTrack = menuTrack;
            _runController.ReturnToMenuRequested += OnReturnToMenuRequested;
        }

        /// <summary>Loads the main menu, shows Continue when a save exists, fills the settings and listens for the buttons.</summary>
        public async Awaitable ShowMainMenuAsync()
        {
            await LoadSceneAsync(SceneNames.MainMenu);
            _music.Play(_menuTrack);
            _mainMenu = Object.FindAnyObjectByType<MainMenuScreen>();
            if (_mainMenu == null)
            {
                Log.Error(LogCategory.App, "The MainMenu scene contains no MainMenuScreen; regenerate the scenes.");
                return;
            }

            _mainMenu.ShowContinue(_runController.HasSavedRun);
            _mainMenu.ConfigureSettings(_applier.ToValues());
            _mainMenu.NewRunRequested += OnNewRunRequested;
            _mainMenu.ContinueRequested += OnContinueRequested;
            _mainMenu.QuitRequested += OnQuitRequested;
            _mainMenu.SettingsChanged += OnSettingsChanged;
            _mainMenu.ResetHintsRequested += OnResetHintsRequested;
        }

        /// <summary>Leaves the application; in the editor it stops Play Mode instead.</summary>
        public static void Quit()
        {
            Log.Info(LogCategory.App, "Quit requested.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private async void OnNewRunRequested(int? seed)
        {
            DetachMenu();
            try
            {
                await LoadSceneAsync(SceneNames.Run);
                _runController.StartRun(seed ?? NewSeed());
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
                _mainMenu.QuitRequested -= OnQuitRequested;
                _mainMenu.SettingsChanged -= OnSettingsChanged;
                _mainMenu.ResetHintsRequested -= OnResetHintsRequested;
                _mainMenu = null;
            }
        }

        private void OnSettingsChanged(SettingsValues values)
        {
            _applier.Apply(values);
        }

        private static void OnQuitRequested()
        {
            Quit();
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
