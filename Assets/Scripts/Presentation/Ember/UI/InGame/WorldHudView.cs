using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame
{
    /// <summary>Live values the World HUD shows — populated each frame by the in-game UI controller.</summary>
    public struct WorldHudData
    {
        public string Location;     // "Day 3 · Dusk · Ashton Crossroads"
        public string EventLine;    // most recent world-event / narration line
        public string CompassLine;  // optional quest/navigation target
        public int Gold, Level;
        public string ClassName;
        public int Hp, HpMax, Fatigue, FatigueMax, Mana, ManaMax;
        public IReadOnlyList<string> SpellSlots;  // up to 5 names (null entry = empty slot)
    }

    /// <summary>
    /// The World HUD overlay — TopBar (day/time/location · gold · level) + BottomHud (event line · HP/FAT/MP
    /// vitals · 5-slot spell bar · I/C/M/J/K/DM buttons). A 1:1 port of ig-ds.jsx TopBar + BottomHud. Sits on the
    /// in-game stage over the live 3D world; <see cref="Refresh"/> binds real values each frame. Bars/labels
    /// ignore picking so they never eat world clicks; only the buttons are interactive.
    /// </summary>
    public sealed class WorldHudView
    {
        public Action<string> OnOpenScreen;   // "inventory" / "character" / "worldmap" / "journal" / "colony"
        public Action OnConsulDm;

        private readonly Label _location, _gold, _level, _eventLine, _compassLine;
        private readonly VitalBar _hp, _fat, _mp;
        private readonly List<Label> _spellLabels = new List<Label>();

        public WorldHudView(VisualElement canvas)
        {
            // ── TOP BAR (height 48) ──
            var top = Row();
            top.pickingMode = PickingMode.Ignore;
            top.style.position = Position.Absolute; top.style.left = 0; top.style.right = 0; top.style.top = 0;
            top.style.height = 48; top.style.alignItems = Align.Center;
            top.style.paddingLeft = 28; top.style.paddingRight = 28;
            _location = Text("Day 3 · Dusk · Ashton Crossroads", Sans, 11, PA(0.40f));
            _location.style.letterSpacing = 2f;
            top.Add(_location);
            var topRight = Row(); topRight.style.marginLeft = StyleKeyword.Auto;
            topRight.style.alignItems = Align.Center;
            // Hidden until the simulation actually tracks gold + level — no fake "0 gp / Lv 0" on the HUD.
            topRight.style.display = DisplayStyle.None;
            _gold = Text("⊙ 0 gp", Sans, 12, Amber, FontStyle.Bold);
            _gold.style.marginRight = 8;
            topRight.Add(_gold);
            _level = Text("Lv 1", Sans, 11, PA(0.38f));
            topRight.Add(_level);
            top.Add(topRight);
            canvas.Add(top);

            // ── BOTTOM HUD (height 90) ──
            var bottom = Row();
            bottom.pickingMode = PickingMode.Ignore;
            bottom.style.position = Position.Absolute; bottom.style.left = 0; bottom.style.right = 0; bottom.style.bottom = 0;
            bottom.style.height = 90; bottom.style.alignItems = Align.FlexEnd;
            bottom.style.paddingLeft = 26; bottom.style.paddingRight = 26; bottom.style.paddingBottom = 18;

            // event log line
            var eventWrap = new VisualElement(); eventWrap.style.width = 320; eventWrap.style.flexShrink = 0;
            _compassLine = Text("", Sans, 11, Gold, FontStyle.Bold);
            _compassLine.style.letterSpacing = 0.8f;
            _compassLine.style.marginBottom = 4;
            _compassLine.style.display = DisplayStyle.None;
            eventWrap.Add(_compassLine);
            _eventLine = Text("", Serif, 13, PA(0.60f), FontStyle.Italic);
            _eventLine.style.whiteSpace = WhiteSpace.Normal;
            eventWrap.Add(_eventLine);
            bottom.Add(eventWrap);

            // vitals (flex 1)
            var vitals = Row(); vitals.style.flexGrow = 1; vitals.style.marginLeft = 24;
            _hp = new VitalBar("HP", Health); _fat = new VitalBar("FAT", Fatigue); _mp = new VitalBar("MP", Mana);
            _hp.Root.style.marginRight = 9; _fat.Root.style.marginRight = 9;
            vitals.Add(_hp.Root); vitals.Add(_fat.Root); vitals.Add(_mp.Root);
            bottom.Add(vitals);

            // spell bar (5 slots)
            var spells = Row(); spells.style.flexShrink = 0; spells.style.marginLeft = 24;
            for (int i = 0; i < 5; i++)
            {
                var slot = new VisualElement();
                slot.style.width = 50; slot.style.height = 50; Radius(slot, 9);
                slot.style.marginRight = i < 4 ? 6 : 0;
                slot.style.backgroundColor = Dark(0.6f);
                Border(slot, i == 0 ? Gold : PA(0.18f), i == 0 ? 2 : 1);
                slot.style.alignItems = Align.Center; slot.style.justifyContent = Justify.Center;
                var num = Text((i + 1).ToString(), Sans, 9, WA(0.5f));
                num.style.position = Position.Absolute; num.style.top = 3; num.style.left = 5;
                slot.Add(num);
                var name = Text("", Sans, 8, ParchDim);
                name.style.unityTextAlign = TextAnchor.MiddleCenter; name.style.marginTop = 4;
                slot.Add(name);
                _spellLabels.Add(name);
                spells.Add(slot);
            }
            bottom.Add(spells);

            // buttons I / C / M / J / K + DM
            var buttons = Row(); buttons.style.flexShrink = 0; buttons.style.marginLeft = 24;
            buttons.style.alignItems = Align.FlexEnd;
            foreach (var (key, screen) in new[] { ("I", "inventory"), ("C", "character"), ("M", "worldmap"), ("J", "journal"), ("K", "colony") })
            {
                var s = screen;
                var b = new Button(() => OnOpenScreen?.Invoke(s)) { text = key };
                ResetButton(b);
                b.style.width = 34; b.style.height = 34; Radius(b, 7);
                b.style.marginRight = 6;
                b.style.backgroundColor = Dark(0.72f); Border(b, PA(0.18f), 1);
                b.style.fontSize = 12; b.style.color = PA(0.55f); ApplyFont(b, Sans);
                b.style.unityFontStyleAndWeight = FontStyle.Bold;
                buttons.Add(b);
            }
            var dm = new Button(() => OnConsulDm?.Invoke()) { text = "⌖ DM" };
            ResetButton(dm);
            dm.style.height = 34; Radius(dm, 7);
            dm.style.paddingLeft = 10; dm.style.paddingRight = 10;
            dm.style.backgroundColor = C(10, 9, 8, 0.72f); Border(dm, Amber, 1);
            dm.style.fontSize = 10; dm.style.color = Gold; ApplyFont(dm, Sans);
            dm.style.unityFontStyleAndWeight = FontStyle.Bold; dm.style.letterSpacing = 1f;
            buttons.Add(dm);
            bottom.Add(buttons);

            canvas.Add(bottom);
        }

        public void Refresh(in WorldHudData d)
        {
            if (!string.IsNullOrEmpty(d.Location)) _location.text = d.Location;
            _compassLine.text = d.CompassLine ?? string.Empty;
            _compassLine.style.display = string.IsNullOrEmpty(d.CompassLine) ? DisplayStyle.None : DisplayStyle.Flex;
            _eventLine.text = d.EventLine ?? string.Empty;
            _gold.text = "⊙ " + d.Gold + " gp";
            _level.text = "Lv " + d.Level + (string.IsNullOrEmpty(d.ClassName) ? "" : " " + d.ClassName);
            _hp.Set(d.Hp, d.HpMax); _fat.Set(d.Fatigue, d.FatigueMax); _mp.Set(d.Mana, d.ManaMax);
            for (int i = 0; i < _spellLabels.Count; i++)
            {
                string name = d.SpellSlots != null && i < d.SpellSlots.Count ? d.SpellSlots[i] : null;
                _spellLabels[i].text = string.IsNullOrEmpty(name) ? "" : FirstWord(name);
            }
        }

        private static string FirstWord(string s)
        {
            int sp = s.IndexOf(' ');
            return sp > 0 ? s.Substring(0, sp) : s;
        }

        /// <summary>A labelled vital bar (HP/FAT/MP): coloured fill + "LABEL n/m" centred.</summary>
        private sealed class VitalBar
        {
            public readonly VisualElement Root;
            private readonly VisualElement _fill;
            private readonly Label _text;
            private readonly string _label;

            public VitalBar(string label, Color color)
            {
                _label = label;
                Root = new VisualElement();
                Root.style.flexGrow = 1; Root.style.height = 24; Radius(Root, 6);
                Root.style.backgroundColor = C(0, 0, 0, 0.45f);
                _fill = new VisualElement();
                _fill.style.position = Position.Absolute; _fill.style.left = 2; _fill.style.top = 2; _fill.style.bottom = 2;
                _fill.style.backgroundColor = color; Radius(_fill, 6);
                Root.Add(_fill);
                _text = Text(label, Sans, 10, Bone, FontStyle.Bold);
                _text.style.letterSpacing = 0.8f;
                _text.style.position = Position.Absolute; _text.style.left = 0; _text.style.right = 0; _text.style.top = 0; _text.style.bottom = 0;
                _text.style.unityTextAlign = TextAnchor.MiddleCenter;
                Root.Add(_text);
            }

            public void Set(int val, int max)
            {
                float pct = max > 0 ? Mathf.Clamp01((float)val / max) : 0f;
                _fill.style.width = Length.Percent(pct * 100f);
                _text.text = max > 0 ? $"{_label} {val}/{max}" : _label;
            }
        }
    }
}
