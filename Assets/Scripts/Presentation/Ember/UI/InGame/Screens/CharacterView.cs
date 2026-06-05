// Why this file is intentionally long: the complete Character screen stays in one UI Toolkit view so layout, live binding hooks, and modal-specific styling remain co-located and readable.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class CharacterView
    {
        private readonly VisualElement _overlay;
        private Image _portraitImage;
        private Label _portraitGlyph;

        public bool HasPortrait { get; private set; }

        public CharacterView(VisualElement stageCanvas, Action onClose, Action<string> onOpenScreen = null)
        {
            _overlay = IgModal.Build("Character", false, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Row;
            content.style.backgroundColor = VoidWarm;

            content.Add(BuildIdentityPane());
            content.Add(BuildStatsPane());
            content.Add(BuildSkillsPane(onOpenScreen));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        public void SetPortrait(Sprite sprite)
        {
            if (sprite == null || _portraitImage == null)
                return;

            _portraitImage.sprite = sprite;
            _portraitImage.style.display = DisplayStyle.Flex;
            if (_portraitGlyph != null)
                _portraitGlyph.style.display = DisplayStyle.None;
            HasPortrait = true;
        }

        private VisualElement BuildIdentityPane()
        {
            var pane = new VisualElement();
            pane.style.width = 220;
            pane.style.borderRightWidth = 1;
            pane.style.borderRightColor = PA(0.10f);
            pane.style.paddingTop = 20;
            pane.style.paddingBottom = 20;
            pane.style.paddingLeft = 20;
            pane.style.paddingRight = 20;
            pane.style.flexShrink = 0;

            var portrait = new VisualElement();
            portrait.style.width = Length.Percent(100);
            portrait.style.height = 240;
            portrait.style.backgroundColor = InputBg;
            Border(portrait, Amber, 2);
            Radius(portrait, 12);
            portrait.style.overflow = Overflow.Hidden;

            _portraitImage = new Image();
            _portraitImage.scaleMode = ScaleMode.ScaleToFit;
            _portraitImage.style.position = Position.Absolute;
            _portraitImage.style.left = 0;
            _portraitImage.style.right = 0;
            _portraitImage.style.top = 0;
            _portraitImage.style.bottom = 0;
            _portraitImage.style.display = DisplayStyle.None;
            portrait.Add(_portraitImage);

            _portraitGlyph = Text("C", Serif, 96, GA(0.24f), FontStyle.Bold);
            _portraitGlyph.style.position = Position.Absolute;
            _portraitGlyph.style.left = 0;
            _portraitGlyph.style.right = 0;
            _portraitGlyph.style.bottom = 18;
            _portraitGlyph.style.unityTextAlign = TextAnchor.MiddleCenter;
            portrait.Add(_portraitGlyph);
            pane.Add(portrait);

            var name = Text(IgMockData.Player.Name, Serif, 18, Parch, FontStyle.Bold);
            pane.Add(name);
            var klass = Text($"LV {IgMockData.Player.Level} {IgMockData.Player.ClassName}".ToUpperInvariant(), Sans, 12, Gold);
            klass.style.letterSpacing = 0.6f;
            pane.Add(klass);
            var sign = Text($"{IgMockData.Player.Birthsign} · {IgMockData.Player.Alignment}", Serif, 12, PA(0.45f), FontStyle.Italic);
            pane.Add(sign);

            var meters = new VisualElement();
            meters.style.marginTop = 12;

            var xp = BuildXpBar();
            xp.style.marginBottom = 5;
            meters.Add(xp);

            var hp = BuildVital("HP", IgMockData.Player.Hp, IgMockData.Player.HpMax, Health);
            hp.style.marginBottom = 5;
            meters.Add(hp);

            var fatigue = BuildVital("FAT", IgMockData.Player.Fatigue, IgMockData.Player.FatigueMax, Fatigue);
            fatigue.style.marginBottom = 5;
            meters.Add(fatigue);

            meters.Add(BuildVital("MP", IgMockData.Player.Mana, IgMockData.Player.ManaMax, Mana));
            pane.Add(meters);
            return pane;
        }

        private static VisualElement BuildStatsPane()
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.paddingTop = 20;
            pane.style.paddingBottom = 20;
            pane.style.paddingLeft = 24;
            pane.style.paddingRight = 24;

            var heading = Text("ATTRIBUTES", Sans, 10, Gold, FontStyle.Bold);
            heading.style.letterSpacing = 1.8f;
            pane.Add(heading);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            pane.Add(grid);

            for (int i = 0; i < IgMockData.Player.Stats.Length; i++)
            {
                var stat = IgMockData.Player.Stats[i];
                int mod = Mathf.FloorToInt((stat.Value - 10) / 2f);
                var card = new VisualElement();
                card.style.width = Length.Percent(31.5f);
                card.style.marginRight = Length.Percent((i % 3) == 2 ? 0f : 2.75f);
                card.style.marginBottom = 12;
                card.style.backgroundColor = Dark(0.72f);
                Border(card, PA(0.12f), 1);
                Radius(card, 12);
                card.style.paddingTop = 14;
                card.style.paddingBottom = 14;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;
                card.style.alignItems = Align.Center;

                var abbr = Text(stat.Abbr, Sans, 10, Stat(stat.Abbr), FontStyle.Bold);
                abbr.style.letterSpacing = 1.5f;
                card.Add(abbr);
                var value = Text(stat.Value.ToString(), Sans, 44, Parch, FontStyle.Bold);
                card.Add(value);
                var modLabel = Text((mod >= 0 ? "+" : string.Empty) + mod, Sans, 12, PA(0.40f));
                modLabel.style.marginTop = 4;
                card.Add(modLabel);
                grid.Add(card);
            }

            var derivedWrap = new VisualElement();
            derivedWrap.style.borderTopWidth = 1;
            derivedWrap.style.borderTopColor = PA(0.08f);
            derivedWrap.style.paddingTop = 12;
            pane.Add(derivedWrap);

            var derived = Text("DERIVED", Sans, 10, Gold, FontStyle.Bold);
            derived.style.letterSpacing = 1.8f;
            derived.style.marginBottom = 10;
            derivedWrap.Add(derived);

            string[,] rows =
            {
                { "Initiative", "14 (+4 AGI)" },
                { "Carry Cap", "50 kg" },
                { "Spell Res", "32%" },
                { "Crit Chance", "8%" },
            };
            for (int i = 0; i < rows.GetLength(0); i++)
                derivedWrap.Add(BuildPairRow(rows[i, 0], rows[i, 1]));
            return pane;
        }

        private static VisualElement BuildSkillsPane(Action<string> onOpenScreen)
        {
            var pane = new ScrollView();
            pane.style.width = 240;
            pane.style.flexShrink = 0;
            pane.style.borderLeftWidth = 1;
            pane.style.borderLeftColor = PA(0.10f);
            pane.style.paddingTop = 20;
            pane.style.paddingBottom = 20;
            pane.style.paddingLeft = 16;
            pane.style.paddingRight = 16;

            var heading = Text("SKILLS", Sans, 10, Gold, FontStyle.Bold);
            heading.style.letterSpacing = 1.8f;
            heading.style.marginBottom = 12;
            pane.Add(heading);

            var set = new HashSet<string>(IgMockData.Player.Skills);
            string[] list =
            {
                "investigation", "arcana", "perception", "insight", "history",
                "athletics", "stealth", "persuasion", "medicine", "deception",
            };

            for (int i = 0; i < list.Length; i++)
            {
                bool active = set.Contains(list[i]);
                int level = active ? Mathf.FloorToInt(IgMockData.Player.Stats[4].Value / 10f) + 2 : 1;
                var row = Row();
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 7;
                row.style.paddingBottom = 7;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = PA(0.06f);

                var dot = new VisualElement();
                dot.style.width = 6;
                dot.style.height = 6;
                dot.style.marginRight = 8;
                dot.style.backgroundColor = active ? Gold : PA(0.15f);
                Radius(dot, 999);
                row.Add(dot);

                var name = Text(char.ToUpperInvariant(list[i][0]) + list[i].Substring(1), Sans, 12, active ? Parch : PA(0.38f));
                name.style.flexGrow = 1;
                row.Add(name);

                var val = Text("+" + level, Sans, 11, active ? Amber : PA(0.25f), FontStyle.Bold);
                row.Add(val);
                pane.Add(row);
            }

            var btn = new Button(() => onOpenScreen?.Invoke("levelup")) { text = "LEVEL UP!" };
            ResetButton(btn);
            btn.style.marginTop = 14;
            btn.style.height = 36;
            btn.style.backgroundColor = Gold;
            btn.style.color = Ink;
            btn.style.fontSize = 12;
            btn.style.letterSpacing = 0.8f;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(btn, Sans);
            Border(btn, Amber, 1);
            Radius(btn, 7);
            pane.Add(btn);
            return pane;
        }

        private static VisualElement BuildXpBar()
        {
            var wrap = new VisualElement();
            var row = Row();
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 6;
            row.Add(Text("XP", Sans, 10, PA(0.38f)));
            row.Add(Text($"{IgMockData.Player.Xp} / {IgMockData.Player.XpNext}", Sans, 10, PA(0.38f)));
            wrap.Add(row);

            var track = new VisualElement();
            track.style.height = 6;
            track.style.backgroundColor = C(0, 0, 0, 0.4f);
            Radius(track, 3);
            var fill = new VisualElement();
            fill.style.height = 6;
            fill.style.width = Length.Percent((float)IgMockData.Player.Xp / IgMockData.Player.XpNext * 100f);
            fill.style.backgroundColor = Gold;
            Radius(fill, 3);
            track.Add(fill);
            wrap.Add(track);
            return wrap;
        }

        private static VisualElement BuildVital(string label, int value, int max, Color color)
        {
            var root = new VisualElement();
            root.style.height = 20;
            root.style.backgroundColor = C(0, 0, 0, 0.4f);
            Radius(root, 6);
            var fill = new VisualElement();
            fill.style.position = Position.Absolute;
            fill.style.left = 2;
            fill.style.top = 2;
            fill.style.bottom = 2;
            fill.style.width = Length.Percent((float)value / max * 100f);
            fill.style.backgroundColor = color;
            Radius(fill, 4);
            root.Add(fill);
            var text = Text($"{label} {value}/{max}", Sans, 9, Bone, FontStyle.Bold);
            text.style.letterSpacing = 0.6f;
            text.style.position = Position.Absolute;
            text.style.left = 0;
            text.style.right = 0;
            text.style.top = 0;
            text.style.bottom = 0;
            text.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(text);
            return root;
        }

        private static VisualElement BuildPairRow(string key, string value)
        {
            var row = Row();
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = PA(0.06f);
            row.Add(Text(key, Sans, 12, PA(0.38f)));
            row.Add(Text(value, Sans, 12, Parch));
            return row;
        }
    }
}
