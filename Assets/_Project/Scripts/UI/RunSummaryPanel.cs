#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DemonFighter.Simulation;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Hazards;
using DemonFighter.Simulation.Progression;
using DemonFighter.Simulation.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemonFighter.UI
{
    /// <summary>
    /// The death screen (GAME_DESIGN, "The run": end of run; D-075): time survived, what killed you, kills, Biomass
    /// eaten, highest and final tier with the level, the evolutions taken, the skills with their levels and the final
    /// body, with one button back to the menu. It only raises the request; the App layer ends the run. Built in code
    /// like the other screens until M5 moves styling into USS.
    /// </summary>
    public sealed class RunSummaryPanel
    {
        private static readonly Color Background = new Color(0.03f, 0.01f, 0.01f, 0.94f);
        private static readonly Color TitleColor = new Color(0.8f, 0.1f, 0.1f);
        private static readonly Color MutedText = new Color(0.75f, 0.7f, 0.65f);

        private readonly Label _time;
        private readonly Label _cause;
        private readonly Label _kills;
        private readonly Label _biomass;
        private readonly Label _tier;
        private readonly Label _evolutions;
        private readonly Label _skills;
        private readonly VisualElement _body;

        public RunSummaryPanel()
        {
            Root = new VisualElement { name = "run-summary" };
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.top = 0;
            Root.style.bottom = 0;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = Background;

            var title = new Label("YOU DIED") { name = "title" };
            title.style.fontSize = 64;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TitleColor;
            title.style.marginBottom = 24;
            Root.Add(title);

            _time = SummaryLabel("time");
            _cause = SummaryLabel("cause");
            _kills = SummaryLabel("kills");
            _biomass = SummaryLabel("biomass");
            _tier = SummaryLabel("tier");
            _evolutions = SummaryLabel("evolutions");
            _skills = SummaryLabel("skills");
            _skills.style.whiteSpace = WhiteSpace.Normal;
            _skills.style.maxWidth = 760;
            _skills.style.unityTextAlign = TextAnchor.MiddleCenter;

            var bodyTitle = new Label("Final body") { name = "body-title" };
            bodyTitle.style.fontSize = 22;
            bodyTitle.style.color = MutedText;
            bodyTitle.style.marginTop = 12;
            Root.Add(bodyTitle);
            _body = new VisualElement { name = "body" };
            _body.style.alignItems = Align.Center;
            _body.style.marginBottom = 24;
            Root.Add(_body);

            var back = new Button { name = "back-to-menu", text = "Back to Menu" };
            back.style.fontSize = 28;
            back.style.width = 320;
            back.style.height = 72;
            back.clicked += () => BackToMenuRequested?.Invoke();
            Root.Add(back);
            SetVisible(false);
        }

        /// <summary>Raised when the player clicks Back to Menu.</summary>
        public event Action? BackToMenuRequested;

        /// <summary>The element to add to the HUD.</summary>
        public VisualElement Root { get; }

        /// <summary>True while the screen is shown.</summary>
        public bool IsVisible { get; private set; }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Fills the summary from the run, the dead demon of the player and the summary record, and shows it.</summary>
        public void Show(RunState state, Demon player, RunSummary summary)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            _time.text = "Survived " + FormatTime(summary.SecondsSurvived) + "   seed " + summary.Seed;
            _cause.text = Cause(summary);
            _kills.text = summary.Kills == 1 ? "1 kill" : summary.Kills + " kills";
            _biomass.text = "Biomass eaten " + summary.BiomassEaten.ToString("0.0", CultureInfo.InvariantCulture);
            _tier.text = "Highest tier " + summary.HighestTier + (summary.FinalTier != summary.HighestTier ? " (final " + summary.FinalTier + ")" : string.Empty) + ", level " + summary.Level;
            _evolutions.text = "Evolutions: " + EvolutionNames(state, summary.EvolutionIds);
            _skills.text = "Skills: " + SkillLevels(player);

            _body.Clear();
            IReadOnlyList<BodyPart> parts = player.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                string condition = part.Condition == PartCondition.Lost
                    ? part.Spec.Fate == PartFate.Severed ? "severed" : "destroyed"
                    : part.Condition == PartCondition.Wounded ? "wounded" : "intact";
                string upgrade = part.UpgradeLevel > 0 ? " +" + part.UpgradeLevel : string.Empty;
                var line = new Label(part.Spec.Name + upgrade + ": " + condition) { name = "part-" + i };
                line.style.fontSize = 18;
                line.style.color = Color.white;
                _body.Add(line);
            }

            SetVisible(true);
        }

        private static string EvolutionNames(RunState state, IReadOnlyList<string> ids)
        {
            if (ids.Count == 0)
            {
                return "none";
            }

            var text = new StringBuilder();
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }

                text.Append(state.Catalog.TryGetEvolution(ids[i], out EvolutionSpec? spec) ? spec.Name + " " + spec.Stage : ids[i]);
            }

            return text.ToString();
        }

        private static string SkillLevels(Demon player)
        {
            IReadOnlyList<SkillInstance> skills = player.Skills;
            if (skills.Count == 0)
            {
                return "none";
            }

            var text = new StringBuilder();
            for (int i = 0; i < skills.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }

                text.Append(skills[i].Spec.Name).Append(" Lv ").Append(skills[i].Level);
            }

            return text.ToString();
        }

        private Label SummaryLabel(string name)
        {
            var label = new Label { name = name };
            label.style.fontSize = 24;
            label.style.color = Color.white;
            label.style.marginBottom = 6;
            Root.Add(label);
            return label;
        }

        // A living killer is named first; otherwise the fire or the wounds did it (D-086).
        private static string Cause(RunSummary summary)
        {
            if (summary.KillerName.Length > 0)
            {
                return "Killed by a " + summary.KillerName;
            }

            switch (summary.DeathHazard)
            {
                case HazardKind.Lava:
                    return "Burned in lava";
                case HazardKind.Fissure:
                    return "Burned in a fissure";
                default:
                    return "Bled out";
            }
        }

        private static string FormatTime(float seconds)
        {
            int whole = Mathf.FloorToInt(seconds);
            int minutes = whole / 60;
            int rest = whole % 60;
            return minutes + ":" + rest.ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
