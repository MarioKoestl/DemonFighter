#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Stats;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The Stats tab of the mutation menu (GAME_DESIGN, "Mutation"). Points are planned first with plus and minus per
    /// stat, a preview shows what the plan would change, and Apply commits every planned point at once; nothing is
    /// spent before Apply. Caps from evolutions bound every stat. The panel only raises the requests; the simulation
    /// applies them and the panel reads the result back from the demon. Built in code until M5 moves styling to USS.
    /// </summary>
    public sealed class StatsPanel
    {
        private static readonly Color TitleColor = new Color(0.8f, 0.1f, 0.1f);
        private static readonly Color ValueColor = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color PlanColor = new Color(0.4f, 0.9f, 0.4f);
        private static readonly Color MutedText = new Color(0.75f, 0.7f, 0.65f);

        private readonly CombatTuning _tuning;
        private readonly Label _points;
        private readonly List<StatRow> _rows = new List<StatRow>();
        private readonly Label[] _preview;
        private readonly Button _apply;
        private readonly Button _reset;
        private bool _visible;

        public StatsPanel(CombatTuning tuning)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            Root = new VisualElement { name = "stats-panel" };

            var title = new Label("STATS") { name = "title" };
            title.style.fontSize = 28;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TitleColor;
            title.style.marginBottom = 8;
            Root.Add(title);

            _points = new Label { name = "points" };
            _points.style.fontSize = 20;
            _points.style.color = Color.white;
            _points.style.marginBottom = 12;
            Root.Add(_points);

            IReadOnlyList<StatSpec> stats = tuning.Stats;
            for (int i = 0; i < stats.Count; i++)
            {
                Root.Add(BuildRow(stats[i]));
            }

            var previewTitle = new Label("With the planned points") { name = "preview-title" };
            previewTitle.style.fontSize = 18;
            previewTitle.style.color = MutedText;
            previewTitle.style.marginTop = 12;
            previewTitle.style.marginBottom = 4;
            Root.Add(previewTitle);

            _preview = new Label[PreviewLines];
            for (int i = 0; i < _preview.Length; i++)
            {
                var line = new Label { name = "preview-" + i };
                line.style.fontSize = 17;
                line.style.color = Color.white;
                line.style.marginBottom = 2;
                Root.Add(line);
                _preview[i] = line;
            }

            var buttons = new VisualElement { name = "buttons" };
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.style.marginTop = 16;
            _reset = new Button(Reset) { name = "reset", text = "Reset" };
            _reset.style.fontSize = 20;
            _reset.style.width = 140;
            _reset.style.height = 44;
            _reset.style.marginRight = 12;
            _apply = new Button(Apply) { name = "apply", text = "Apply" };
            _apply.style.fontSize = 20;
            _apply.style.width = 200;
            _apply.style.height = 44;
            buttons.Add(_reset);
            buttons.Add(_apply);
            Root.Add(buttons);
            SetVisible(false);
        }

        private const int PreviewLines = 7;

        /// <summary>Raised once per planned point when Apply is clicked.</summary>
        public event Action<StatId>? SpendRequested;

        /// <summary>The element to add to the menu.</summary>
        public VisualElement Root { get; }

        /// <summary>True while the panel is shown.</summary>
        public bool IsVisible => _visible;

        /// <summary>Points planned but not yet applied.</summary>
        public int PlannedPoints
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _rows.Count; i++)
                {
                    total += _rows[i].Planned;
                }

                return total;
            }
        }

        /// <summary>Shows or hides the panel; hiding drops an unapplied plan.</summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
            {
                ClearPlan();
            }
        }

        /// <summary>Reads the current values, caps and points from the demon and recomputes the preview.</summary>
        public void Refresh(Demon demon)
        {
            if (demon == null)
            {
                throw new ArgumentNullException(nameof(demon));
            }

            int unspent = demon.Stats.UnspentPoints;
            int planned = Math.Min(PlannedPoints, unspent);
            if (planned < PlannedPoints)
            {
                ClearPlan();
                planned = 0;
            }

            _points.text = unspent == 1 ? "1 point to spend" : unspent + " points to spend";
            if (planned > 0)
            {
                _points.text += ", " + planned + " planned";
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                StatRow row = _rows[i];
                int current = demon.Stats.Has(row.Id) ? demon.Stats.Get(row.Id) : 0;
                int cap = demon.StatCap(row.Id);
                row.Value.text = current.ToString(CultureInfo.InvariantCulture) + " / " + cap.ToString(CultureInfo.InvariantCulture);
                row.Plan.text = row.Planned > 0 ? "+" + row.Planned : string.Empty;
                row.Minus.SetEnabled(row.Planned > 0);
                row.Plus.SetEnabled(planned < unspent && current + row.Planned < cap);
            }

            _apply.text = planned > 0 ? "Apply " + planned + (planned == 1 ? " point" : " points") : "Apply";
            _apply.SetEnabled(planned > 0);
            _reset.SetEnabled(planned > 0);
            RefreshPreview(demon, planned > 0);
        }

        private void RefreshPreview(Demon demon, bool hasPlan)
        {
            DerivedStats current = demon.Derived;
            DerivedStats next = current;
            if (hasPlan)
            {
                BaseStats planned = demon.EffectiveStats();
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].Planned > 0 && planned.Has(_rows[i].Id))
                    {
                        planned = planned.WithAdded(_rows[i].Id, _rows[i].Planned);
                    }
                }

                next = DerivedStats.From(planned, _tuning);
            }

            float maxHp = demon.Body.TotalMaxHp;
            float nextHp = current.HpMultiplier > 0f ? maxHp * next.HpMultiplier / current.HpMultiplier : maxHp;
            _preview[0].text = Line("Health", maxHp, nextHp, "0", string.Empty, hasPlan);
            _preview[1].text = Line("Damage", current.DamageMultiplier, next.DamageMultiplier, "0.00", "x", hasPlan);
            _preview[2].text = Line("Move speed", current.MoveSpeedMultiplier, next.MoveSpeedMultiplier, "0.00", "x", hasPlan);
            _preview[3].text = Line("Attack speed", current.AttackSpeedMultiplier, next.AttackSpeedMultiplier, "0.00", "x", hasPlan);
            _preview[4].text = Line("Stamina", current.MaxStamina, next.MaxStamina, "0", string.Empty, hasPlan);
            _preview[5].text = Line("Regeneration per second", current.RegenPerSecond, next.RegenPerSecond, "0.00", string.Empty, hasPlan);
            _preview[6].text = Line("Bleed duration", current.BleedDurationFactor, next.BleedDurationFactor, "0.00", "x", hasPlan);
        }

        // "Health  100 -> 117  (+17%)" with a plan, "Health  100" without one.
        private static string Line(string label, float current, float next, string format, string prefix, bool hasPlan)
        {
            string now = prefix + current.ToString(format, CultureInfo.InvariantCulture);
            if (!hasPlan)
            {
                return label + "  " + now;
            }

            string then = prefix + next.ToString(format, CultureInfo.InvariantCulture);
            float percent = current != 0f ? (next / current - 1f) * 100f : 0f;
            string sign = percent >= 0f ? "+" : string.Empty;
            return label + "  " + now + " -> " + then + "  (" + sign + percent.ToString("0.#", CultureInfo.InvariantCulture) + "%)";
        }

        private void Apply()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                StatRow row = _rows[i];
                for (int p = 0; p < row.Planned; p++)
                {
                    SpendRequested?.Invoke(row.Id);
                }
            }

            ClearPlan();
        }

        private void Reset()
        {
            ClearPlan();
        }

        private void ClearPlan()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].Planned = 0;
            }
        }

        private VisualElement BuildRow(StatSpec stat)
        {
            var row = new VisualElement { name = "stat-" + stat.Id.Value };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 10;

            var name = new Label(stat.Name);
            name.style.width = 140;
            name.style.fontSize = 22;
            name.style.color = Color.white;

            var value = new Label("0 / 10");
            value.style.width = 80;
            value.style.fontSize = 20;
            value.style.color = ValueColor;
            value.style.unityTextAlign = TextAnchor.MiddleRight;

            var statRow = new StatRow(stat.Id, value);
            var minus = new Button(() => Change(statRow, -1)) { text = "-" };
            StyleSmallButton(minus);
            minus.style.marginLeft = 16;

            var plan = new Label(string.Empty);
            plan.style.width = 44;
            plan.style.fontSize = 20;
            plan.style.color = PlanColor;
            plan.style.unityTextAlign = TextAnchor.MiddleCenter;

            var plus = new Button(() => Change(statRow, 1)) { text = "+" };
            StyleSmallButton(plus);

            var description = new Label(stat.Description);
            description.style.fontSize = 14;
            description.style.color = MutedText;
            description.style.flexGrow = 1f;
            description.style.flexShrink = 1f;
            description.style.marginLeft = 16;
            description.style.whiteSpace = WhiteSpace.Normal;

            statRow.Minus = minus;
            statRow.Plan = plan;
            statRow.Plus = plus;
            row.Add(name);
            row.Add(value);
            row.Add(minus);
            row.Add(plan);
            row.Add(plus);
            row.Add(description);
            _rows.Add(statRow);
            return row;
        }

        // The buttons only change the plan; Refresh, called every frame while open, redraws and re-enables them.
        private void Change(StatRow row, int delta)
        {
            row.Planned = Math.Max(0, row.Planned + delta);
        }

        private static void StyleSmallButton(Button button)
        {
            button.style.width = 40;
            button.style.height = 36;
            button.style.fontSize = 22;
        }

        private sealed class StatRow
        {
            public StatRow(StatId id, Label value)
            {
                Id = id;
                Value = value;
            }

            public StatId Id { get; }

            public Label Value { get; }

            public Button Minus { get; set; } = null!;

            public Label Plan { get; set; } = null!;

            public Button Plus { get; set; } = null!;

            public int Planned { get; set; }
        }
    }
}
