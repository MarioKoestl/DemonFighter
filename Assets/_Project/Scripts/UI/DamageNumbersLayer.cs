#nullable enable
using System;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// Draws the damage numbers of the player's hits and of the damage it takes (D-092): a full-screen layer under the HUD
    /// panels with a small pool of labels, placed each frame by projecting the target's head into the panel. A target
    /// that died or vanished keeps its number where it last stood.
    /// </summary>
    public sealed class DamageNumbersLayer : IDisposable
    {
        private const int PoolSize = 24;
        private const float BaseFontSize = 26f;
        private const float BigHitFontBonus = 10f;
        private const float BigHitAmount = 25f;

        private readonly FloatingDamage _numbers = new FloatingDamage();
        private readonly Label[] _labels = new Label[PoolSize];
        private RunState? _state;
        private DemonId _player = DemonId.None;
        private Func<DemonId, float>? _stanceOf;
        private IDisposable? _subscription;
        private IDisposable? _blockedSubscription;

        public DamageNumbersLayer()
        {
            Root = new VisualElement { name = "damage-numbers", pickingMode = PickingMode.Ignore };
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.top = 0;
            Root.style.right = 0;
            Root.style.bottom = 0;
            for (int i = 0; i < PoolSize; i++)
            {
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.style.position = Position.Absolute;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.unityTextOutlineWidth = 1.5f;
                label.style.unityTextOutlineColor = new Color(0.08f, 0.02f, 0.02f, 0.9f);
                label.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-100f));
                label.style.display = DisplayStyle.None;
                _labels[i] = label;
                Root.Add(label);
            }
        }

        /// <summary>The layer to put into the HUD, under its panels.</summary>
        public VisualElement Root { get; }

        /// <summary>Listens to a new run's damage; numbers of an earlier run are dropped. The stance callback says how high a core stands on its legs (D-094).</summary>
        public void Bind(RunState state, SimulationEvents events, DemonId player, Func<DemonId, float>? stanceOf = null)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _subscription?.Dispose();
            _blockedSubscription?.Dispose();
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _player = player;
            _stanceOf = stanceOf;
            _numbers.Clear();
            _subscription = events.Subscribe<DamageApplied>(OnDamage);
            _blockedSubscription = events.Subscribe<DamageBlocked>(OnBlocked);
        }

        /// <summary>Ages the numbers and places their labels; call once per frame with the camera the player sees through.</summary>
        public void Update(float deltaTime, Camera? camera)
        {
            _numbers.Advance(deltaTime);
            int shown = 0;
            if (camera != null && Root.panel != null)
            {
                for (int i = 0; i < _numbers.Numbers.Count && shown < PoolSize; i++)
                {
                    FloatingDamage.Number number = _numbers.Numbers[i];
                    Vector3 world = number.WorldPosition(FeetOf(number));
                    if (camera.WorldToViewportPoint(world).z <= 0f)
                    {
                        continue;
                    }

                    Vector2 point = RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, world, camera);
                    Label label = _labels[shown];
                    label.text = FloatingDamage.Text(number.Amount, number.Blocked);
                    Color color = number.Taken ? FloatingDamage.TakenColor : FloatingDamage.ColorFor(number.Type);
                    color.a = FloatingDamage.Alpha(number.Age);
                    label.style.color = color;
                    label.style.fontSize = BaseFontSize + (BigHitFontBonus * Mathf.Clamp01(number.Amount / BigHitAmount));
                    label.style.left = point.x + number.Side;
                    label.style.top = point.y;
                    label.style.display = DisplayStyle.Flex;
                    shown++;
                }
            }

            for (int i = shown; i < PoolSize; i++)
            {
                _labels[i].style.display = DisplayStyle.None;
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _blockedSubscription?.Dispose();
            _blockedSubscription = null;
        }

        private void OnDamage(DamageApplied hit)
        {
            if (_state == null || !_state.TryGetDemon(hit.Target, out Demon? target))
            {
                return;
            }

            _numbers.Add(hit, _player, Base(target), target.SizeMeters);
        }

        // In test mode the player takes nothing, but still sees what it would have taken (D-089, D-092).
        private void OnBlocked(DamageBlocked hit)
        {
            if (_state == null || !_state.TryGetDemon(hit.Target, out Demon? target))
            {
                return;
            }

            _numbers.Add(hit, _player, Base(target), target.SizeMeters);
        }

        // Over the target while it is there; where it last stood once it is gone.
        private Vector3 FeetOf(FloatingDamage.Number number)
        {
            return _state != null && _state.TryGetDemon(number.Target, out Demon? target) ? Base(target) : number.Anchor;
        }

        // The underside of the core: the feet, raised by the legs it stands on.
        private Vector3 Base(Demon demon)
        {
            float stance = _stanceOf != null ? _stanceOf(demon.Id) : 0f;
            return new Vector3(demon.Position.X, demon.Position.Y + stance, demon.Position.Z);
        }
    }
}
