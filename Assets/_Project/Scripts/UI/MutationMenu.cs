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
    /// The mutation menu (GAME_DESIGN, "Mutation"): one screen with the tabs Mutate, Evolve, Stats and Skills. Mutate
    /// is a planner like Stats (D-064): offers in three groups (new parts, upgrades, regrowing lost parts) with cost,
    /// effect and the reason one is unavailable are selected, a turning 3D preview shows the selection on the own body
    /// (D-063), the footer sums the cost against the Biomass, and one Apply sends every selected mutation. Evolve
    /// shows the pending options as cards; Stats hosts the point planner; Skills lists levels, XP and perks. The menu
    /// only raises requests, the simulation applies them, and it rebuilds a list only when its content changed.
    /// Built in code until M5 moves styling into USS.
    /// </summary>
    public sealed class MutationMenu
    {
        private const int PreviewWidth = 300;
        private const int PreviewHeight = 375;
        private const float DragDegreesPerPixel = 0.6f;
        private const float CostTolerance = 0.001f;

        private static readonly Color Backdrop = new Color(0.03f, 0.01f, 0.01f, 0.92f);
        private static readonly Color PanelBackground = new Color(0.07f, 0.03f, 0.03f, 1f);
        private static readonly Color TitleColor = new Color(0.95f, 0.3f, 0.25f);
        private static readonly Color MutedText = new Color(0.75f, 0.7f, 0.65f);
        private static readonly Color WarningText = new Color(0.95f, 0.45f, 0.3f);
        private static readonly Color ValueColor = new Color(0.95f, 0.8f, 0.3f);
        private static readonly Color SelectedTab = new Color(0.62f, 0.12f, 0.12f);
        private static readonly Color PlainTab = new Color(0.22f, 0.16f, 0.16f);
        private static readonly Color TabBorder = new Color(0.5f, 0.3f, 0.3f);
        private static readonly Color RowLine = new Color(0.3f, 0.2f, 0.2f);
        private static readonly Color SelectedOffer = new Color(0.25f, 0.45f, 0.25f);
        private static readonly Color PreviewBackground = new Color(0.05f, 0.02f, 0.02f, 1f);
        private static readonly MutationKind[] ApplyOrder = { MutationKind.Attach, MutationKind.Upgrade, MutationKind.Regrow };

        private readonly RunState _state;
        private readonly Demon _player;
        private readonly IMutationOfferPolicy _offers;
        private readonly StatsPanel _stats;
        private readonly IBodyPreview? _preview;
        private readonly Dictionary<MenuTab, Button> _tabButtons = new Dictionary<MenuTab, Button>();
        private readonly Dictionary<MenuTab, VisualElement> _pages = new Dictionary<MenuTab, VisualElement>();
        private readonly Dictionary<string, MutationOffer> _planned = new Dictionary<string, MutationOffer>(StringComparer.Ordinal);
        private readonly List<string> _bodyLines = new List<string>();
        private readonly List<bool> _bodyFilled = new List<bool>();
        private readonly Label _biomass;
        private readonly VisualElement? _previewImage;
        private readonly VisualElement _bodyList;
        private readonly VisualElement _offerList;
        private readonly VisualElement _footer;
        private readonly Label _planSummary;
        private readonly Button _applyButton;
        private readonly Button _resetButton;
        private readonly VisualElement _evolveList;
        private readonly Label _evolveStatus;
        private readonly VisualElement _skillList;
        private int _biomassTenths = -1;
        private string _bodySignature = string.Empty;
        private string _offerSignature = string.Empty;
        private string _planSignature = string.Empty;
        private string _evolveSignature = string.Empty;
        private long _skillsTick = -1;
        private bool _rowsDirty;
        private bool _dragging;

        public MutationMenu(RunState state, Demon player, IMutationOfferPolicy offers, StatsPanel stats, IBodyPreview? preview)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _offers = offers ?? throw new ArgumentNullException(nameof(offers));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _preview = preview;

            Root = new VisualElement { name = "mutation-menu" };
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.top = 0;
            Root.style.bottom = 0;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = Backdrop;

            var panel = new VisualElement { name = "panel" };
            panel.style.width = 1040;
            panel.style.maxHeight = Length.Percent(92f);
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.paddingTop = 16;
            panel.style.paddingBottom = 16;
            panel.style.backgroundColor = PanelBackground;
            Root.Add(panel);

            var header = new VisualElement { name = "header" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 12;
            var title = new Label("MUTATION") { name = "title" };
            title.style.fontSize = 32;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TitleColor;
            title.style.marginRight = 32;
            header.Add(title);
            foreach (MenuTab tab in new[] { MenuTab.Mutate, MenuTab.Evolve, MenuTab.Stats, MenuTab.Skills })
            {
                MenuTab captured = tab;
                var button = new Button(() => Open(captured)) { name = "tab-" + tab, text = tab.ToString() };
                button.style.fontSize = 20;
                button.style.width = 140;
                button.style.height = 40;
                button.style.marginRight = 8;
                button.style.color = Color.white;
                button.style.backgroundColor = PlainTab;
                button.style.borderTopColor = TabBorder;
                button.style.borderBottomColor = TabBorder;
                button.style.borderLeftColor = TabBorder;
                button.style.borderRightColor = TabBorder;
                header.Add(button);
                _tabButtons[tab] = button;
            }

            _biomass = new Label { name = "biomass" };
            _biomass.style.fontSize = 20;
            _biomass.style.color = ValueColor;
            _biomass.style.flexGrow = 1f;
            _biomass.style.unityTextAlign = TextAnchor.MiddleRight;
            header.Add(_biomass);
            panel.Add(header);

            var content = new ScrollView(ScrollViewMode.Vertical) { name = "content" };
            content.style.flexGrow = 1f;
            content.style.flexShrink = 1f;
            panel.Add(content);

            VisualElement mutate = Page(MenuTab.Mutate, content);
            mutate.style.flexDirection = FlexDirection.Row;
            var bodyColumn = new VisualElement { name = "body-column" };
            bodyColumn.style.width = PreviewWidth;
            bodyColumn.style.flexShrink = 0f;
            bodyColumn.style.marginRight = 24;
            if (preview != null)
            {
                _previewImage = new VisualElement { name = "body-preview" };
                _previewImage.style.width = PreviewWidth;
                _previewImage.style.height = PreviewHeight;
                _previewImage.style.marginBottom = 6;
                _previewImage.style.backgroundColor = PreviewBackground;
                _previewImage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(preview.Texture));
                _previewImage.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
                _previewImage.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
                _previewImage.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
                _previewImage.RegisterCallback<PointerCaptureOutEvent>(OnPreviewCaptureOut);
                bodyColumn.Add(_previewImage);
                bodyColumn.Add(Line("Drag to turn. Selected mutations show on your body.", MutedText, 13));
            }

            Label bodyTitle = SectionTitle("Body");
            bodyTitle.style.marginTop = 8;
            bodyColumn.Add(bodyTitle);
            _bodyList = new VisualElement { name = "body-list" };
            bodyColumn.Add(_bodyList);
            mutate.Add(bodyColumn);
            var offerColumn = new VisualElement { name = "offer-column" };
            offerColumn.style.flexGrow = 1f;
            offerColumn.style.flexShrink = 1f;
            _offerList = new VisualElement { name = "offer-list" };
            offerColumn.Add(_offerList);
            mutate.Add(offerColumn);

            VisualElement evolve = Page(MenuTab.Evolve, content);
            _evolveStatus = new Label { name = "evolve-status" };
            _evolveStatus.style.fontSize = 20;
            _evolveStatus.style.color = Color.white;
            _evolveStatus.style.marginBottom = 12;
            _evolveStatus.style.whiteSpace = WhiteSpace.Normal;
            evolve.Add(_evolveStatus);
            _evolveList = new VisualElement { name = "evolve-list" };
            _evolveList.style.flexDirection = FlexDirection.Row;
            evolve.Add(_evolveList);

            VisualElement statsPage = Page(MenuTab.Stats, content);
            statsPage.Add(_stats.Root);

            VisualElement skills = Page(MenuTab.Skills, content);
            _skillList = new VisualElement { name = "skill-list" };
            skills.Add(_skillList);

            // The footer of the Mutate tab stays visible however long the offer list scrolls.
            _footer = new VisualElement { name = "mutation-footer" };
            _footer.style.flexDirection = FlexDirection.Row;
            _footer.style.alignItems = Align.Center;
            _footer.style.marginTop = 12;
            _planSummary = new Label { name = "plan-summary" };
            _planSummary.style.fontSize = 18;
            _planSummary.style.color = Color.white;
            _planSummary.style.flexGrow = 1f;
            _planSummary.style.flexShrink = 1f;
            _planSummary.style.whiteSpace = WhiteSpace.Normal;
            _resetButton = new Button(ResetPlan) { name = "reset-mutations", text = "Reset" };
            _resetButton.style.fontSize = 20;
            _resetButton.style.width = 140;
            _resetButton.style.height = 44;
            _resetButton.style.marginRight = 12;
            _applyButton = new Button(ApplyPlan) { name = "apply-mutations", text = "Apply" };
            _applyButton.style.fontSize = 20;
            _applyButton.style.width = 240;
            _applyButton.style.height = 44;
            _footer.Add(_planSummary);
            _footer.Add(_resetButton);
            _footer.Add(_applyButton);
            panel.Add(_footer);

            var hint = new Label("Tab or C closes the menu. Select mutations, then Apply: the body reshapes once for two seconds, unable to act and immune to harm.") { name = "hint" };
            hint.style.fontSize = 15;
            hint.style.color = MutedText;
            hint.style.marginTop = 12;
            hint.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(hint);

            Root.style.display = DisplayStyle.None;
        }

        /// <summary>Raised when the player applies the selected mutations, every one as its own command, in order.</summary>
        public event Action<IReadOnlyList<MutateCommand>>? MutationsRequested;

        /// <summary>Raised when the player chooses an evolution.</summary>
        public event Action<string>? EvolutionRequested;

        /// <summary>The element to add to the HUD.</summary>
        public VisualElement Root { get; }

        /// <summary>True while the menu is shown.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>The tab shown now, or last shown.</summary>
        public MenuTab Tab { get; private set; } = MenuTab.Mutate;

        /// <summary>Opens the menu on a tab, or switches the tab while open; an unapplied selection is dropped.</summary>
        public void Open(MenuTab tab)
        {
            IsOpen = true;
            Tab = tab;
            Root.style.display = DisplayStyle.Flex;
            foreach (KeyValuePair<MenuTab, VisualElement> page in _pages)
            {
                page.Value.style.display = page.Key == tab ? DisplayStyle.Flex : DisplayStyle.None;
            }

            foreach (KeyValuePair<MenuTab, Button> button in _tabButtons)
            {
                bool selected = button.Key == tab;
                button.Value.style.backgroundColor = selected ? SelectedTab : PlainTab;
                button.Value.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            }

            _footer.style.display = tab == MenuTab.Mutate ? DisplayStyle.Flex : DisplayStyle.None;
            _stats.SetVisible(tab == MenuTab.Stats);
            _preview?.SetActive(tab == MenuTab.Mutate);
            _planned.Clear();
            _biomassTenths = -1;
            _bodySignature = string.Empty;
            _offerSignature = string.Empty;
            _planSignature = string.Empty;
            _evolveSignature = string.Empty;
            _skillsTick = -1;
            Refresh();
        }

        /// <summary>Hides the menu; unapplied selections and stat plans are dropped and the preview stops rendering.</summary>
        public void Close()
        {
            IsOpen = false;
            _dragging = false;
            _planned.Clear();
            Root.style.display = DisplayStyle.None;
            _stats.SetVisible(false);
            _preview?.SetActive(false);
        }

        /// <summary>Redraws what changed; called every frame while open.</summary>
        public void Refresh()
        {
            if (!IsOpen)
            {
                return;
            }

            int tenths = Mathf.RoundToInt(_player.Biomass * 10f);
            if (tenths != _biomassTenths)
            {
                _biomassTenths = tenths;
                _biomass.text = "Biomass " + (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture);
            }

            switch (Tab)
            {
                case MenuTab.Mutate:
                    RefreshBody();
                    RefreshOffers();
                    RefreshFooter();
                    break;
                case MenuTab.Evolve:
                    RefreshEvolve();
                    break;
                case MenuTab.Stats:
                    _stats.Refresh(_player);
                    break;
                case MenuTab.Skills:
                    RefreshSkills();
                    break;
            }
        }

        // The sockets with what fills them, under the live preview of the body.
        private void RefreshBody()
        {
            Body body = _player.Body;
            _bodyLines.Clear();
            _bodyFilled.Clear();
            _bodyLines.Add(body.Core.Spec.Name + "  " + Mathf.RoundToInt(body.Core.Hp) + " / " + Mathf.RoundToInt(body.Core.MaxHp) + " HP");
            _bodyFilled.Add(true);
            IReadOnlyList<SocketSlot> sockets = body.Core.Spec.Sockets;
            IReadOnlyList<BodyPart> parts = body.Parts;
            for (int s = 0; s < sockets.Count; s++)
            {
                SocketSlot socket = sockets[s];
                int used = 0;
                var names = new StringBuilder();
                for (int i = 0; i < parts.Count; i++)
                {
                    BodyPart part = parts[i];
                    if (part.Spec.Socket != socket.Kind)
                    {
                        continue;
                    }

                    used++;
                    if (names.Length > 0)
                    {
                        names.Append(", ");
                    }

                    names.Append(part.Spec.Name);
                    if (part.UpgradeLevel > 0)
                    {
                        names.Append(" +").Append(part.UpgradeLevel);
                    }

                    if (part.IsLost)
                    {
                        names.Append(" (lost)");
                    }
                }

                _bodyLines.Add(socket.Kind + " " + used + " / " + socket.Capacity + (names.Length > 0 ? ":  " + names : ":  empty"));
                _bodyFilled.Add(used > 0);
            }

            string signature = string.Join("\n", _bodyLines);
            if (signature == _bodySignature)
            {
                return;
            }

            _bodySignature = signature;
            _bodyList.Clear();
            for (int i = 0; i < _bodyLines.Count; i++)
            {
                _bodyList.Add(Line(_bodyLines[i], _bodyFilled[i] ? Color.white : MutedText, i == 0 ? 18 : 16));
            }
        }

        // Rows are rebuilt when the offers change (a purchase, a level) or the selection changes; a change of the
        // offers also drops the selection, because it refers to offers that may be gone.
        private void RefreshOffers()
        {
            IReadOnlyList<MutationOffer> offers = _offers.Offers(_state, _player);
            var signature = new StringBuilder();
            for (int i = 0; i < offers.Count; i++)
            {
                MutationOffer offer = offers[i];
                signature.Append(offer.Kind).Append(offer.Part.Id).Append(offer.PartIndex).Append(offer.Available ? 1 : 0).Append(offer.Reason).Append(offer.Cost.ToString(CultureInfo.InvariantCulture)).Append(';');
            }

            string current = signature.ToString();
            if (current != _offerSignature)
            {
                _offerSignature = current;
                _planned.Clear();
                _rowsDirty = false;
                RebuildPreview();
                RebuildOfferRows(offers);
                return;
            }

            if (_rowsDirty)
            {
                _rowsDirty = false;
                RebuildOfferRows(offers);
            }
        }

        private void RefreshFooter()
        {
            int count = _planned.Count;
            string summary = count == 0
                ? "Nothing selected. Select mutations, then Apply."
                : "Selected " + count + (count == 1 ? " mutation, " : " mutations, ") + PlannedCost().ToString("0", CultureInfo.InvariantCulture) + " of " + _player.Biomass.ToString("0", CultureInfo.InvariantCulture) + " Biomass";
            if (summary == _planSignature)
            {
                return;
            }

            _planSignature = summary;
            _planSummary.text = summary;
            _applyButton.text = count == 0 ? "Apply" : "Apply " + count + (count == 1 ? " mutation" : " mutations");
            _applyButton.SetEnabled(count > 0);
            _resetButton.SetEnabled(count > 0);
        }

        private void RebuildOfferRows(IReadOnlyList<MutationOffer> offers)
        {
            _offerList.Clear();
            if (offers.Count == 0)
            {
                _offerList.Add(Line("Nothing to mutate.", MutedText, 16));
                return;
            }

            AddSection("New parts", offers, MutationKind.Attach);
            AddSection("Upgrades", offers, MutationKind.Upgrade);
            AddSection("Regrow lost parts", offers, MutationKind.Regrow);
        }

        private void AddSection(string title, IReadOnlyList<MutationOffer> offers, MutationKind kind)
        {
            VisualElement? section = null;
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i].Kind != kind)
                {
                    continue;
                }

                if (section == null)
                {
                    section = new VisualElement { name = "section-" + kind };
                    section.style.marginBottom = 14;
                    section.Add(SectionTitle(title));
                }

                section.Add(OfferRow(offers[i]));
            }

            if (section != null)
            {
                _offerList.Add(section);
            }
        }

        private VisualElement OfferRow(MutationOffer offer)
        {
            var row = new VisualElement { name = "offer-" + offer.Kind + "-" + offer.Part.Id + "-" + offer.PartIndex };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8;
            row.style.paddingBottom = 6;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = RowLine;

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.flexShrink = 1f;
            string kind = offer.Kind == MutationKind.Attach
                ? "New"
                : offer.Kind == MutationKind.Upgrade ? "Upgrade to +" + (_player.Body.GetPart(offer.PartIndex).UpgradeLevel + 1) : "Regrow";
            text.Add(Line(offer.Part.Name + "   " + kind, Color.white, 20));
            text.Add(Line(Describe(offer.Part), MutedText, 14));
            if (!offer.Available)
            {
                text.Add(Line(offer.Reason, WarningText, 14));
            }

            var cost = new Label(offer.Cost.ToString("0", CultureInfo.InvariantCulture) + " Biomass");
            cost.style.fontSize = 18;
            cost.style.color = ValueColor;
            cost.style.width = 120;
            cost.style.unityTextAlign = TextAnchor.MiddleRight;
            cost.style.marginRight = 12;
            row.Add(text);
            row.Add(cost);

            bool selected = _planned.ContainsKey(Key(offer));
            MutationOffer captured = offer;
            var select = new Button(() => ToggleSelection(captured)) { name = "select", text = selected ? "Selected" : "Select" };
            select.style.fontSize = 18;
            select.style.width = 120;
            select.style.height = 40;
            if (selected)
            {
                select.style.backgroundColor = SelectedOffer;
                select.style.color = Color.white;
            }

            select.SetEnabled(selected || CanAdd(offer));
            row.Add(select);
            return row;
        }

        private void ToggleSelection(MutationOffer offer)
        {
            string key = Key(offer);
            if (!_planned.Remove(key))
            {
                if (!CanAdd(offer))
                {
                    return;
                }

                _planned[key] = offer;
            }

            _rowsDirty = true;
            RebuildPreview();
        }

        // What the Biomass and the free sockets can carry on top of what is already selected; the simulation
        // validates every command again when it applies them.
        private bool CanAdd(MutationOffer offer)
        {
            if (!offer.Available)
            {
                return false;
            }

            if (PlannedCost() + offer.Cost > _player.Biomass + CostTolerance)
            {
                return false;
            }

            return offer.Kind != MutationKind.Attach || PlannedAttaches(offer.Part.Socket) < _player.Body.FreeSlots(offer.Part.Socket);
        }

        private float PlannedCost()
        {
            float total = 0f;
            foreach (MutationOffer offer in _planned.Values)
            {
                total += offer.Cost;
            }

            return total;
        }

        private int PlannedAttaches(SocketKind socket)
        {
            int count = 0;
            foreach (MutationOffer offer in _planned.Values)
            {
                if (offer.Kind == MutationKind.Attach && offer.Part.Socket == socket)
                {
                    count++;
                }
            }

            return count;
        }

        // One command per selected offer, new parts first, in the order the shop lists them.
        private void ApplyPlan()
        {
            if (_planned.Count == 0)
            {
                return;
            }

            IReadOnlyList<MutationOffer> offers = _offers.Offers(_state, _player);
            var commands = new List<MutateCommand>();
            for (int k = 0; k < ApplyOrder.Length; k++)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i].Kind == ApplyOrder[k] && _planned.ContainsKey(Key(offers[i])))
                    {
                        commands.Add(offers[i].ToCommand(_player.Id));
                    }
                }
            }

            _planned.Clear();
            _rowsDirty = true;
            if (commands.Count > 0)
            {
                MutationsRequested?.Invoke(commands);
            }
        }

        private void ResetPlan()
        {
            _planned.Clear();
            _rowsDirty = true;
            RebuildPreview();
        }

        // The preview body: the own attached parts, lost parts whose regrow is selected, and the selected new parts.
        private void RebuildPreview()
        {
            if (_preview == null)
            {
                return;
            }

            CombatTuning tuning = _state.Catalog.Tuning;
            Body current = _player.Body;
            var body = new Body(current.Core.Spec, 1f, BodyRules.From(tuning));
            IReadOnlyList<BodyPart> parts = current.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                if (part.Spec.IsCore)
                {
                    continue;
                }

                bool shown = !part.IsLost || _planned.ContainsKey(Key(MutationKind.Regrow, part.Spec.Id, part.Index));
                if (shown && body.CanAttach(part.Spec, out _))
                {
                    body.Attach(part.Spec);
                }
            }

            int added = 0;
            foreach (MutationOffer offer in _planned.Values)
            {
                if (offer.Kind == MutationKind.Attach && body.CanAttach(offer.Part, out _))
                {
                    body.Attach(offer.Part);
                    added++;
                }
            }

            int tier = Demon.TierFor(_player.Spec, _player.Evolutions, current.InvestmentPoints + added, tuning);
            _preview.Show(body, Demon.SizeFor(_player.Spec, tier, tuning));
        }

        private void RefreshEvolve()
        {
            CombatTuning tuning = _state.Catalog.Tuning;
            int stage = EvolutionRules.PendingStage(_player, tuning);
            IReadOnlyList<EvolutionSpec> options = EvolutionRules.Options(_state, _player);
            var signature = new StringBuilder().Append(stage).Append(';').Append(_player.Level).Append(';');
            for (int i = 0; i < options.Count; i++)
            {
                bool can = EvolutionRules.CanEvolve(_player, options[i], _state, out string reason);
                signature.Append(options[i].Id).Append(can ? 1 : 0).Append(reason).Append(';');
            }

            string current = signature.ToString();
            if (current == _evolveSignature)
            {
                return;
            }

            _evolveSignature = current;
            _evolveList.Clear();
            if (stage == 0)
            {
                int next = EvolutionRules.NextThreshold(_player, tuning);
                _evolveStatus.text = next > 0
                    ? "Next evolution at level " + next + "; you are level " + _player.Level + "."
                    : "No further evolutions in this version.";
                return;
            }

            _evolveStatus.text = "Evolution " + stage + " is ready. Choose a line:";
            for (int i = 0; i < options.Count; i++)
            {
                _evolveList.Add(EvolutionCard(options[i]));
            }
        }

        private VisualElement EvolutionCard(EvolutionSpec spec)
        {
            var card = new VisualElement { name = "evolution-" + spec.Id };
            card.style.width = 310;
            card.style.marginRight = 16;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.backgroundColor = PlainTab;

            var name = new Label(spec.Name);
            name.style.fontSize = 24;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.color = ValueColor;
            card.Add(name);
            card.Add(Line(spec.Description, Color.white, 15));
            card.Add(Line(DescribePackage(spec), MutedText, 14));

            bool can = EvolutionRules.CanEvolve(_player, spec, _state, out string reason);
            if (!can)
            {
                card.Add(Line(reason, WarningText, 14));
            }

            string id = spec.Id;
            var choose = new Button(() => EvolutionRequested?.Invoke(id)) { name = "choose", text = "Choose " + spec.Name };
            choose.style.fontSize = 18;
            choose.style.height = 40;
            choose.style.marginTop = 8;
            choose.SetEnabled(can);
            card.Add(choose);
            return card;
        }

        private void RefreshSkills()
        {
            if (_skillsTick == _state.Tick)
            {
                return;
            }

            _skillsTick = _state.Tick;
            _skillList.Clear();
            IReadOnlyList<SkillInstance> skills = _player.Skills;
            if (skills.Count == 0)
            {
                _skillList.Add(Line("No skills yet.", MutedText, 16));
                return;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                SkillInstance skill = skills[i];
                bool granted = skill.IsGrantedBy(_player.Body);
                var row = new VisualElement { name = "skill-" + skill.Spec.Id };
                row.style.marginBottom = 10;
                string progress = skill.XpForNextLevel > 0f
                    ? "XP " + Mathf.RoundToInt(skill.Xp) + " / " + Mathf.RoundToInt(skill.XpForNextLevel)
                    : "max level";
                string header = skill.Spec.Name + "   Lv " + skill.Level + "   " + progress + (granted ? string.Empty : "   (part lost, level kept)");
                row.Add(Line(header, granted ? Color.white : MutedText, 20));
                SkillPerkSpec? perk = skill.Spec.Perk;
                if (perk != null)
                {
                    string perkLine = skill.HasPerk
                        ? "Perk active: " + perk.Name + ". " + perk.Description
                        : "Perk at level " + perk.Level + ": " + perk.Name + ". " + perk.Description;
                    row.Add(Line(perkLine, skill.HasPerk ? ValueColor : MutedText, 14));
                }

                _skillList.Add(row);
            }
        }

        private void OnPreviewPointerDown(PointerDownEvent evt)
        {
            if (_previewImage == null)
            {
                return;
            }

            _dragging = true;
            _previewImage.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            if (_dragging && _preview != null)
            {
                _preview.Rotate(evt.deltaPosition.x * DragDegreesPerPixel);
            }
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            _dragging = false;
            if (_previewImage != null && _previewImage.HasPointerCapture(evt.pointerId))
            {
                _previewImage.ReleasePointer(evt.pointerId);
            }
        }

        private void OnPreviewCaptureOut(PointerCaptureOutEvent evt)
        {
            _dragging = false;
        }

        // A short effect summary from the data, so the player can judge a part before buying it.
        private string Describe(BodyPartSpec part)
        {
            var text = new StringBuilder();
            Append(text, Spaced(part.Socket.ToString()) + " socket, " + Mathf.RoundToInt(part.MaxHp) + " HP");
            if (part.Defense != DefenseType.None)
            {
                Append(text, Spaced(part.Defense.ToString()) + " armor");
            }

            for (int i = 0; i < part.GrantedSkillIds.Count; i++)
            {
                Append(text, "grants " + SkillName(part.GrantedSkillIds[i]));
            }

            for (int i = 0; i < part.StatBonusesPerLevel.Count; i++)
            {
                StatValue bonus = part.StatBonusesPerLevel[i];
                Append(text, "+" + bonus.Value + " " + StatName(bonus.Stat) + " (+" + bonus.Value + " more per upgrade)");
            }

            for (int i = 0; i < part.SkillDamageBonusesPerLevel.Count; i++)
            {
                SkillBonus bonus = part.SkillDamageBonusesPerLevel[i];
                int percent = Mathf.RoundToInt(bonus.DamagePerLevel * 100f);
                Append(text, "+" + percent + "% " + SkillName(bonus.SkillId) + " damage (+" + percent + "% more per upgrade)");
            }

            if (part.MoveSpeedBonus != 0f)
            {
                Append(text, "+" + Mathf.RoundToInt(part.MoveSpeedBonus * 100f) + "% speed");
            }

            if (part.PerceptionBonus != 0f)
            {
                Append(text, "+" + Mathf.RoundToInt(part.PerceptionBonus * 100f) + "% perception and reach");
            }

            if (part.ReturnDamageFraction > 0f)
            {
                Append(text, "returns " + Mathf.RoundToInt(part.ReturnDamageFraction * 100f) + "% of melee hits as Pierce");
            }

            if (part.MinLevel > 1)
            {
                Append(text, "needs level " + part.MinLevel);
            }

            for (int i = 0; i < part.RequiredPartIds.Count; i++)
            {
                Append(text, "needs " + PartName(part.RequiredPartIds[i]));
            }

            if (part.RequiresUnlock)
            {
                Append(text, "unlocked by an evolution");
            }

            return text.ToString();
        }

        private string DescribePackage(EvolutionSpec spec)
        {
            var text = new StringBuilder();
            for (int i = 0; i < spec.StatBonuses.Count; i++)
            {
                Append(text, "+" + spec.StatBonuses[i].Value + " " + StatName(spec.StatBonuses[i].Stat));
            }

            if (spec.StatPoints > 0)
            {
                Append(text, spec.StatPoints + " free stat points");
            }

            for (int i = 0; i < spec.StatCapBonuses.Count; i++)
            {
                Append(text, StatName(spec.StatCapBonuses[i].Stat) + " cap +" + spec.StatCapBonuses[i].Value);
            }

            for (int i = 0; i < spec.UnlockedPartIds.Count; i++)
            {
                Append(text, "unlocks " + PartName(spec.UnlockedPartIds[i]));
            }

            for (int i = 0; i < spec.FreeMutationPartIds.Count; i++)
            {
                Append(text, "free " + PartName(spec.FreeMutationPartIds[i]));
            }

            for (int i = 0; i < spec.ExtraSkillIds.Count; i++)
            {
                Append(text, "skill " + SkillName(spec.ExtraSkillIds[i]));
            }

            Append(text, "one size step");
            return text.ToString();
        }

        private string PartName(string id)
        {
            return _state.Catalog.TryGetBodyPart(id, out BodyPartSpec? part) ? part.Name : id;
        }

        private string SkillName(string id)
        {
            return _state.Catalog.TryGetSkill(id, out SkillSpec? skill) ? skill.Name : id;
        }

        private string StatName(StatId id)
        {
            IReadOnlyList<StatSpec> stats = _state.Catalog.Tuning.Stats;
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i].Id == id)
                {
                    return stats[i].Name;
                }
            }

            return id.Value;
        }

        private static string Key(MutationOffer offer)
        {
            return Key(offer.Kind, offer.Part.Id, offer.PartIndex);
        }

        private static string Key(MutationKind kind, string partId, int partIndex)
        {
            return kind + ":" + partId + ":" + partIndex.ToString(CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder text, string piece)
        {
            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append(piece);
        }

        // "ThickHide" reads as "Thick Hide".
        private static string Spaced(string pascal)
        {
            var text = new StringBuilder();
            for (int i = 0; i < pascal.Length; i++)
            {
                if (i > 0 && char.IsUpper(pascal[i]))
                {
                    text.Append(' ');
                }

                text.Append(pascal[i]);
            }

            return text.ToString();
        }

        private VisualElement Page(MenuTab tab, VisualElement parent)
        {
            var page = new VisualElement { name = "page-" + tab };
            page.style.display = DisplayStyle.None;
            parent.Add(page);
            _pages[tab] = page;
            return page;
        }

        private static Label SectionTitle(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 22;
            label.style.color = TitleColor;
            label.style.marginBottom = 8;
            return label;
        }

        private static Label Line(string text, Color color, int fontSize)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 2;
            return label;
        }
    }
}
