#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonFighter.Input
{
    /// <summary>
    /// The only class in the project that reads input (ARCHITECTURE, "Input"). Reads the Gameplay action map and
    /// submits, per tick, one move command (WASD relative to the camera, facing the camera while attacking), a skill
    /// use for each attack press and an eat command while Eat is held on food; mouse look and the view toggle go to
    /// the camera every frame. The stats menu key is raised at once, so it also works while the run is paused.
    /// Adding a gamepad is a binding change.
    /// </summary>
    public sealed class PlayerInputAdapter : ICommandSource, IFrameUpdatable, IDisposable
    {
        private const string GameplayMap = "Gameplay";
        private const string MoveAction = "Move";
        private const string LookAction = "Look";
        private const string SprintAction = "Sprint";
        private const string PrimaryAttackAction = "PrimaryAttack";
        private const string EatAction = "Eat";
        private const string StatsMenuAction = "StatsMenu";
        private const string ToggleCameraAction = "ToggleCamera";

        private readonly InputActionMap _gameplay;
        private readonly InputAction _move;
        private readonly InputAction _look;
        private readonly InputAction _sprint;
        private readonly InputAction _primaryAttack;
        private readonly InputAction _eat;
        private readonly InputAction _statsMenu;
        private readonly InputAction _toggleCamera;
        private readonly IHeadingProvider _heading;
        private readonly ICameraControl _camera;
        private readonly IPlayerAim _aim;
        private readonly Demon _player;
        private bool _toggleRequested;
        private bool _attackRequested;

        public PlayerInputAdapter(InputActionAsset actions, Demon player, IHeadingProvider heading, ICameraControl camera, IPlayerAim aim)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            _heading = heading ?? throw new ArgumentNullException(nameof(heading));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _aim = aim ?? throw new ArgumentNullException(nameof(aim));
            _gameplay = actions.FindActionMap(GameplayMap, throwIfNotFound: true);
            _move = _gameplay.FindAction(MoveAction, throwIfNotFound: true);
            _look = _gameplay.FindAction(LookAction, throwIfNotFound: true);
            _sprint = _gameplay.FindAction(SprintAction, throwIfNotFound: true);
            _primaryAttack = _gameplay.FindAction(PrimaryAttackAction, throwIfNotFound: true);
            _eat = _gameplay.FindAction(EatAction, throwIfNotFound: true);
            _statsMenu = _gameplay.FindAction(StatsMenuAction, throwIfNotFound: true);
            _toggleCamera = _gameplay.FindAction(ToggleCameraAction, throwIfNotFound: true);
            _toggleCamera.performed += OnToggleCamera;
            _primaryAttack.performed += OnPrimaryAttack;
            _statsMenu.performed += OnStatsMenu;
            _gameplay.Enable();
        }

        /// <summary>Raised when the stats menu key is pressed, from the input callback, so it fires while paused too.</summary>
        public event Action? StatsMenuToggled;

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
            // While attacking the body faces the camera, so the bite lands where the crosshair points.
            bool aiming = _attackRequested || _player.CurrentSkillUse != null;
            System.Numerics.Vector2 facing = aiming ? HeadingDirection() : System.Numerics.Vector2.Zero;
            Vector2 input = _move.ReadValue<Vector2>();
            commands.Submit(new MoveCommand(_player.Id, ToWorldDirection(input), _sprint.IsPressed(), facing));

            if (_attackRequested)
            {
                _attackRequested = false;
                SkillInstance? skill = PrimarySkill();
                if (skill != null)
                {
                    commands.Submit(new UseSkillCommand(_player.Id, skill.Spec.Id));
                }
            }

            if (_eat.IsPressed() && _aim.AimedFood.IsValid)
            {
                commands.Submit(new EatCommand(_player.Id, _aim.AimedFood));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _toggleCamera.performed -= OnToggleCamera;
            _primaryAttack.performed -= OnPrimaryAttack;
            _statsMenu.performed -= OnStatsMenu;
            _gameplay.Disable();
        }

        // The first skill the body still grants is the primary attack: Bite for a blob, Claw once an arm exists (M3).
        private SkillInstance? PrimarySkill()
        {
            IReadOnlyList<SkillInstance> skills = _player.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].IsGrantedBy(_player.Body))
                {
                    return skills[i];
                }
            }

            return null;
        }

        private System.Numerics.Vector2 HeadingDirection()
        {
            float yaw = _heading.YawRadians;
            return new System.Numerics.Vector2(MathF.Sin(yaw), MathF.Cos(yaw));
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

        private void OnPrimaryAttack(InputAction.CallbackContext context)
        {
            _attackRequested = true;
        }

        private void OnStatsMenu(InputAction.CallbackContext context)
        {
            StatsMenuToggled?.Invoke();
        }
    }
}
