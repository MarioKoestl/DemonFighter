#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DemonFighter.Common;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Evolution;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The in-run HUD (GAME_DESIGN, "UI"): health with a line per part, stamina, Biomass, level with XP, tier, the
    /// skills the body grants with their keys and cooldowns, a bleeding warning, a transforming or held notice, the
    /// stat points and evolutions waiting, plus a crosshair that names the aimed part, the analysis panel of a locked
    /// target (what the senses reveal, D-066), the eat prompt and the seed. Hosts the mutation menu (Tab, C) and the death
    /// screen. Refreshes once per simulation tick, every frame while the menu is open, and only rewrites a label when
    /// its value changed. Styling in code is a placeholder until the M5 UI work moves it into USS.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudScreen : MonoBehaviour
    {
        private const int CrosshairSize = 6;
        private const int PromptNone = 0;
        private const int PromptEat = 1;
        private const int PromptEating = 2;
        private const int StatusNone = 0;
        private const int StatusTransforming = 1;
        private const int StatusHeld = 2;

        private static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color MutedText = new Color(0.75f, 0.7f, 0.65f);
        private static readonly Color WarningText = new Color(0.95f, 0.15f, 0.1f);
        private static readonly Color HighlightText = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color CrosshairColor = new Color(1f, 1f, 1f, 0.85f);

        private readonly List<Label> _partLabels = new List<Label>();
        private readonly List<PartCache> _partCache = new List<PartCache>();
        private readonly List<Label> _skillLabels = new List<Label>();
        private readonly List<string> _skillTexts = new List<string>();

        private UIDocument _document = null!;
        private VisualElement _overlay = null!;
        private Label _health = null!;
        private VisualElement _partList = null!;
        private Label _stamina = null!;
        private Label _biomass = null!;
        private Label _level = null!;
        private Label _tier = null!;
        private VisualElement _skillList = null!;
        private Label _bleeding = null!;
        private Label _status = null!;
        private Label _points = null!;
        private Label _evolution = null!;
        private Label _prompt = null!;
        private Label _seed = null!;
        private Label _focus = null!;
        private VisualElement _target = null!;
        private Label _targetTitle = null!;
        private Label _targetBody = null!;
        private RunSummaryPanel? _summary;
        private StatsPanel? _stats;
        private MutationMenu? _menu;
        private RunState? _state;
        private Demon? _player;
        private Func<FoodId>? _aimedFood;
        private Func<TargetFocus>? _targetFocus;
        private string _focusText = string.Empty;
        private string _targetText = string.Empty;
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
        private int _bleedingShown;
        private int _statusState;
        private int _pointsValue;
        private int _evolutionShown;
        private int _promptState;

        /// <summary>Raised when the player clicks a plus button in the Stats tab, once per point on Apply.</summary>
        public event Action<StatId>? StatPointRequested;

        /// <summary>Raised when the player applies the selected mutations, every one as its own command, in order.</summary>
        public event Action<IReadOnlyList<MutateCommand>>? MutationsRequested;

        /// <summary>Raised when the player chooses an evolution.</summary>
        public event Action<string>? EvolutionRequested;

        /// <summary>Raised when the player clicks Back to Menu on the death screen.</summary>
        public event Action? BackToMenuRequested;

        /// <summary>True while the mutation menu is open.</summary>
        public bool IsMenuOpen => _menu != null && _menu.IsOpen;

        /// <summary>Shows the run and follows the demon of the player from now on; the aim callback feeds the eat prompt, the preview the menu.</summary>
        public void Bind(RunState state, Demon player, Func<FoodId> aimedFood, IMutationOfferPolicy offers, IBodyPreview? preview = null, Func<TargetFocus>? targetFocus = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _aimedFood = aimedFood ?? throw new ArgumentNullException(nameof(aimedFood));
            _targetFocus = targetFocus;
            if (offers == null)
            {
                throw new ArgumentNullException(nameof(offers));
            }

            ResetCaches();
            _summary?.SetVisible(false);
            _seed.text = "Seed " + state.Seed;

            if (_menu != null)
            {
                _menu.MutationsRequested -= OnMutationsRequested;
                _menu.EvolutionRequested -= OnEvolutionRequested;
                _overlay.Remove(_menu.Root);
            }

            if (_stats != null)
            {
                _stats.SpendRequested -= OnSpendRequested;
            }

            _stats = new StatsPanel(state.Catalog.Tuning);
            _stats.SpendRequested += OnSpendRequested;
            _menu = new MutationMenu(state, player, offers, _stats, preview);
            _menu.MutationsRequested += OnMutationsRequested;
            _menu.EvolutionRequested += OnEvolutionRequested;
            _overlay.Add(_menu.Root);
            if (_summary != null)
            {
                _summary.Root.BringToFront();
            }

            Refresh();
        }

        /// <summary>Opens the menu on the tab, or closes it when it is open; returns true when it is open afterwards.</summary>
        public bool ToggleMenu(MenuTab tab)
        {
            if (_menu == null)
            {
                return false;
            }

            if (_menu.IsOpen)
            {
                _menu.Close();
            }
            else
            {
                _menu.Open(tab);
            }

            return _menu.IsOpen;
        }

        /// <summary>Closes the menu if it is open.</summary>
        public void CloseMenu()
        {
            _menu?.Close();
        }

        /// <summary>Shows the death screen for the run; the menu closes if it was open.</summary>
        public void ShowRunSummary(RunState state, Demon player)
        {
            _menu?.Close();
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
            if (_menu != null)
            {
                _overlay.Add(_menu.Root);
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

            if (_menu != null && _menu.IsOpen)
            {
                _menu.Refresh();
            }

            RefreshFocus();
            if (_state.Tick == _lastTick)
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
                _tier.text = "Tier " + _tierValue + "   " + _player.SizeMeters.ToString("0.0", CultureInfo.InvariantCulture) + " m";
            }

            RefreshSkills();

            int statusState = _player.IsTransforming(_state.Tick) ? StatusTransforming : _player.IsHeld(_state.Tick) ? StatusHeld : StatusNone;
            if (Changed(ref _statusState, statusState))
            {
                _status.text = statusState == StatusTransforming ? "TRANSFORMING" : "HELD";
                _status.style.display = statusState == StatusNone ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (Changed(ref _bleedingShown, IsBleeding(body) ? 1 : 0))
            {
                _bleeding.style.display = _bleedingShown == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (Changed(ref _pointsValue, _player.Stats.UnspentPoints))
            {
                _points.text = _pointsValue == 1 ? "C: 1 stat point to spend" : "C: " + _pointsValue + " stat points to spend";
                _points.style.display = _pointsValue > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (Changed(ref _evolutionShown, EvolutionRules.PendingStage(_player, tuning) > 0 ? 1 : 0))
            {
                _evolution.style.display = _evolutionShown == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            int promptState = _player.IsEating ? PromptEating : _aimedFood != null && _aimedFood().IsValid ? PromptEat : PromptNone;
            if (Changed(ref _promptState, promptState))
            {
                _prompt.text = promptState == PromptEating ? "Eating" : "Hold E to eat";
                _prompt.style.display = promptState == PromptNone ? DisplayStyle.None : DisplayStyle.Flex;
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
                if (!cache.Update(hp, maxHp, part.Condition, part.IsBleeding, part.UpgradeLevel))
                {
                    continue;
                }

                string state = part.Condition == PartCondition.Lost
                    ? part.Spec.Fate == PartFate.Severed ? " severed" : " destroyed"
                    : part.Condition == PartCondition.Wounded ? " wounded" : string.Empty;
                string upgrade = part.UpgradeLevel > 0 ? " +" + part.UpgradeLevel : string.Empty;
                string bleeding = part.IsBleeding ? ", bleeding" : string.Empty;
                _partLabels[i].text = part.Spec.Name + upgrade + " " + hp + " / " + maxHp + state + bleeding;
            }
        }

        // One line per granted skill with its key and cooldown; lost skills vanish with their part (GAME_DESIGN, "HUD").
        private void RefreshSkills()
        {
            IReadOnlyList<SkillInstance> skills = _player!.Skills;
            int shown = 0;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillInstance skill = skills[i];
                if (!skill.IsGrantedBy(_player.Body) || skill.Spec.InputSlot == SkillSlot.None)
                {
                    continue;
                }

                if (SkillSlots.Find(_player, skill.Spec.InputSlot) != skill)
                {
                    continue;
                }

                while (_skillLabels.Count <= shown)
                {
                    var label = new Label { name = "skill-" + _skillLabels.Count, pickingMode = PickingMode.Ignore };
                    label.style.fontSize = 16;
                    label.style.color = Color.white;
                    label.style.marginBottom = 2;
                    _skillList.Add(label);
                    _skillLabels.Add(label);
                    _skillTexts.Add(string.Empty);
                }

                string text = KeyName(skill.Spec.InputSlot) + " " + skill.Spec.Name + " Lv " + skill.Level + Cooldown(skill);
                if (_skillTexts[shown] != text)
                {
                    _skillTexts[shown] = text;
                    _skillLabels[shown].text = text;
                    _skillLabels[shown].style.display = DisplayStyle.Flex;
                }

                shown++;
            }

            for (int i = shown; i < _skillLabels.Count; i++)
            {
                if (_skillTexts[i].Length > 0)
                {
                    _skillTexts[i] = string.Empty;
                    _skillLabels[i].style.display = DisplayStyle.None;
                }
            }
        }

        private string Cooldown(SkillInstance skill)
        {
            if (skill.Spec.IsPassive || !skill.IsOnCooldown(_state!.Tick))
            {
                return string.Empty;
            }

            float seconds = (skill.CooldownUntilTick - _state.Tick) * _state.Config.TickSeconds;
            return "   " + seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
        }

        private static string KeyName(SkillSlot slot)
        {
            switch (slot)
            {
                case SkillSlot.Primary:
                    return "[LMB]";
                case SkillSlot.Secondary:
                    return "[RMB]";
                case SkillSlot.Lunge:
                    return "[Space]";
                case SkillSlot.TailSwing:
                    return "[Q]";
                case SkillSlot.Sprint:
                    return "[Shift]";
                default:
                    return string.Empty;
            }
        }

        private void ResetCaches()
        {
            _lastTick = -1;
            _hp = _maxHp = _staminaValue = _maxStamina = _biomassTenths = _levelValue = _xp = _xpNext = _tierValue = -1;
            _bleedingShown = _statusState = _pointsValue = _evolutionShown = _promptState = -1;
            _focusText = string.Empty;
            _targetText = string.Empty;
            for (int i = 0; i < _partCache.Count; i++)
            {
                _partCache[i].Reset();
            }

            for (int i = 0; i < _skillTexts.Count; i++)
            {
                _skillTexts[i] = string.Empty;
            }
        }

        // The aim line and the analysis panel follow the crosshair every frame; both only rewrite on a change.
        private void RefreshFocus()
        {
            TargetFocus focus = _targetFocus != null ? _targetFocus() : TargetFocus.None;
            string line = string.Empty;
            if (focus.Focused.IsValid && _state!.TryGetDemon(focus.Focused, out Demon? aimed) && aimed.IsAlive)
            {
                string part = aimed.Body.HasPart(focus.PartIndex) ? aimed.Body.GetPart(focus.PartIndex).Spec.Name : "body";
                line = aimed.Spec.Name + "   Tier " + aimed.Tier + "   " + part
                    + (focus.InReach ? string.Empty : "   (out of reach)")
                    + (focus.Locked == focus.Focused ? "   locked" : "   F: analyze");
            }

            if (line != _focusText)
            {
                _focusText = line;
                _focus.text = line;
                _focus.style.display = line.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }

            string title = string.Empty;
            string body = string.Empty;
            if (focus.Locked.IsValid && _state!.TryGetDemon(focus.Locked, out Demon? locked))
            {
                title = locked.Spec.Name + "   Tier " + locked.Tier + "   " + locked.SizeMeters.ToString("0.0", CultureInfo.InvariantCulture) + " m" + (locked.IsAlive ? string.Empty : "   dead");
                body = Analyze(locked, _player!.SenseLevel);
            }

            string panel = title + "\n" + body;
            if (panel != _targetText)
            {
                _targetText = panel;
                _targetTitle.text = title;
                _targetBody.text = body;
                _target.style.display = title.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        // What the senses of the player reveal (D-066): dull senses see the shape and visible wounds, Eyes the health
        // and skills, two pairs of Eyes the level, stats, evolutions and the worth as food.
        private string Analyze(Demon target, int senseLevel)
        {
            var text = new StringBuilder();
            IReadOnlyList<BodyPart> parts = target.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                text.Append(part.Spec.Name);
                if (part.UpgradeLevel > 0)
                {
                    text.Append(" +").Append(part.UpgradeLevel);
                }

                if (part.IsLost)
                {
                    text.Append(part.Spec.Fate == PartFate.Severed ? "   severed" : "   destroyed");
                }
                else if (senseLevel >= 1)
                {
                    text.Append("   ").Append(Mathf.RoundToInt(part.Hp)).Append(" / ").Append(Mathf.RoundToInt(part.MaxHp));
                    if (part.IsBleeding)
                    {
                        text.Append(", bleeding");
                    }
                }
                else if (part.Condition == PartCondition.Wounded)
                {
                    text.Append("   wounded");
                }

                text.Append('\n');
            }

            if (senseLevel >= 1)
            {
                text.Append("Health ").Append(Mathf.RoundToInt(target.Body.TotalHp)).Append(" / ").Append(Mathf.RoundToInt(target.Body.TotalMaxHp)).Append('\n');
                text.Append("Skills: ");
                bool any = false;
                IReadOnlyList<SkillInstance> skills = target.Skills;
                for (int i = 0; i < skills.Count; i++)
                {
                    if (!skills[i].IsGrantedBy(target.Body))
                    {
                        continue;
                    }

                    text.Append(any ? ", " : string.Empty).Append(skills[i].Spec.Name).Append(" Lv ").Append(skills[i].Level);
                    any = true;
                }

                text.Append(any ? string.Empty : "none").Append('\n');
            }

            if (senseLevel >= 2)
            {
                CombatTuning tuning = _state!.Catalog.Tuning;
                text.Append("Level ").Append(target.Level).Append(", ").Append(target.Evolutions).Append(target.Evolutions == 1 ? " evolution" : " evolutions").Append('\n');
                IReadOnlyList<StatSpec> stats = tuning.Stats;
                for (int i = 0; i < stats.Count; i++)
                {
                    text.Append(i == 0 ? string.Empty : ", ").Append(stats[i].Name).Append(' ').Append(target.EffectiveStats().Has(stats[i].Id) ? target.EffectiveStats().Get(stats[i].Id) : 0);
                }

                text.Append('\n');
                text.Append("Worth ").Append(Mathf.RoundToInt(tuning.CorpseBiomassPerTier * (target.Tier + 1))).Append(" Biomass as food");
            }
            else
            {
                text.Append(senseLevel == 0 ? "Your senses are dull. Eyes would show wounds, health and skills." : "Sharper senses would show its level, stats and worth.");
            }

            return text.ToString().TrimEnd();
        }

        private void OnBackToMenu()
        {
            BackToMenuRequested?.Invoke();
        }

        private void OnSpendRequested(StatId stat)
        {
            StatPointRequested?.Invoke(stat);
        }

        private void OnMutationsRequested(IReadOnlyList<MutateCommand> commands)
        {
            MutationsRequested?.Invoke(commands);
        }

        private void OnEvolutionRequested(string evolutionId)
        {
            EvolutionRequested?.Invoke(evolutionId);
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
            _skillList = new VisualElement { name = "skills", pickingMode = PickingMode.Ignore };
            _skillList.style.marginTop = 6;
            stats.Add(_skillList);
            _bleeding = StatLabel("bleeding", stats, WarningText);
            _bleeding.text = "BLEEDING";
            _bleeding.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bleeding.style.display = DisplayStyle.None;
            _status = StatLabel("status", stats, HighlightText);
            _status.style.unityFontStyleAndWeight = FontStyle.Bold;
            _status.style.display = DisplayStyle.None;
            _points = StatLabel("points", stats, HighlightText);
            _points.style.display = DisplayStyle.None;
            _evolution = StatLabel("evolution", stats, HighlightText);
            _evolution.text = "Tab: an evolution is ready";
            _evolution.style.display = DisplayStyle.None;

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
            _focus = new Label { name = "focus", pickingMode = PickingMode.Ignore };
            _focus.style.position = Position.Absolute;
            _focus.style.left = 0;
            _focus.style.right = 0;
            _focus.style.top = Length.Percent(50f);
            _focus.style.marginTop = 18;
            _focus.style.fontSize = 16;
            _focus.style.color = HighlightText;
            _focus.style.unityTextAlign = TextAnchor.MiddleCenter;
            _focus.style.display = DisplayStyle.None;

            _target = new VisualElement { name = "target", pickingMode = PickingMode.Ignore };
            _target.style.position = Position.Absolute;
            _target.style.right = 16;
            _target.style.top = 16;
            _target.style.width = 340;
            _target.style.paddingLeft = 12;
            _target.style.paddingRight = 12;
            _target.style.paddingTop = 8;
            _target.style.paddingBottom = 8;
            _target.style.backgroundColor = PanelBackground;
            _target.style.display = DisplayStyle.None;
            _targetTitle = new Label { name = "target-title", pickingMode = PickingMode.Ignore };
            _targetTitle.style.fontSize = 20;
            _targetTitle.style.color = HighlightText;
            _targetTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _targetTitle.style.marginBottom = 4;
            _targetBody = new Label { name = "target-body", pickingMode = PickingMode.Ignore };
            _targetBody.style.fontSize = 15;
            _targetBody.style.color = Color.white;
            _targetBody.style.whiteSpace = WhiteSpace.Normal;
            _target.Add(_targetTitle);
            _target.Add(_targetBody);

            overlay.Add(_seed);
            overlay.Add(_focus);
            overlay.Add(_target);
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
            private int _upgrade = -1;
            private PartCondition _condition;
            private bool _bleeding;
            private bool _set;

            public bool Update(int hp, int maxHp, PartCondition condition, bool bleeding, int upgrade)
            {
                if (_set && _hp == hp && _maxHp == maxHp && _condition == condition && _bleeding == bleeding && _upgrade == upgrade)
                {
                    return false;
                }

                _set = true;
                _hp = hp;
                _maxHp = maxHp;
                _condition = condition;
                _bleeding = bleeding;
                _upgrade = upgrade;
                return true;
            }

            public void Reset()
            {
                _set = false;
            }
        }
    }
}
