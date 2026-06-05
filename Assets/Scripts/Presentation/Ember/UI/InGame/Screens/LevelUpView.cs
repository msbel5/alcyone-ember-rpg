using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class LevelUpView
    {
        private readonly VisualElement _overlay;
        private readonly Label _remainingLabel;
        private readonly Label[] _statValueLabels;
        private readonly Button[] _minusButtons;
        private readonly Button[] _plusButtons;
        private readonly VisualElement[] _spellCards;
        private readonly StatData[] _baseStats;
        private readonly int[] _adjustments;
        private readonly List<SpellData> _spellOptions;
        private readonly Action _onConfirm;
        private int _remainingPoints;
        private string _selectedSpellName;

        public LevelUpView(VisualElement stageCanvas, Action onClose, Action onConfirm = null)
        {
            _baseStats = IgMockData.Player.Stats;
            _adjustments = new int[_baseStats.Length];
            _statValueLabels = new Label[_baseStats.Length];
            _minusButtons = new Button[_baseStats.Length];
            _plusButtons = new Button[_baseStats.Length];
            _spellOptions = IgMockData.GetAllSpells();
            _spellCards = new VisualElement[Mathf.Min(6, _spellOptions.Count)];
            _selectedSpellName = _spellOptions.Count > 0 ? _spellOptions[0].Name : string.Empty;
            _remainingPoints = 5;
            _onConfirm = onConfirm;

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

            var fanfare = new VisualElement();
            fanfare.style.alignItems = Align.Center;
            fanfare.style.marginBottom = 28;
            fanfare.Add(BuildCenter("LEVEL UP!", Serif, 14, Gold, FontStyle.Bold, 6f));
            fanfare.Add(BuildCenter($"Level {IgMockData.Player.Level + 1}", Sans, 48, Parch, FontStyle.Bold));
            fanfare.Add(BuildCenter("Distribute 5 attribute points.", Serif, 15, PA(0.55f), FontStyle.Italic));
            panel.Add(fanfare);

            var remain = new VisualElement();
            remain.style.height = 36;
            remain.style.width = 220;
            remain.style.marginLeft = StyleKeyword.Auto;
            remain.style.marginRight = StyleKeyword.Auto;
            remain.style.marginBottom = 20;
            remain.style.backgroundColor = Alpha(Panel, 0.55f);
            Border(remain, PA(0.18f), 1);
            Radius(remain, 20);
            remain.style.alignItems = Align.Center;
            remain.style.justifyContent = Justify.Center;
            _remainingLabel = Text("5 POINTS REMAINING", Sans, 14, Amber, FontStyle.Bold);
            remain.Add(_remainingLabel);
            panel.Add(remain);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginBottom = 24;
            panel.Add(grid);
            for (int i = 0; i < IgMockData.Player.Stats.Length; i++)
                grid.Add(BuildStatRow(i));

            var spellSection = Text("SPELLS", Sans, 10, Gold, FontStyle.Bold);
            spellSection.style.letterSpacing = 1.8f;
            spellSection.style.marginBottom = 10;
            panel.Add(spellSection);
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

            var confirm = new Button(() =>
            {
                _onConfirm?.Invoke();
                onClose?.Invoke();
            }) { text = "CONFIRM" };
            ResetButton(confirm);
            confirm.style.height = 46;
            confirm.style.width = 180;
            confirm.style.marginLeft = StyleKeyword.Auto;
            confirm.style.marginRight = StyleKeyword.Auto;
            confirm.style.backgroundColor = Gold;
            confirm.style.color = Ink;
            confirm.style.fontSize = 15;
            confirm.style.letterSpacing = 2f;
            confirm.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(confirm, Serif);
            Border(confirm, Amber, 1);
            Radius(confirm, 10);
            panel.Add(confirm);

            stageCanvas.Add(_overlay);
            RefreshStats();
            RefreshSpellSelection();
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
            var stat = _baseStats[index];
            var row = Row();
            row.style.width = Length.Percent(48.5f);
            row.style.marginRight = (stat.Abbr == "AGI" || stat.Abbr == "MND" || stat.Abbr == "PRE") ? 0 : Length.Percent(3f);
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
            var abbr = Text(stat.Abbr, Sans, 10, Stat(stat.Abbr), FontStyle.Bold);
            abbr.style.letterSpacing = 1.2f;
            left.Add(abbr);
            var num = Text(stat.Value.ToString(), Sans, 28, Parch, FontStyle.Bold);
            _statValueLabels[index] = num;
            left.Add(num);
            row.Add(left);

            var controls = Row();
            controls.style.marginLeft = StyleKeyword.Auto;
            var minus = BuildAdjust("−", false, ParchDim, () => AdjustStat(index, -1));
            var plus = BuildAdjust("+", true, Stat(stat.Abbr), () => AdjustStat(index, +1));
            _minusButtons[index] = minus;
            _plusButtons[index] = plus;
            controls.Add(minus);
            controls.Add(plus);
            row.Add(controls);
            return row;
        }

        private VisualElement BuildSpellChoice(int index)
        {
            var spell = _spellOptions[index];
            bool selected = spell.Name == _selectedSpellName;
            string school = FindSchool(spell.Name);
            var card = new Button(() => SelectSpell(spell.Name));
            ResetButton(card);
            card.style.width = Length.Percent(48.5f);
            card.style.marginRight = selected || spell.Name == "Shock Burst" || spell.Name == "Cure Poison" || spell.Name == "Charm" ? 0 : Length.Percent(3f);
            card.style.marginBottom = 10;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.backgroundColor = selected ? GA(0.10f) : Dark(0.62f);
            Border(card, selected ? Gold : PA(0.10f), selected ? 2 : 1);
            Radius(card, 10);
            card.style.flexDirection = FlexDirection.Column;
            card.Add(Text(spell.Name, Sans, 13, selected ? Parch : ParchDim, FontStyle.Bold));
            var head = Text(school.ToUpperInvariant(), Sans, 10, School(school));
            head.style.letterSpacing = 0.8f;
            head.style.marginTop = 4;
            card.Add(head);
            var fx = Text($"{spell.Effect} · {spell.ManaCost} MP", Serif, 12, PA(0.50f), FontStyle.Italic);
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
                if (_remainingPoints <= 0) return;
                _adjustments[index] += 1;
                _remainingPoints -= 1;
            }
            else
            {
                if (_adjustments[index] <= 0) return;
                _adjustments[index] -= 1;
                _remainingPoints += 1;
            }

            RefreshStats();
        }

        private void SelectSpell(string spellName)
        {
            _selectedSpellName = spellName ?? string.Empty;
            RefreshSpellSelection();
        }

        private void RefreshStats()
        {
            _remainingLabel.text = _remainingPoints + " POINTS REMAINING";
            for (int i = 0; i < _baseStats.Length; i++)
            {
                int current = _baseStats[i].Value + _adjustments[i];
                string suffix = _adjustments[i] > 0 ? " +" + _adjustments[i] : string.Empty;
                _statValueLabels[i].text = current + suffix;
                _minusButtons[i].SetEnabled(_adjustments[i] > 0);
                _plusButtons[i].SetEnabled(_remainingPoints > 0);
            }
        }

        private void RefreshSpellSelection()
        {
            for (int i = 0; i < _spellCards.Length; i++)
            {
                var spell = _spellOptions[i];
                bool selected = spell.Name == _selectedSpellName;
                _spellCards[i].style.backgroundColor = selected ? GA(0.10f) : Dark(0.62f);
                Border(_spellCards[i], selected ? Gold : PA(0.10f), selected ? 2 : 1);
            }
        }

        private static string FindSchool(string spellName)
        {
            for (int i = 0; i < IgMockData.SpellSchools.Length; i++)
            {
                for (int s = 0; s < IgMockData.SpellSchools[i].Spells.Length; s++)
                {
                    if (IgMockData.SpellSchools[i].Spells[s].Name == spellName)
                        return IgMockData.SpellSchools[i].Name;
                }
            }
            return "Destruction";
        }
    }
}
