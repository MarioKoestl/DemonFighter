#nullable enable
using System;
using DemonFighter.Common;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonFighter.Input
{
    /// <summary>
    /// The only class in the project that reads input (ARCHITECTURE, "Input"). Reads the Gameplay action map, turns
    /// WASD into a world direction relative to the camera and submits one move command per tick for the player's
    /// demon; mouse look and the view toggle go to the camera every frame. Adding a gamepad is a binding change.
    /// </summary>
    public sealed class PlayerInputAdapter : ICommandSource, IFrameUpdatable, IDisposable
    {
        private const string GameplayMap = "Gameplay";
        private const string MoveAction = "Move";
        private const string LookAction = "Look";
        private const string SprintAction = "Sprint";
        private const string ToggleCameraAction = "ToggleCamera";

        private readonly InputActionMap _gameplay;
        private readonly InputAction _move;
        private readonly InputAction _look;
        private readonly InputAction _sprint;
        private readonly InputAction _toggleCamera;
        private readonly IHeadingProvider _heading;
        private readonly ICameraControl _camera;
        private readonly DemonId _player;
        private bool _toggleRequested;

        public PlayerInputAdapter(InputActionAsset actions, DemonId player, IHeadingProvider heading, ICameraControl camera)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            if (!player.IsValid)
            {
                throw new ArgumentException("The player needs an issued demon id.", nameof(player));
            }

            _heading = heading ?? throw new ArgumentNullException(nameof(heading));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _player = player;
            _gameplay = actions.FindActionMap(GameplayMap, throwIfNotFound: true);
            _move = _gameplay.FindAction(MoveAction, throwIfNotFound: true);
            _look = _gameplay.FindAction(LookAction, throwIfNotFound: true);
            _sprint = _gameplay.FindAction(SprintAction, throwIfNotFound: true);
            _toggleCamera = _gameplay.FindAction(ToggleCameraAction, throwIfNotFound: true);
            _toggleCamera.performed += OnToggleCamera;
            _gameplay.Enable();
        }

        /// <inheritdoc />
        public void UpdateFrame()
        {
            Vector2 look = _look.ReadValue<Vector2>();
            if (look != Vector2.zero)
            {
                _camera.AddLook(look);
            }

            if (_toggleRequested)
            {
                _toggleRequested = false;
                _camera.ToggleView();
            }
        }

        /// <inheritdoc />
        public void SubmitCommands(CommandQueue commands)
        {
            Vector2 input = _move.ReadValue<Vector2>();
            commands.Submit(new MoveCommand(_player, ToWorldDirection(input), _sprint.IsPressed()));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _toggleCamera.performed -= OnToggleCamera;
            _gameplay.Disable();
        }

        // Stick axes are right and forward relative to the camera; rotate them by the camera yaw into east and north.
        private System.Numerics.Vector2 ToWorldDirection(Vector2 input)
        {
            float yaw = _heading.YawRadians;
            float sin = MathF.Sin(yaw);
            float cos = MathF.Cos(yaw);
            float east = input.x * cos + input.y * sin;
            float north = -input.x * sin + input.y * cos;
            return new System.Numerics.Vector2(east, north);
        }

        private void OnToggleCamera(InputAction.CallbackContext context)
        {
            _toggleRequested = true;
        }
    }
}
