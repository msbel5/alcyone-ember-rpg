using System;
using EmberCrpg.Presentation.Ember.UI;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class LevelUpView
    {
        private readonly VisualElement _overlay;
        private readonly LevelUpScreenState _state;
        private readonly Func<LevelUpSelection, LevelUpActionResult> _onConfirm;
        private readonly Label _remainingLabel;
        private readonly Label _statusLabel;
        private readonly Label[] _statValueLabels;
        private readonly Button[] _minusButtons;
        private readonly Button[] _plusButtons;
        private readonly VisualElement[] _spellCards;
        private readonly int[] _adjustments;
        private readonly Button _confirmButton;
        private int _remainingPoints;
        private string _selectedSpellId;

        public LevelUpView(VisualElement stageCanvas, Action onClose, LevelUpScreenState state, Func<LevelUpSelection, LevelUpActionResult> onConfirm)
        {
            _state = state ?? new LevelUpScreenState("Unknown", 1, 5, Array.Empty<LevelUpStatRow>(), Array.Empty<LevelUpSpellRow>());
            _onConfirm = onConfirm;
            _remainingPoints = _state.PointsAvailable;
            _selectedSpellId = _state.SpellChoices.Count > 0 ? _state.SpellChoices[0].TemplateId : string.Empty;
            _adjustments = new int[_state.Stats.Count];
            _statValueLabels = new Label[_state.Stats.Count];
            _minusButtons = new Button[_state.Stats.Count];
            _plusButtons = new Button[_state.Stats.Count];
            _spellCards = new VisualElement[_state.SpellChoices.Count];

            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(4, 3, 2, 0.86f);
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.pickingMode = PickingMode.Position;

            var panel = new ScrollView();
            panel.style.width = 680;
            panel.style.maxHeight = 860;
            panel.style.backgroundColor = Alpha(VoidWarm, 0.98f);
            Border(panel, Gold, 1);
            Radius(panel, 20);
            panel.style.paddingTop = 36;
            panel.style.paddingBottom = 36;
            panel.style.paddingLeft = 36;
            panel.style.paddingRight = 36;
            _overlay.Add(panel);

            var actorName = string.IsNullOrWhiteSpace(_state.ActorName) ? "Warden" : _state.ActorName;
            panel.Add(BuildCenter("LEVEL UP!", Serif, 14, Gold, FontStyle.Bold, 6f));
            panel.Add(BuildCenter(actorName, Sans, 42, Parch, FontStyle.Bold));
            panel.Add(BuildCenter("Level " + _state.CurrentLevel + " → " + (_state.CurrentLevel + 1), Serif, 15, PA(0.55f), FontStyle.Italic));

            var remain = new VisualElement();
            remain.style.height = 36;
            remain.style.width = 260;
            remain.style.marginLeft = StyleKeyword.Auto;
            remain.style.marginRight = StyleKeyword.Auto;
            remain.style.marginTop = 16;
            remain.style.marginBottom = 20;
            remain.style.backgroundColor = Alpha(Panel, 0.55f);
            Border(remain, PA(0.18f), 1);
            Radius(remain, 20);
            remain.style.alignItems = Align.Center;
            remain.style.justifyContent = Justify.Center;
            _remainingLabel = Text(string.Empty, Sans, 14, Amber, FontStyle.Bold);
            remain.Add(_remainingLabel);
            panel.Add(remain);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginBottom = 24;
            panel.Add(grid);
            for (int i = 0; i < _state.Stats.Count; i++)
                grid.Add(BuildStatRow(i));

            var spellSection = Text("NEW SPELL", Sans, 10, Gold, FontStyle.Bold);
            spellSection.style.letterSpacing = 1.8f;
            spellSection.style.marginBottom = 10;
            panel.Add(spellSection);

            if (_state.SpellChoices.Count == 0)
            {
                var note = Text("No new spell is available at this level.", Serif, 13, ParchDim, FontStyle.Italic);
                note.style.marginBottom = 20;
                panel.Add(note);
            }
            else
            {
                var spells = new VisualElement();
                spells.style.flexDirection = FlexDirection.Row;
                spells.style.flexWrap = Wrap.Wrap;
                spells.style.marginBottom = 24;
                panel.Add(spells);
                for (int i = 0; i < _spellCards.Length; i++)
                {
                    var card = BuildSpellChoice(i);
                    _spellCards[i] = card;
                    spells.Add(card);
                }
            }

            _statusLabel = Text("Spend all points before confirming.", Serif, 13, ParchDim, FontStyle.Italic);
            _statusLabel.style.marginBottom = 12;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(_statusLabel);

            _confirmButton = new Button(() =>
            {
                var result = _onConfirm != null ? _onConfirm(BuildSelection()) : new LevelUpActionResult(false, "Level-up commands are unavailable.");
                _statusLabel.text = result.Message;
                _statusLabel.style.color = result.Success ? Success : Amber;
                if (result.Success)
                {
                    Close();
                    onClose?.Invoke();
                }
            })
            { text = "CONFIRM" };
            ResetButton(_confirmButton);
            _confirmButton.style.height = 46;
            _confirmButton.style.width = 180;
            _confirmButton.style.marginLeft = StyleKeyword.Auto;
            _confirmButton.style.marginRight = StyleKeyword.Auto;
            _confirmButton.style.backgroundColor = Gold;
            _confirmButton.style.color = Ink;
            _confirmButton.style.fontSize = 15;
            _confirmButton.style.letterSpacing = 2f;
            _confirmButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(_confirmButton, Serif);
            Border(_confirmButton, Amber, 1);
            Radius(_confirmButton, 10);
            panel.Add(_confirmButton);

            stageCanvas.Add(_overlay);
            RefreshStats();
            RefreshSpellSelection();
            RefreshConfirmState();
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static Label BuildCenter(string text, Font font, int size, Color color, FontStyle style, float letterSpacing = 0f)
        {
            var label = Text(text, font, size, color, style);
            label.style.letterSpacing = letterSpacing;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        private VisualElement BuildStatRow(int index)
        {
            var stat = _state.Stats[index];
            var row = Row();
            row.style.width = Length.Percent(48.5f);
            row.style.marginRight = index % 2 == 1 ? 0 : Length.Percent(3f);
            row.style.marginBottom = 10;
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 14;
            row.style.paddingRight = 14;
            row.style.backgroundColor = Dark(0.72f);
            Border(row, PA(0.12f), 1);
            Radius(row, 10);
            row.style.alignItems = Align.Center;

            var left = new VisualElement();
            left.style.minWidth = 80;
            var abbr = Text(stat.Label, Sans, 10, Stat(stat.Id), FontStyle.Bold);
            abbr.style.letterSpacing = 1.2f;
            left.Add(abbr);
            var num = Text(stat.Value.ToString(), Sans, 28, Parch, FontStyle.Bold);
            _statValueLabels[index] = num;
            left.Add(num);
            row.Add(left);

            var controls = Row();
            controls.style.marginLeft = StyleKeyword.Auto;
            var minus = BuildAdjust("−", false, ParchDim, () => AdjustStat(index, -1));
            var plus = BuildAdjust("+", true, Stat(stat.Id), () => AdjustStat(index, +1));
            _minusButtons[index] = minus;
            _plusButtons[index] = plus;
            controls.Add(minus);
            controls.Add(plus);
            row.Add(controls);
            return row;
        }

        private VisualElement BuildSpellChoice(int index)
        {
            var spell = _state.SpellChoices[index];
            var card = new Button(() => SelectSpell(spell.TemplateId));
            ResetButton(card);
            card.style.width = Length.Percent(48.5f);
            card.style.marginRight = index % 2 == 1 ? 0 : Length.Percent(3f);
            card.style.marginBottom = 10;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.flexDirection = FlexDirection.Column;
            card.Add(Text(spell.Name, Sans, 13, Parch, FontStyle.Bold));
            var head = Text(spell.School.ToUpperInvariant(), Sans, 10, School(spell.School));
            head.style.letterSpacing = 0.8f;
            head.style.marginTop = 4;
            card.Add(head);
            var fx = Text(spell.Summary + " · " + spell.ManaCost + " MP", Serif, 12, PA(0.50f), FontStyle.Italic);
            fx.style.marginTop = 6;
            fx.style.whiteSpace = WhiteSpace.Normal;
            card.Add(fx);
            return card;
        }

        private static Button BuildAdjust(string text, bool bright, Color color, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            ResetButton(button);
            button.style.width = 28;
            button.style.height = 28;
            button.style.backgroundColor = bright ? Alpha(color, 0.12f) : Alpha(Panel, 0.72f);
            button.style.color = bright ? color : ParchDim;
            Border(button, bright ? color : PA(0.18f), 1);
            Radius(button, 6);
            button.style.fontSize = 16;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(button, Sans);
            return button;
        }

        private void AdjustStat(int index, int delta)
        {
            if (delta > 0)
            {
                if (_remainingPoints <= 0)
                    return;
                _adjustments[index]++;
                _remainingPoints--;
            }
            else
            {
                if (_adjustments[index] <= 0)
                    return;
                _adjustments[index]--;
                _remainingPoints++;
            }

            RefreshStats();
            RefreshConfirmState();
        }

        private void SelectSpell(string spellId)
        {
            _selectedSpellId = spellId ?? string.Empty;
            RefreshSpellSelection();
            RefreshConfirmState();
        }

        private void RefreshStats()
        {
            _remainingLabel.text = _remainingPoints + " POINTS REMAINING";
            for (int i = 0; i < _state.Stats.Count; i++)
            {
                var stat = _state.Stats[i];
                var current = stat.Value + _adjustments[i];
                var suffix = _adjustments[i] > 0 ? " +" + _adjustments[i] : string.Empty;
                _statValueLabels[i].text = current + suffix;
                _minusButtons[i].SetEnabled(_adjustments[i] > 0);
                _plusButtons[i].SetEnabled(_remainingPoints > 0);
            }
        }

        private void RefreshSpellSelection()
        {
            for (int i = 0; i < _spellCards.Length; i++)
            {
                var selected = string.Equals(_state.SpellChoices[i].TemplateId, _selectedSpellId, StringComparison.Ordinal);
                _spellCards[i].style.backgroundColor = selected ? GA(0.10f) : Dark(0.62f);
                Border(_spellCards[i], selected ? Gold : PA(0.10f), selected ? 2 : 1);
            }
        }

        private void RefreshConfirmState()
        {
            var spellReady = _state.SpellChoices.Count == 0 || !string.IsNullOrWhiteSpace(_selectedSpellId);
            _confirmButton.SetEnabled(_remainingPoints == 0 && spellReady);
            if (_remainingPoints > 0)
            {
                _statusLabel.text = "Spend all points before confirming.";
                _statusLabel.style.color = ParchDim;
                return;
            }

            _statusLabel.text = _state.SpellChoices.Count == 0
                ? "Confirm to apply the attribute gains."
                : "Confirm to apply the attribute gains and learn the selected spell.";
            _statusLabel.style.color = ParchDim;
        }

        private LevelUpSelection BuildSelection()
        {
            return new LevelUpSelection(
                StatDelta("MIG"),
                StatDelta("AGI"),
                StatDelta("END"),
                StatDelta("MND"),
                StatDelta("INS"),
                StatDelta("PRE"),
                _selectedSpellId);
        }

        private int StatDelta(string statId)
        {
            for (int i = 0; i < _state.Stats.Count; i++)
            {
                if (string.Equals(_state.Stats[i].Id, statId, StringComparison.Ordinal))
                    return _adjustments[i];
            }
            return 0;
        }
    }
}
