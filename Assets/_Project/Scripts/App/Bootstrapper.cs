#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Data;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Worldgen;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonFighter.App
{
    /// <summary>
    /// Entry point of the application and the only object in the Bootstrap scene. Loads the content it references,
    /// builds the composition root once, survives scene loads and opens the main menu. Nothing else may use
    /// DontDestroyOnLoad (CODING_GUIDELINES).
    /// </summary>
    internal sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private BiomeDefinition _biome = null!;
        [SerializeField] private InputActionAsset _actions = null!;

        private RunController? _runController;
        private SceneFlow? _sceneFlow;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (_biome == null || _actions == null)
            {
                Log.Error(LogCategory.App, "Bootstrapper is missing its biome or actions reference; run Demon Fighter > Generate > Scenes.", this);
                return;
            }

            try
            {
                var services = new GameServices(
                    SimulationConfig.Default,
                    new SimulationEvents(),
                    _biome.ToSpec(),
                    new CavernWorldGenerator(),
                    _actions);
                _runController = new RunController(services);
                _sceneFlow = new SceneFlow(_runController);
                Log.Info(LogCategory.App, "Bootstrap complete with biome " + services.Biome.Id + ".");
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.App, "Bootstrap failed.", exception, this);
            }
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
