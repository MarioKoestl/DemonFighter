#nullable enable
using System;
using System.Collections.Generic;
using DemonFighter.Common;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonFighter.Input
{
    /// <summary>
    /// The only class in the project that reads input (ARCHITECTURE, "Input"). Reads the Gameplay action map and
    /// submits, per tick, one move command (WASD relative to the camera, facing the camera while attacking), a skill
    /// use for every attack key pressed, resolved through the skill slot the key stands for (GAME_DESIGN,
    /// "Controls": left mouse Bite or Claw, right mouse Grab, Space Lunge, Q Tail Swing), and an eat command while Eat
    /// is held on food; mouse look and the view toggle go to the camera every frame. The menu keys and the Analyze
    /// key (F, D-066) are raised at once, so the menu keys also work while the run is paused. Adding a gamepad is a
    /// binding change.
    /// </summary>
    public sealed class PlayerInputAdapter : ICommandSource, IFrameUpdatable, IDisposable
    {
        private const string GameplayMap = "Gameplay";
        private const string MoveAction = "Move";
        private const string LookAction = "Look";
        private const string SprintAction = "Sprint";
        private const string PrimaryAttackAction = "PrimaryAttack";
        private const string SecondaryAttackAction = "SecondaryAttack";
        private const string LungeAction = "Lunge";
        private const string TailSwingAction = "TailSwing";
        private const string EatAction = "Eat";
        private const string MutationMenuAction = "MutationMenu";
        private const string StatsMenuAction = "StatsMenu";
        private const string ToggleCameraAction = "ToggleCamera";
        private const string AnalyzeAction = "Analyze";

        private readonly InputActionMap _gameplay;
        private readonly InputAction _move;
        private readonly InputAction _look;
        private readonly InputAction _sprint;
        private readonly InputAction _eat;
        private readonly InputAction _mutationMenu;
        private readonly InputAction _statsMenu;
        private readonly InputAction _toggleCamera;
        private readonly InputAction _analyze;
        private readonly List<SlotBinding> _attacks = new List<SlotBinding>();
        private readonly List<SkillSlot> _requestedSlots = new List<SkillSlot>();
        private readonly IHeadingProvider _heading;
        private readonly ICameraControl _camera;
        private readonly IPlayerAim _aim;
        private readonly Demon _player;
        private bool _toggleRequested;

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
            _eat = _gameplay.FindAction(EatAction, throwIfNotFound: true);
            _mutationMenu = _gameplay.FindAction(MutationMenuAction, throwIfNotFound: true);
            _statsMenu = _gameplay.FindAction(StatsMenuAction, throwIfNotFound: true);
            _toggleCamera = _gameplay.FindAction(ToggleCameraAction, throwIfNotFound: true);
            _analyze = _gameplay.FindAction(AnalyzeAction, throwIfNotFound: true);
            _attacks.Add(new SlotBinding(_gameplay.FindAction(PrimaryAttackAction, throwIfNotFound: true), SkillSlot.Primary, OnAttack));
            _attacks.Add(new SlotBinding(_gameplay.FindAction(SecondaryAttackAction, throwIfNotFound: true), SkillSlot.Secondary, OnAttack));
            _attacks.Add(new SlotBinding(_gameplay.FindAction(LungeAction, throwIfNotFound: true), SkillSlot.Lunge, OnAttack));
            _attacks.Add(new SlotBinding(_gameplay.FindAction(TailSwingAction, throwIfNotFound: true), SkillSlot.TailSwing, OnAttack));
            _toggleCamera.performed += OnToggleCamera;
            _analyze.performed += OnAnalyze;
            _mutationMenu.performed += OnMutationMenu;
            _statsMenu.performed += OnStatsMenu;
            _gameplay.Enable();
        }

        /// <summary>Raised when a menu key is pressed, with the tab it stands for; raised from the input callback, so it fires while paused too.</summary>
        public event Action<MenuTab>? MenuToggled;

        /// <summary>Raised when the Analyze key is pressed; the run controller locks or releases the target under the crosshair (D-066).</summary>
        public event Action? AnalyzeRequested;

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
            // While attacking the body faces the camera, so the hit lands where the crosshair points.
            bool aiming = _requestedSlots.Count > 0 || _player.CurrentSkillUse != null;
            System.Numerics.Vector2 facing = aiming ? HeadingDirection() : System.Numerics.Vector2.Zero;
            Vector2 input = _move.ReadValue<Vector2>();
            commands.Submit(new MoveCommand(_player.Id, ToWorldDirection(input), _sprint.IsPressed(), facing));

            for (int i = 0; i < _requestedSlots.Count; i++)
            {
                SkillInstance? skill = SkillSlots.Find(_player, _requestedSlots[i]);
                if (skill != null)
                {
                    commands.Submit(new UseSkillCommand(_player.Id, skill.Spec.Id));
                }
            }

            _requestedSlots.Clear();

            if (_eat.IsPressed() && _aim.AimedFood.IsValid)
            {
                commands.Submit(new EatCommand(_player.Id, _aim.AimedFood));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _toggleCamera.performed -= OnToggleCamera;
            _analyze.performed -= OnAnalyze;
            _mutationMenu.performed -= OnMutationMenu;
            _statsMenu.performed -= OnStatsMenu;
            for (int i = 0; i < _attacks.Count; i++)
            {
                _attacks[i].Dispose();
            }

            _gameplay.Disable();
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

        private void OnAttack(SkillSlot slot)
        {
            if (!_requestedSlots.Contains(slot))
            {
                _requestedSlots.Add(slot);
            }
        }

        private void OnToggleCamera(InputAction.CallbackContext context)
        {
            _toggleRequested = true;
        }

        private void OnAnalyze(InputAction.CallbackContext context)
        {
            AnalyzeRequested?.Invoke();
        }

        private void OnMutationMenu(InputAction.CallbackContext context)
        {
            MenuToggled?.Invoke(MenuTab.Mutate);
        }

        private void OnStatsMenu(InputAction.CallbackContext context)
        {
            MenuToggled?.Invoke(MenuTab.Stats);
        }

        // One key, one slot: the callback remembers which slot was pressed until the next tick submits it.
        private sealed class SlotBinding : IDisposable
        {
            private readonly InputAction _action;
            private readonly SkillSlot _slot;
            private readonly Action<SkillSlot> _pressed;

            public SlotBinding(InputAction action, SkillSlot slot, Action<SkillSlot> pressed)
            {
                _action = action;
                _slot = slot;
                _pressed = pressed;
                _action.performed += OnPerformed;
            }

            public void Dispose()
            {
                _action.performed -= OnPerformed;
            }

            private void OnPerformed(InputAction.CallbackContext context)
            {
                _pressed(_slot);
            }
        }
    }
}
