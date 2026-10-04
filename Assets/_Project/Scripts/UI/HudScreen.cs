#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The in-run HUD (GAME_DESIGN, "UI"): health with a line per part, stamina, Biomass, level with XP, tier, the
    /// primary skill with its level, a bleeding warning and the stat points waiting, plus a crosshair, the eat prompt
    /// and the seed. Hosts the Stats panel (C). Refreshes once per simulation tick, every frame while the panel is
    /// open, and only rewrites a label when its value changed, so it allocates nothing while idle. Styling in code
    /// is a placeholder until the M5 UI work moves it into USS.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudScreen : MonoBehaviour
    {
        private const int CrosshairSize = 6;
        private const int PromptNone = 0;
        private const int PromptEat = 1;
        private const int PromptEating = 2;

        private static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color MutedText = new Color(0.75f, 0.7f, 0.65f);
        private static readonly Color WarningText = new Color(0.95f, 0.15f, 0.1f);
        private static readonly Color HighlightText = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color CrosshairColor = new Color(1f, 1f, 1f, 0.85f);

        private readonly List<Label> _partLabels = new List<Label>();
        private readonly List<PartCache> _partCache = new List<PartCache>();

        private UIDocument _document = null!;
        private VisualElement _overlay = null!;
        private Label _health = null!;
        private VisualElement _partList = null!;
        private Label _stamina = null!;
        private Label _biomass = null!;
        private Label _level = null!;
        private Label _tier = null!;
        private Label _skill = null!;
        private Label _bleeding = null!;
        private Label _points = null!;
        private Label _prompt = null!;
        private Label _seed = null!;
        private RunSummaryPanel? _summary;
        private StatsPanel? _stats;
        private RunState? _state;
        private Demon? _player;
        private Func<FoodId>? _aimedFood;
        private long _lastTick = -1;
        private int _hp;
        private int _maxHp;
        private int _staminaValue;
        private int _maxStamina;
        private int _biomassTenths;
        private int _levelValue;
        private int _xp;
        private int _xpNext;
        private int _tierValue;
        private int _skillLevel;
        private int _skillXp;
        private int _skillXpNext;
        private int _skillCooling;
        private int _bleedingShown;
        private int _pointsValue;
        private int _promptState;

        /// <summary>Raised when the player clicks a plus button in the Stats panel.</summary>
        public event Action<StatId>? StatPointRequested;

        /// <summary>Raised when the player clicks Back to Menu on the death screen.</summary>
        public event Action? BackToMenuRequested;

        /// <summary>True while the Stats panel is open.</summary>
        public bool IsStatsPanelOpen => _stats != null && _stats.IsVisible;

        /// <summary>Shows the run and follows the demon of the player from now on; the aim callback feeds the eat prompt.</summary>
        public void Bind(RunState state, Demon player, Func<FoodId> aimedFood)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _aimedFood = aimedFood ?? throw new ArgumentNullException(nameof(aimedFood));
            ResetCaches();
            _summary?.SetVisible(false);
            _seed.text = "Seed " + state.Seed;

            if (_stats != null)
            {
                _stats.SpendRequested -= OnSpendRequested;
                _overlay.Remove(_stats.Root);
            }

            _stats = new StatsPanel(state.Catalog.Tuning);
            _stats.SpendRequested += OnSpendRequested;
            _overlay.Add(_stats.Root);
            Refresh();
        }

        /// <summary>Opens or closes the Stats panel; returns true when it is open afterwards.</summary>
        public bool ToggleStatsPanel()
        {
            if (_stats == null)
            {
                return false;
            }

            _stats.SetVisible(!_stats.IsVisible);
            if (_stats.IsVisible)
            {
                Refresh();
            }

            return _stats.IsVisible;
        }

        /// <summary>Shows the death screen for the run; the Stats panel closes if it was open.</summary>
        public void ShowRunSummary(RunState state, Demon player)
        {
            _stats?.SetVisible(false);
            _summary?.Show(state, player);
        }

        private void Awake()
        {
            // Built here and not in a field initializer: Unity forbids creating elements while it deserializes the scene.
            _document = GetComponent<UIDocument>();
            _summary = new RunSummaryPanel();
            _summary.BackToMenuRequested += OnBackToMenu;
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            _overlay = BuildLayout();
            root.Add(_overlay);
            if (_stats != null)
            {
                _overlay.Add(_stats.Root);
            }

            if (_summary != null)
            {
                _overlay.Add(_summary.Root);
            }
        }

        private void Update()
        {
            if (_state == null || _player == null)
            {
                return;
            }

            if (_state.Tick == _lastTick && !IsStatsPanelOpen)
            {
                return;
            }

            _lastTick = _state.Tick;
            Refresh();
        }

        private void Refresh()
        {
            if (_state == null || _player == null)
            {
                return;
            }

            CombatTuning tuning = _state.Catalog.Tuning;
            Body body = _player.Body;
            int hp = Mathf.RoundToInt(body.TotalHp);
            int maxHp = Mathf.RoundToInt(body.TotalMaxHp);
            if (Changed(ref _hp, hp) | Changed(ref _maxHp, maxHp))
            {
                _health.text = "Health " + hp + " / " + maxHp;
            }

            RefreshParts(body);

            int stamina = Mathf.RoundToInt(_player.Stamina);
            int maxStamina = Mathf.RoundToInt(_player.Derived.MaxStamina);
            if (Changed(ref _staminaValue, stamina) | Changed(ref _maxStamina, maxStamina))
            {
                _stamina.text = "Stamina " + stamina + " / " + maxStamina;
            }

            if (Changed(ref _biomassTenths, Mathf.RoundToInt(_player.Biomass * 10f)))
            {
                _biomass.text = "Biomass " + (_biomassTenths / 10f).ToString("0.0", CultureInfo.InvariantCulture);
            }

            int xpNext = Mathf.RoundToInt(tuning.LevelXpForNext(_player.Level));
            if (Changed(ref _levelValue, _player.Level) | Changed(ref _xp, Mathf.RoundToInt(_player.Xp)) | Changed(ref _xpNext, xpNext))
            {
                _level.text = "Level " + _levelValue + "   XP " + _xp + " / " + _xpNext;
            }

            if (Changed(ref _tierValue, _player.Tier))
            {
                _tier.text = "Tier " + _tierValue;
            }

            RefreshSkill();

            if (Changed(ref _bleedingShown, IsBleeding(body) ? 1 : 0))
            {
                _bleeding.style.display = _bleedingShown == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (Changed(ref _pointsValue, _player.Stats.UnspentPoints))
            {
                _points.text = _pointsValue == 1 ? "C: 1 stat point to spend" : "C: " + _pointsValue + " stat points to spend";
                _points.style.display = _pointsValue > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            int promptState = _player.IsEating ? PromptEating : _aimedFood != null && _aimedFood().IsValid ? PromptEat : PromptNone;
            if (Changed(ref _promptState, promptState))
            {
                _prompt.text = promptState == PromptEating ? "Eating" : "Hold E to eat";
                _prompt.style.display = promptState == PromptNone ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_stats != null && _stats.IsVisible)
            {
                _stats.Refresh(_player);
            }
        }

        private void RefreshParts(Body body)
        {
            IReadOnlyList<BodyPart> parts = body.Parts;
            while (_partLabels.Count < parts.Count)
            {
                var label = new Label { name = "part-" + _partLabels.Count, pickingMode = PickingMode.Ignore };
                label.style.fontSize = 16;
                label.style.color = MutedText;
                label.style.marginLeft = 12;
                label.style.marginBottom = 2;
                _partList.Add(label);
                _partLabels.Add(label);
                _partCache.Add(new PartCache());
            }

            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                PartCache cache = _partCache[i];
                int hp = Mathf.RoundToInt(part.Hp);
                int maxHp = Mathf.RoundToInt(part.MaxHp);
                if (!cache.Update(hp, maxHp, part.Condition, part.IsBleeding))
                {
                    continue;
                }

                string state = part.Condition == PartCondition.Lost
                    ? part.Spec.Fate == PartFate.Severed ? " severed" : " destroyed"
                    : part.Condition == PartCondition.Wounded ? " wounded" : string.Empty;
                string bleeding = part.IsBleeding ? ", bleeding" : string.Empty;
                _partLabels[i].text = part.Spec.Name + " " + hp + " / " + maxHp + state + bleeding;
            }
        }

        private void RefreshSkill()
        {
            SkillInstance? skill = PrimarySkill();
            if (skill == null)
            {
                _skill.text = string.Empty;
                return;
            }

            int xpNext = Mathf.RoundToInt(skill.Spec.LevelCurve.XpForNextLevel(skill.Level));
            int cooling = skill.IsOnCooldown(_state!.Tick) ? 1 : 0;
            if (Changed(ref _skillLevel, skill.Level) | Changed(ref _skillXp, Mathf.RoundToInt(skill.Xp)) | Changed(ref _skillXpNext, xpNext) | Changed(ref _skillCooling, cooling))
            {
                _skill.text = skill.Spec.Name + " Lv " + _skillLevel + "   XP " + _skillXp + " / " + _skillXpNext + (cooling == 1 ? "   (cooldown)" : string.Empty);
            }
        }

        private SkillInstance? PrimarySkill()
        {
            IReadOnlyList<SkillInstance> skills = _player!.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].IsGrantedBy(_player.Body))
                {
                    return skills[i];
                }
            }

            return null;
        }

        private void ResetCaches()
        {
            _lastTick = -1;
            _hp = _maxHp = _staminaValue = _maxStamina = _biomassTenths = _levelValue = _xp = _xpNext = _tierValue = -1;
            _skillLevel = _skillXp = _skillXpNext = _skillCooling = _bleedingShown = _pointsValue = _promptState = -1;
            for (int i = 0; i < _partCache.Count; i++)
            {
                _partCache[i].Reset();
            }
        }

        private void OnBackToMenu()
        {
            BackToMenuRequested?.Invoke();
        }

        private void OnSpendRequested(StatId stat)
        {
            StatPointRequested?.Invoke(stat);
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
            stats.style.backgroundColor = PanelBackground;

            _health = StatLabel("health", stats, Color.white);
            _partList = new VisualElement { name = "parts", pickingMode = PickingMode.Ignore };
            stats.Add(_partList);
            _stamina = StatLabel("stamina", stats, Color.white);
            _biomass = StatLabel("biomass", stats, Color.white);
            _level = StatLabel("level", stats, Color.white);
            _tier = StatLabel("tier", stats, Color.white);
            _skill = StatLabel("skill", stats, Color.white);
            _bleeding = StatLabel("bleeding", stats, WarningText);
            _bleeding.text = "BLEEDING";
            _bleeding.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bleeding.style.display = DisplayStyle.None;
            _points = StatLabel("points", stats, HighlightText);
            _points.style.display = DisplayStyle.None;

            var crosshair = new VisualElement { name = "crosshair", pickingMode = PickingMode.Ignore };
            crosshair.style.position = Position.Absolute;
            crosshair.style.left = Length.Percent(50f);
            crosshair.style.top = Length.Percent(50f);
            crosshair.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-50f));
            crosshair.style.width = CrosshairSize;
            crosshair.style.height = CrosshairSize;
            crosshair.style.backgroundColor = CrosshairColor;
            crosshair.style.borderTopLeftRadius = CrosshairSize;
            crosshair.style.borderTopRightRadius = CrosshairSize;
            crosshair.style.borderBottomLeftRadius = CrosshairSize;
            crosshair.style.borderBottomRightRadius = CrosshairSize;

            _prompt = new Label { name = "prompt", pickingMode = PickingMode.Ignore };
            _prompt.style.position = Position.Absolute;
            _prompt.style.left = 0;
            _prompt.style.right = 0;
            _prompt.style.bottom = 120;
            _prompt.style.fontSize = 22;
            _prompt.style.color = HighlightText;
            _prompt.style.unityTextAlign = TextAnchor.MiddleCenter;
            _prompt.style.display = DisplayStyle.None;

            _seed = new Label { name = "seed", pickingMode = PickingMode.Ignore };
            _seed.style.position = Position.Absolute;
            _seed.style.right = 16;
            _seed.style.bottom = 12;
            _seed.style.fontSize = 16;
            _seed.style.color = MutedText;

            overlay.Add(stats);
            overlay.Add(crosshair);
            overlay.Add(_prompt);
            overlay.Add(_seed);
            return overlay;
        }

        private static Label StatLabel(string name, VisualElement parent, Color color)
        {
            var label = new Label { name = name, pickingMode = PickingMode.Ignore };
            label.style.fontSize = 20;
            label.style.color = color;
            label.style.marginBottom = 2;
            parent.Add(label);
            return label;
        }

        private static bool Changed(ref int cache, int value)
        {
            if (cache == value)
            {
                return false;
            }

            cache = value;
            return true;
        }

        private static bool IsBleeding(Body body)
        {
            IReadOnlyList<BodyPart> parts = body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsLost && parts[i].IsBleeding)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class PartCache
        {
            private int _hp = -1;
            private int _maxHp = -1;
            private PartCondition _condition;
            private bool _bleeding;
            private bool _set;

            public bool Update(int hp, int maxHp, PartCondition condition, bool bleeding)
            {
                if (_set && _hp == hp && _maxHp == maxHp && _condition == condition && _bleeding == bleeding)
                {
                    return false;
                }

                _set = true;
                _hp = hp;
                _maxHp = maxHp;
                _condition = condition;
                _bleeding = bleeding;
                return true;
            }

            public void Reset()
            {
                _set = false;
            }
        }
    }
}
