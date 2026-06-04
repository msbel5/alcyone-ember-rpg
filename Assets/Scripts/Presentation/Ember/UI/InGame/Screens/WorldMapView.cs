using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Presentation.Ember.UI.InGame;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class WorldMapView
    {
        private readonly VisualElement _overlay;

        public WorldMapView(VisualElement stageCanvas, Action onClose)
        {
            _overlay = IgModal.Build("World Map", true, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Row;

            var selected = IgMockData.WorldLocations[0];
            content.Add(BuildMapArea(selected));
            content.Add(BuildDetailPane(selected));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildMapArea(WorldLocationData selected)
        {
            var area = new VisualElement();
            area.style.flexGrow = 1;
            area.style.position = Position.Relative;
            area.style.overflow = Overflow.Hidden;
            area.style.backgroundColor = WorldSea;

            AddLand(area, 28, 44, 30, 34, Alpha(WorldLandA, 0.85f));
            AddLand(area, 66, 60, 24, 28, Alpha(WorldLandB, 0.72f));
            AddLand(area, 50, 28, 18, 22, Alpha(WorldLandC, 0.50f));
            AddLand(area, 78, 24, 14, 16, Alpha(WorldLandD, 0.42f));

            for (int x = 0; x < 100; x += 6)
            {
                var line = new VisualElement();
                line.style.position = Position.Absolute;
                line.style.left = Length.Percent(x);
                line.style.top = 0;
                line.style.bottom = 0;
                line.style.width = 1;
                line.style.backgroundColor = WA(0.03f);
                area.Add(line);
            }
            for (int y = 0; y < 100; y += 6)
            {
                var line = new VisualElement();
                line.style.position = Position.Absolute;
                line.style.left = 0;
                line.style.right = 0;
                line.style.top = Length.Percent(y);
                line.style.height = 1;
                line.style.backgroundColor = WA(0.03f);
                area.Add(line);
            }

            for (int i = 0; i < IgMockData.WorldLocations.Length; i++)
            {
                var loc = IgMockData.WorldLocations[i];
                bool isSelected = loc.Id == selected.Id;
                var pin = new VisualElement();
                pin.style.position = Position.Absolute;
                pin.style.left = Length.Percent(loc.XPercent);
                pin.style.top = Length.Percent(loc.YPercent);
                pin.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
                pin.style.alignItems = Align.Center;

                var dot = new VisualElement();
                float size = isSelected ? 22 : 14;
                dot.style.width = size;
                dot.style.height = size;
                dot.style.backgroundColor = isSelected ? LocationColor(loc.Type) : Alpha(LocationColor(loc.Type), loc.Visited ? 0.53f : 0.20f);
                Border(dot, LocationColor(loc.Type), isSelected ? 2 : 1);
                Radius(dot, 999);
                pin.Add(dot);

                if (isSelected || loc.Visited)
                {
                    var label = Text(loc.Name, Sans, 10, LocationColor(loc.Type), FontStyle.Bold);
                    label.style.letterSpacing = 0.6f;
                    label.style.marginTop = 3;
                    pin.Add(label);
                }

                area.Add(pin);
            }

            var player = Text("◉", Serif, 16, Gold);
            player.style.position = Position.Absolute;
            player.style.left = Length.Percent(38);
            player.style.top = Length.Percent(58);
            area.Add(player);
            return area;
        }

        private static VisualElement BuildDetailPane(WorldLocationData selected)
        {
            var pane = new ScrollView();
            pane.style.width = 260;
            pane.style.flexShrink = 0;
            pane.style.borderLeftWidth = 1;
            pane.style.borderLeftColor = PA(0.10f);
            pane.style.paddingTop = 18;
            pane.style.paddingBottom = 18;
            pane.style.paddingLeft = 16;
            pane.style.paddingRight = 16;

            pane.Add(Text(selected.Name, Serif, 16, Parch));
            var kind = Text($"{LocationIcon(selected.Type)} {selected.Type}".ToUpperInvariant(), Sans, 11, LocationColor(selected.Type));
            kind.style.letterSpacing = 1f;
            kind.style.marginTop = 4;
            kind.style.marginBottom = 14;
            pane.Add(kind);
            var copy = Text(selected.Visited ? "You have been here before." : "The path to here is unknown.", Serif, 14, PA(0.55f), FontStyle.Italic);
            copy.style.marginBottom = 18;
            pane.Add(copy);

            if (selected.Visited)
                pane.Add(BuildButton("FAST TRAVEL", true));
            else
                pane.Add(Text("Explore to unlock fast travel", Sans, 11, PA(0.28f)));

            var head = Text("LOCATIONS", Sans, 10, Gold, FontStyle.Bold);
            head.style.letterSpacing = 1.8f;
            head.style.marginTop = 20;
            head.style.marginBottom = 14;
            pane.Add(head);
            for (int i = 0; i < IgMockData.WorldLocations.Length; i++)
            {
                var row = Row();
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 6;
                row.style.paddingBottom = 6;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = PA(0.07f);
                row.Add(Text(LocationIcon(IgMockData.WorldLocations[i].Type), Sans, 12, LocationColor(IgMockData.WorldLocations[i].Type)));
                var name = Text(IgMockData.WorldLocations[i].Name, Sans, 12, IgMockData.WorldLocations[i].Visited ? ParchDim : PA(0.28f));
                name.style.marginLeft = 8;
                row.Add(name);
                pane.Add(row);
            }

            return pane;
        }

        private static void AddLand(VisualElement root, float x, float y, float w, float h, Color color)
        {
            var land = new VisualElement();
            land.style.position = Position.Absolute;
            land.style.left = Length.Percent(x);
            land.style.top = Length.Percent(y);
            land.style.width = Length.Percent(w);
            land.style.height = Length.Percent(h);
            land.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            land.style.backgroundColor = color;
            land.style.borderTopLeftRadius = 220;
            land.style.borderTopRightRadius = 220;
            land.style.borderBottomLeftRadius = 220;
            land.style.borderBottomRightRadius = 220;
            root.Add(land);
        }

        private static Button BuildButton(string text, bool active)
        {
            var button = new Button { text = text };
            ResetButton(button);
            button.style.height = 36;
            button.style.backgroundColor = active ? Gold : Alpha(Panel, 0.62f);
            button.style.color = active ? Ink : Parch;
            Border(button, active ? Amber : PA(0.18f), 1);
            Radius(button, 7);
            button.style.fontSize = 12;
            button.style.letterSpacing = 0.8f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            ApplyFont(button, Sans);
            return button;
        }

        private static string LocationIcon(string type)
        {
            switch (type)
            {
                case "Town": return "⊙";
                case "Road": return "—";
                case "Dungeon": return "▼";
                case "Camp": return "▲";
                case "City": return "◉";
                default: return "◈";
            }
        }

        private static Color LocationColor(string type)
        {
            switch (type)
            {
                case "Town": return Parch;
                case "Road": return ParchDim;
                case "Dungeon": return Health;
                case "Camp": return Amber;
                case "City": return Gold;
                default: return Violet;
            }
        }
    }
}
