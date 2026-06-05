// Why this file is intentionally long: the live combat overlay keeps its layout, read-model refresh, and action wiring together so the screen stays self-contained while the rest of the in-game browser is still being extracted.
using System;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CombatView
    {
        private readonly VisualElement _overlay;
        private readonly VisualElement _center;
        private readonly Label _stateLabel;
        private readonly Label _logLine;
        private readonly Label _playerName;
        private readonly Label _enemyName;
        private readonly VisualElement _playerHealthFill;
        private readonly VisualElement _playerFatigueFill;
        private readonly VisualElement _playerManaFill;
        private readonly VisualElement _enemyHealthFill;
        private readonly VisualElement _actions;
        private readonly Action<string> _onAction;
        private string _actionSignature = string.Empty;
        private bool _showingEmpty = true;

        public CombatView(VisualElement stageCanvas, Action onClose, EmberCrpg.Presentation.Ember.UI.CombatScreenState state, Action<string> onAction = null, Action onFlee = null)
        {
            _onAction = onAction;
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.top = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = C(4, 3, 2, 0.72f);
            _overlay.pickingMode = PickingMode.Position;

            _overlay.Add(BuildTopNav(onFlee ?? onClose));
            _stateLabel = _overlay.Q<Label>("combat-state-label");

            _center = new VisualElement();
            _center.style.position = Position.Absolute;
            _center.style.left = 0;
            _center.style.right = 0;
            _center.style.top = 0;
            _center.style.bottom = 0;
            _center.style.alignItems = Align.Center;
            _center.style.justifyContent = Justify.Center;
            _overlay.Add(_center);

            var vitals = new VisualElement();
            vitals.style.position = Position.Absolute;
            vitals.style.left = 26;
            vitals.style.bottom = 110;
            vitals.style.width = 360;
            _playerName = Text(string.Empty, Sans, 13, Parch, FontStyle.Bold);
            vitals.Add(_playerName);

            var bars = Row();
            bars.style.marginTop = 8;
            bars.Add(BuildBar("HP", Health, out _playerHealthFill));
            bars.Add(BuildBar("FAT", Fatigue, out _playerFatigueFill));
            bars.Add(BuildBar("MP", Mana, out _playerManaFill));
            vitals.Add(bars);
            _overlay.Add(vitals);

            var enemy = new VisualElement();
            enemy.style.position = Position.Absolute;
            enemy.style.right = 26;
            enemy.style.bottom = 110;
            enemy.style.width = 320;
            enemy.style.paddingTop = 12;
            enemy.style.paddingBottom = 12;
            enemy.style.paddingLeft = 14;
            enemy.style.paddingRight = 14;
            enemy.style.backgroundColor = C(12, 9, 6, 0.82f);
            Border(enemy, PA(0.12f), 1);
            Radius(enemy, 12);
            _enemyName = Text(string.Empty, Sans, 13, Gold, FontStyle.Bold);
            enemy.Add(_enemyName);
            enemy.Add(BuildBar("HP", Health, out _enemyHealthFill));
            _logLine = Text(string.Empty, Sans, 11, ParchDim);
            _logLine.style.marginTop = 10;
            _logLine.style.whiteSpace = WhiteSpace.Normal;
            enemy.Add(_logLine);
            _overlay.Add(enemy);

            _actions = Row();
            _actions.style.position = Position.Absolute;
            _actions.style.left = 26;
            _actions.style.right = 26;
            _actions.style.bottom = 26;
            _actions.style.flexWrap = Wrap.Wrap;
            _actions.style.justifyContent = Justify.Center;
            _overlay.Add(_actions);

            stageCanvas.Add(_overlay);
            Refresh(state);
        }

        public void Close()
        {
            _overlay?.RemoveFromHierarchy();
        }

        public void Refresh(EmberCrpg.Presentation.Ember.UI.CombatScreenState state)
        {
            if (state == null)
            {
                state = new EmberCrpg.Presentation.Ember.UI.CombatScreenState(false, "Unknown", 0, 0, 0, 0, 0, 0, string.Empty, 0, 0, string.Empty, Array.Empty<EmberCrpg.Presentation.Ember.UI.CombatSpellActionRow>());
            }

            _playerName.text = state.PlayerName;
            _enemyName.text = state.HasEncounter ? state.EnemyName : "No Hostile Contact";
            _logLine.text = string.IsNullOrEmpty(state.LastEventLine) ? "No recent combat event." : state.LastEventLine;

            SetBar(_playerHealthFill, state.PlayerHealth, state.PlayerHealthMax);
            SetBar(_playerFatigueFill, state.PlayerFatigue, state.PlayerFatigueMax);
            SetBar(_playerManaFill, state.PlayerMana, state.PlayerManaMax);
            SetBar(_enemyHealthFill, state.EnemyHealth, state.EnemyHealthMax);

            if (!state.HasEncounter)
            {
                if (!_showingEmpty)
                {
                    _center.Clear();
                    _showingEmpty = true;
                }

                if (_center.childCount == 0)
                {
                    _center.Add(EmptyState(
                        "Combat",
                        "No live encounter in this room.",
                        "Combat becomes actionable once a hostile actor shares the player room."));
                }

                _stateLabel.text = "NO ACTIVE ENCOUNTER";
                _actionSignature = string.Empty;
                _actions.style.display = DisplayStyle.None;
                return;
            }

            if (_showingEmpty)
            {
                _center.Clear();
                _showingEmpty = false;
            }

            _stateLabel.text = "LIVE ENCOUNTER";
            _actions.style.display = DisplayStyle.Flex;
            var signature = BuildActionSignature(state);
            if (!string.Equals(signature, _actionSignature, StringComparison.Ordinal))
            {
                _actionSignature = signature;
                RebuildActions(state);
            }
        }

        private void RebuildActions(EmberCrpg.Presentation.Ember.UI.CombatScreenState state)
        {
            _actions.Clear();
            _actions.Add(BuildActionButton("ATTACK", "Strike the nearest hostile.", "attack", true));
            for (int i = 0; i < state.Spells.Count; i++)
            {
                var spell = state.Spells[i];
                _actions.Add(BuildActionButton(
                    spell.Name.ToUpperInvariant(),
                    spell.School + " · " + spell.ManaCost + " MP",
                    spell.ActionId,
                    spell.Enabled));
            }
        }

        private Button BuildActionButton(string label, string hint, string actionId, bool enabled)
        {
            var button = new Button(() => _onAction?.Invoke(actionId));
            ResetButton(button);
            button.SetEnabled(enabled);
            button.style.width = 170;
            button.style.minHeight = 58;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.marginTop = 4;
            button.style.marginBottom = 4;
            button.style.paddingTop = 9;
            button.style.paddingBottom = 9;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.backgroundColor = enabled ? C(16, 12, 8, 0.84f) : C(10, 8, 6, 0.45f);
            Border(button, enabled ? PA(0.16f) : PA(0.08f), 1);
            Radius(button, 10);

            var text = Col();
            text.style.alignItems = Align.FlexStart;
            text.Add(Text(label, Sans, 11, enabled ? Gold : PA(0.24f), FontStyle.Bold));
            text.Add(Text(hint, Sans, 9, enabled ? ParchDim : PA(0.18f)));
            button.Add(text);
            return button;
        }

        private static VisualElement BuildTopNav(Action onClose)
        {
            var nav = Row();
            nav.style.position = Position.Absolute;
            nav.style.right = 26;
            nav.style.top = 14;
            nav.style.alignItems = Align.Center;
            var state = Text("NO ACTIVE ENCOUNTER", Sans, 11, PA(0.52f));
            state.name = "combat-state-label";
            state.style.letterSpacing = 1.4f;
            nav.Add(state);

            var close = new Button(() => onClose?.Invoke()) { text = "CLOSE" };
            ResetButton(close);
            close.style.height = 32;
            close.style.paddingLeft = 12;
            close.style.paddingRight = 12;
            close.style.backgroundColor = C(8, 6, 4, 0.70f);
            close.style.color = PA(0.38f);
            close.style.fontSize = 11;
            ApplyFont(close, Sans);
            Border(close, PA(0.15f), 1);
            Radius(close, 7);
            nav.Add(close);
            return nav;
        }

        private static VisualElement BuildBar(string label, Color color, out VisualElement fill)
        {
            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.style.height = 20;
            root.style.backgroundColor = C(0, 0, 0, 0.4f);
            root.style.marginTop = 6;
            root.style.marginRight = 6;
            Radius(root, 999);

            fill = new VisualElement();
            fill.style.position = Position.Absolute;
            fill.style.left = 2;
            fill.style.top = 2;
            fill.style.bottom = 2;
            fill.style.backgroundColor = color;
            Radius(fill, 999);
            root.Add(fill);

            var text = Text(label, Sans, 9, Bone, FontStyle.Bold);
            text.style.letterSpacing = 0.6f;
            text.style.position = Position.Absolute;
            text.style.left = 8;
            text.style.top = 0;
            text.style.bottom = 0;
            text.style.unityTextAlign = TextAnchor.MiddleLeft;
            root.Add(text);
            return root;
        }

        private static void SetBar(VisualElement fill, int value, int max)
        {
            if (fill == null)
                return;

            var ratio = max <= 0 ? 0f : Mathf.Clamp01((float)value / max);
            fill.style.width = Length.Percent(ratio * 100f);
        }

        private static VisualElement Col()
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Column;
            return element;
        }

        private static string BuildActionSignature(EmberCrpg.Presentation.Ember.UI.CombatScreenState state)
        {
            var parts = new string[state.Spells.Count + 1];
            parts[0] = "attack";
            for (int i = 0; i < state.Spells.Count; i++)
            {
                var spell = state.Spells[i];
                parts[i + 1] = spell.ActionId + "|" + spell.Enabled;
            }

            return string.Join(";", parts);
        }
    }
}
