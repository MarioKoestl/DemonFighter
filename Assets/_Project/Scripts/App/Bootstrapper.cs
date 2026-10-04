#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Events;
using UnityEngine;

namespace DemonFighter.App
{
    /// <summary>
    /// Entry point of the application and the only object in the Bootstrap scene. Builds the composition root once,
    /// survives scene loads and opens the main menu. Nothing else may use DontDestroyOnLoad (CODING_GUIDELINES).
    /// </summary>
    internal sealed class Bootstrapper : MonoBehaviour
    {
        private RunController? _runController;
        private SceneFlow? _sceneFlow;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            var services = new GameServices(SimulationConfig.Default, new SimulationEvents());
            _runController = new RunController(services);
            _sceneFlow = new SceneFlow(_runController);
            Log.Info(LogCategory.App, "Bootstrap complete.");
        }

        private async void Start()
        {
            if (_sceneFlow == null)
            {
                return;
            }

            try
            {
                await _sceneFlow.ShowMainMenuAsync();
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Opening the main menu failed.", exception, this);
            }
        }

        private void OnDestroy()
        {
            if (_runController != null)
            {
                _runController.EndRun();
            }
        }
    }
}
