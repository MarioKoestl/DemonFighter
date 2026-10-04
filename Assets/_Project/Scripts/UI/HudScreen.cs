#nullable enable
using System;
using DemonFighter.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The in-run HUD (GAME_DESIGN, "UI"): Health, Stamina, Biomass, Level and Tier top left, the seed in a corner.
    /// Health, Stamina, Biomass and Level are fixed placeholders until M2 models them; Tier and Seed are real.
    /// Refreshes once per simulation tick and only rewrites a label when its value changed, so it allocates nothing
    /// while idle. Styling in code is a placeholder until the M5 UI work moves it into USS.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudScreen : MonoBehaviour
    {
        private const int PlaceholderHealth = 100;
        private const int PlaceholderStamina = 100;
        private const int PlaceholderBiomass = 0;
        private const int PlaceholderLevel = 1;

        private UIDocument _document = null!;
        private Label _health = null!;
        private Label _stamina = null!;
        private Label _biomass = null!;
        private Label _level = null!;
        private Label _tier = null!;
        private Label _seed = null!;
        private RunState? _state;
        private Demon? _player;
        private long _lastTick = -1;
        private int _lastTier = -1;

        /// <summary>Shows the run and follows the player's demon from now on.</summary>
        public void Bind(RunState state, Demon player)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _lastTick = -1;
            _lastTier = -1;
            _health.text = "Health " + PlaceholderHealth;
            _stamina.text = "Stamina " + PlaceholderStamina;
            _biomass.text = "Biomass " + PlaceholderBiomass;
            _level.text = "Level " + PlaceholderLevel;
            _seed.text = "Seed " + state.Seed;
            Refresh();
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.Add(BuildLayout());
        }

        private void Update()
        {
            if (_state == null || _player == null || _state.Tick == _lastTick)
            {
                return;
            }

            _lastTick = _state.Tick;
            Refresh();
        }

        private void Refresh()
        {
            if (_player == null)
            {
                return;
            }

            if (_player.Tier != _lastTier)
            {
                _lastTier = _player.Tier;
                _tier.text = "Tier " + _lastTier;
            }
        }

        private VisualElement BuildLayout()
        {
            var overlay = new VisualElement { name = "hud", pickingMode = PickingMode.Ignore };
            overlay.style.flexGrow = 1f;

            var stats = new VisualElement { name = "stats", pickingMode = PickingMode.Ignore };
            stats.style.position = Position.Absolute;
            stats.style.left = 16;
            stats.style.top = 16;
            stats.style.paddingLeft = 12;
            stats.style.paddingRight = 12;
            stats.style.paddingTop = 8;
            stats.style.paddingBottom = 8;
            stats.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);

            _health = StatLabel("health", stats);
            _stamina = StatLabel("stamina", stats);
            _biomass = StatLabel("biomass", stats);
            _level = StatLabel("level", stats);
            _tier = StatLabel("tier", stats);

            _seed = new Label { name = "seed", pickingMode = PickingMode.Ignore };
            _seed.style.position = Position.Absolute;
            _seed.style.right = 16;
            _seed.style.bottom = 12;
            _seed.style.fontSize = 16;
            _seed.style.color = new Color(0.75f, 0.7f, 0.65f);

            overlay.Add(stats);
            overlay.Add(_seed);
            return overlay;
        }

        private static Label StatLabel(string name, VisualElement parent)
        {
            var label = new Label { name = name, pickingMode = PickingMode.Ignore };
            label.style.fontSize = 20;
            label.style.color = Color.white;
            label.style.marginBottom = 2;
            parent.Add(label);
            return label;
        }
    }
}
