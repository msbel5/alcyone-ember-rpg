using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Overland;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI.InGame;
using EmberCrpg.Simulation.Overland;
using UnityEngine;
using UnityEngine.UIElements;
using static EmberCrpg.Presentation.Ember.UI.InGame.IgDesign;

namespace EmberCrpg.Presentation.Ember.UI.InGame.Screens
{
    public sealed class WorldMapView
    {
        private readonly VisualElement _overlay;

        public WorldMapView(VisualElement stageCanvas, Action onClose, Action<string> onFastTravel = null)
        {
            _overlay = IgModal.Build("World Map", true, () => { Close(); onClose?.Invoke(); }, out var content);
            content.style.flexDirection = FlexDirection.Row;

            var world = EmberDomainAdapterLocator.WorldViewReadModel;
            var map = world != null ? world.Overland : null;
            if (map == null)
            {
                content.Add(EmptyState(
                    "Overland Unavailable",
                    "The overland has not been generated yet.",
                    "No live world-map read model is available in this scene."));
                stageCanvas.Add(_overlay);
                return;
            }

            var playerTile = world.PlayerOverlandTile;
            var texture = CreateMapTexture(map);
            var locations = BuildLocations(map, playerTile, world.StartingSettlementName);
            var selected = ChooseSelectedLocation(map, playerTile, world.StartingSettlementName, locations);

            content.Add(BuildMapArea(map, playerTile, selected, locations, texture));
            content.Add(BuildDetailPane(map, playerTile, selected, locations, onFastTravel));

            stageCanvas.Add(_overlay);
        }

        public void Close() { _overlay?.RemoveFromHierarchy(); }

        private static VisualElement BuildMapArea(
            OverlandMap map,
            GridPosition playerTile,
            MapLocationData selected,
            IReadOnlyList<MapLocationData> locations,
            Texture2D texture)
        {
            var area = new VisualElement();
            area.style.flexGrow = 1;
            area.style.position = Position.Relative;
            area.style.overflow = Overflow.Hidden;
            area.style.backgroundColor = WorldSea;

            var mapFrame = new VisualElement();
            mapFrame.style.position = Position.Absolute;
            mapFrame.style.overflow = Overflow.Hidden;
            mapFrame.style.backgroundColor = WorldSea;
            Radius(mapFrame, 12);
            area.Add(mapFrame);

            if (texture != null)
            {
                var image = new Image();
                image.image = texture;
                image.scaleMode = ScaleMode.StretchToFill;
                image.style.position = Position.Absolute;
                image.style.left = 0;
                image.style.right = 0;
                image.style.top = 0;
                image.style.bottom = 0;
                mapFrame.Add(image);
            }

            AddGrid(mapFrame);

            for (int i = 0; i < locations.Count; i++)
            {
                var loc = locations[i];
                bool isSelected = ReferenceEquals(loc, selected);
                AddLocationPin(mapFrame, loc, isSelected);
            }

            var player = new VisualElement();
            player.style.position = Position.Absolute;
            player.style.left = Length.Percent(ToPercent(playerTile.X, map.Width));
            player.style.top = Length.Percent(ToPercent(playerTile.Y, map.Height));
            player.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            player.style.width = 28;
            player.style.height = 28;
            Border(player, WA(0.92f), 2);
            Radius(player, 999);
            player.style.backgroundColor = Color.clear;
            mapFrame.Add(player);

            area.RegisterCallback<GeometryChangedEvent>(_ => FitMapFrame(area, mapFrame, map.Width, map.Height));
            area.schedule.Execute(() => FitMapFrame(area, mapFrame, map.Width, map.Height)).StartingIn(0);

            var summary = new VisualElement();
            summary.style.position = Position.Absolute;
            summary.style.left = 16;
            summary.style.bottom = 16;
            summary.style.paddingLeft = 12;
            summary.style.paddingRight = 12;
            summary.style.paddingTop = 8;
            summary.style.paddingBottom = 8;
            summary.style.backgroundColor = Alpha(VoidWarm, 0.72f);
            Border(summary, PA(0.12f), 1);
            Radius(summary, 10);
            summary.Add(Text(
                map.Width + " × " + map.Height + " tiles · " + CountRegions(map) + " regions · " + map.Settlements.Count + " settlements",
                Sans,
                11,
                ParchDim));
            area.Add(summary);

            return area;
        }

        private static ScrollView BuildDetailPane(
            OverlandMap map,
            GridPosition playerTile,
            MapLocationData selected,
            IReadOnlyList<MapLocationData> locations,
            Action<string> onFastTravel)
        {
            var pane = new ScrollView();
            pane.style.width = 300;
            pane.style.flexShrink = 0;
            pane.style.borderLeftWidth = 1;
            pane.style.borderLeftColor = PA(0.10f);
            pane.style.paddingTop = 18;
            pane.style.paddingBottom = 18;
            pane.style.paddingLeft = 16;
            pane.style.paddingRight = 16;

            var currentRegion = TryGetRegionText(map, playerTile);
            pane.Add(Text(selected != null ? selected.Name : "World Atlas", Serif, 18, Parch, FontStyle.Bold));

            var kind = Text(
                selected != null ? (LocationIcon(selected.Kind) + " " + selected.Kind.ToString()).ToUpperInvariant() : "OVERLAND",
                Sans,
                11,
                selected != null ? LocationColor(selected.Kind) : Gold);
            kind.style.letterSpacing = 1f;
            kind.style.marginTop = 4;
            pane.Add(kind);

            var copy = Text(
                selected != null
                    ? "Current overland projection. Click a settlement to travel — the world re-realizes there."
                    : "The atlas is live, but no settlements are projected onto it yet.",
                Serif,
                14,
                PA(0.55f),
                FontStyle.Italic);
            copy.style.whiteSpace = WhiteSpace.Normal;
            copy.style.marginTop = 10;
            copy.style.marginBottom = 18;
            pane.Add(copy);

            pane.Add(BuildFact("Player Tile", playerTile.X + ", " + playerTile.Y));
            pane.Add(BuildFact("Current Region", currentRegion ?? "Unknown"));
            if (selected != null)
            {
                pane.Add(BuildFact("Settlement Tile", selected.Tile.X + ", " + selected.Tile.Y));
                pane.Add(BuildFact("Settlement Region", selected.RegionText));
                pane.Add(BuildFact("Distance", selected.DistanceFromPlayer + " tiles from the player"));
                if (!string.IsNullOrWhiteSpace(selected.TemplatePackTag))
                    pane.Add(BuildFact("Template Pack", selected.TemplatePackTag));
            }

            if (onFastTravel != null && selected != null && !selected.IsCurrent)
            {
                string target = selected.Name;
                var travelButton = new Button(() => onFastTravel(target)) { text = "TRAVEL TO " + target.ToUpperInvariant() };
                travelButton.style.marginTop = 18;
                travelButton.style.marginBottom = 18;
                travelButton.style.paddingTop = 10;
                travelButton.style.paddingBottom = 10;
                travelButton.style.backgroundColor = GA(0.14f);
                travelButton.style.color = Gold;
                travelButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                Border(travelButton, Gold, 1);
                Radius(travelButton, 9);
                pane.Add(travelButton);
            }
            else
            {
                var travelNote = Text(
                    onFastTravel == null
                        ? "Fast travel is unavailable from this screen."
                        : "You are here. Click another settlement to travel.",
                    Sans,
                    11,
                    PA(0.34f));
                travelNote.style.whiteSpace = WhiteSpace.Normal;
                travelNote.style.marginTop = 18;
                travelNote.style.marginBottom = 18;
                pane.Add(travelNote);
            }

            // Legend: the pin colors finally explain themselves ("dots are meaningless" playtest feedback).
            var legendHead = Text("LEGEND", Sans, 10, Gold, FontStyle.Bold);
            legendHead.style.letterSpacing = 1.8f;
            legendHead.style.marginBottom = 6;
            pane.Add(legendHead);

            var legend = Row();
            legend.style.flexWrap = Wrap.Wrap;
            legend.style.marginBottom = 14;
            foreach (var settlementKind in new[]
            {
                SettlementKind.City, SettlementKind.Town, SettlementKind.Village, SettlementKind.Hamlet,
                SettlementKind.Inn, SettlementKind.Shrine, SettlementKind.Dungeon,
            })
            {
                var item = Text(LocationIcon(settlementKind) + " " + settlementKind, Sans, 9, LocationColor(settlementKind));
                item.style.marginRight = 10;
                item.style.marginBottom = 4;
                legend.Add(item);
            }
            var youItem = Text("● You", Sans, 9, Gold);
            youItem.style.marginBottom = 4;
            legend.Add(youItem);
            pane.Add(legend);

            var head = Text("SETTLEMENTS", Sans, 10, Gold, FontStyle.Bold);
            head.style.letterSpacing = 1.8f;
            head.style.marginBottom = 14;
            pane.Add(head);

            if (locations.Count == 0)
            {
                pane.Add(EmptyState(
                    "No Settlements",
                    "No settlements are projected onto this overland yet.",
                    "The map background is live, but there are no settlement nodes to list."));
                return pane;
            }

            for (int i = 0; i < locations.Count; i++)
                pane.Add(BuildLocationRow(locations[i], ReferenceEquals(locations[i], selected), onFastTravel));

            return pane;
        }

        private static VisualElement BuildLocationRow(MapLocationData loc, bool selected, Action<string> onFastTravel)
        {
            var row = new VisualElement();
            row.style.marginBottom = 6;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.backgroundColor = selected ? GA(0.10f) : Dark(0.55f);
            Border(row, selected ? Gold : PA(0.10f), selected ? 2 : 1);
            Radius(row, 9);

            // Clicking any non-current settlement row IS the travel action (no second selection pass needed).
            if (onFastTravel != null && !loc.IsCurrent)
            {
                string target = loc.Name;
                row.RegisterCallback<ClickEvent>(_ => onFastTravel(target));
            }

            var top = Row();
            top.style.alignItems = Align.Center;
            top.Add(Text(LocationIcon(loc.Kind), Sans, 12, LocationColor(loc.Kind)));
            var name = Text(loc.Name, Sans, 12, selected ? Parch : ParchDim, FontStyle.Bold);
            name.style.marginLeft = 8;
            top.Add(name);
            if (loc.IsCurrent)
            {
                var current = Text("CURRENT", Sans, 10, Gold, FontStyle.Bold);
                current.style.marginLeft = StyleKeyword.Auto;
                current.style.letterSpacing = 0.8f;
                top.Add(current);
            }
            row.Add(top);

            var region = Text(loc.RegionText + " · " + loc.DistanceFromPlayer + " tiles", Sans, 10, PA(0.38f));
            region.style.marginTop = 4;
            row.Add(region);
            return row;
        }

        private static void AddLocationPin(VisualElement mapFrame, MapLocationData loc, bool isSelected)
        {
            var pin = new VisualElement();
            pin.style.position = Position.Absolute;
            pin.style.left = Length.Percent(loc.XPercent);
            pin.style.top = Length.Percent(loc.YPercent);
            pin.style.width = 0;
            pin.style.height = 0;
            mapFrame.Add(pin);

            // F9 ("zindanı bulamadım"): dungeon pins must read at a glance — bigger, full-strength red,
            // and ALWAYS labeled with the ▼ glyph (other pins only label when current/selected).
            bool isDungeon = loc.Kind == SettlementKind.Dungeon;
            float size = loc.IsCurrent ? 20f : (isSelected ? 16f : (isDungeon ? 16f : 12f));
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.left = -size * 0.5f;
            dot.style.top = -size * 0.5f;
            dot.style.width = size;
            dot.style.height = size;
            dot.style.backgroundColor = loc.IsCurrent ? Gold : Alpha(LocationColor(loc.Kind), isDungeon ? 0.88f : 0.56f);
            Border(dot, loc.IsCurrent ? Amber : LocationColor(loc.Kind), loc.IsCurrent || isDungeon ? 2 : 1);
            Radius(dot, 999);
            pin.Add(dot);

            if (!loc.IsCurrent && !isSelected && !isDungeon)
                return;

            var label = Text(
                isDungeon && !loc.IsCurrent ? "▼ " + loc.Name : loc.Name,
                Sans, 10,
                loc.IsCurrent ? Gold : (isDungeon ? LocationColor(SettlementKind.Dungeon) : Parch),
                FontStyle.Bold);
            label.style.position = Position.Absolute;
            label.style.top = (size * 0.5f) + 4f;
            label.style.left = -90;
            label.style.width = 180;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.letterSpacing = 0.6f;
            pin.Add(label);
        }

        private static VisualElement BuildFact(string label, string value)
        {
            var wrap = new VisualElement();
            wrap.style.marginBottom = 10;

            var head = Text(label.ToUpperInvariant(), Sans, 10, Gold, FontStyle.Bold);
            head.style.letterSpacing = 1f;
            wrap.Add(head);

            var body = Text(value, Sans, 12, ParchDim);
            body.style.marginTop = 2;
            body.style.whiteSpace = WhiteSpace.Normal;
            wrap.Add(body);
            return wrap;
        }

        private static void AddGrid(VisualElement area)
        {
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
        }

        private static Texture2D CreateMapTexture(OverlandMap map)
        {
            if (map == null) return null;
            // Authoritative in-game map. Prefer the rich planet-sourced render (organic icosphere sampling —
            // the pre-projection look) and fall back to the blocky overland raster only for legacy worlds.
            // Both share the SAME equirect tile-space as the markers, so dots/ring cannot drift.
            byte[] rgba;
            int width, height;
            FilterMode filter;
            if (PlanetAtlas.TryRender(map, 1024, 512, out var planetImage))
            {
                rgba = planetImage.Rgba;
                width = planetImage.Width;
                height = planetImage.Height;
                filter = FilterMode.Bilinear; // organic planet render reads as a painting, not a tile grid
            }
            else
            {
                var image = OverlandMapImageSampler.Sample(map, 1024, 512);
                rgba = image.RgbaBytes;
                width = image.Width;
                height = image.Height;
                filter = FilterMode.Point; // crisp tiles so coastal land reads clearly under the markers
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                name = "InGameWorldMap"
            };
            texture.LoadRawTextureData(rgba);
            texture.Apply(false, true);
            return texture;
        }

        private static MapLocationData[] BuildLocations(OverlandMap map, GridPosition playerTile, string startingSettlementName)
        {
            var locations = new MapLocationData[map.Settlements.Count];
            for (int i = 0; i < map.Settlements.Count; i++)
            {
                var settlement = map.Settlements[i];
                locations[i] = new MapLocationData(
                    settlement.Id.Value.ToString(),
                    settlement.Name,
                    settlement.Kind,
                    settlement.TilePosition,
                    TryGetRegionText(map, settlement.TilePosition) ?? "Unknown Region",
                    settlement.TemplatePackTag,
                    string.Equals(settlement.Name, startingSettlementName, StringComparison.Ordinal),
                    AnchorXPercent(map, settlement.TilePosition),
                    AnchorYPercent(map, settlement.TilePosition),
                    OverlandMap.ChebyshevDistance(playerTile, settlement.TilePosition));
            }

            Array.Sort(locations, CompareLocations);
            return locations;
        }

        private static MapLocationData ChooseSelectedLocation(
            OverlandMap map,
            GridPosition playerTile,
            string startingSettlementName,
            IReadOnlyList<MapLocationData> locations)
        {
            if (locations == null || locations.Count == 0) return null;

            if (!string.IsNullOrWhiteSpace(startingSettlementName))
            {
                for (int i = 0; i < locations.Count; i++)
                    if (string.Equals(locations[i].Name, startingSettlementName, StringComparison.Ordinal))
                        return locations[i];
            }

            int bestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < locations.Count; i++)
            {
                int distance = OverlandMap.ChebyshevDistance(playerTile, locations[i].Tile);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return locations[bestIndex];
        }

        private static int CountRegions(OverlandMap map)
        {
            var ids = new HashSet<ulong>();
            for (int i = 0; i < map.Tiles.Count; i++)
                ids.Add(map.Tiles[i].RegionId.Value);
            return ids.Count;
        }

        private static string TryGetRegionText(OverlandMap map, GridPosition tile)
        {
            if (map == null) return null;
            if (!map.TryGetTile(tile.X, tile.Y, out var regionTile) || regionTile == null) return null;
            return "Region " + regionTile.RegionId.Value;
        }

        private static float ToPercent(int coordinate, int size)
        {
            if (size <= 0) return 50f;
            return OverlandMapProjection.TileCenterPercent(coordinate, size);
        }

        // ONE map truth: pins anchor to the icosphere tile their overland cell maps to (PlanetAtlas), so a
        // dot can never sit on a neighbouring ocean tile's pixels. Cell-centre fallback for legacy worlds.
        private static float AnchorXPercent(OverlandMap map, GridPosition tile)
            => PlanetAtlas.TryGetTileAnchorPercent(map, tile.X, tile.Y, out var x, out _) ? x : ToPercent(tile.X, map.Width);

        private static float AnchorYPercent(OverlandMap map, GridPosition tile)
            => PlanetAtlas.TryGetTileAnchorPercent(map, tile.X, tile.Y, out _, out var y) ? y : ToPercent(tile.Y, map.Height);

        private static void FitMapFrame(VisualElement area, VisualElement frame, int mapWidth, int mapHeight)
        {
            if (area == null || frame == null || mapWidth <= 0 || mapHeight <= 0) return;

            var bounds = area.contentRect;
            if (bounds.width <= 1f || bounds.height <= 1f) return;

            float aspect = mapWidth / (float)mapHeight;
            float fittedWidth = bounds.width;
            float fittedHeight = fittedWidth / aspect;

            if (fittedHeight > bounds.height)
            {
                fittedHeight = bounds.height;
                fittedWidth = fittedHeight * aspect;
            }

            frame.style.width = fittedWidth;
            frame.style.height = fittedHeight;
            frame.style.left = (bounds.width - fittedWidth) * 0.5f;
            frame.style.top = (bounds.height - fittedHeight) * 0.5f;
        }

        private static int CompareLocations(MapLocationData a, MapLocationData b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            if (a.IsCurrent != b.IsCurrent) return a.IsCurrent ? -1 : 1;
            // F9: dungeons surface right after the current settlement so the list answers
            // "where can I delve?" without scrolling the whole atlas.
            bool ad = a.Kind == SettlementKind.Dungeon, bd = b.Kind == SettlementKind.Dungeon;
            if (ad != bd) return ad ? -1 : 1;
            return string.CompareOrdinal(a.Name, b.Name);
        }

        private static string LocationIcon(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.City: return "◉";
                case SettlementKind.Town: return "⊙";
                case SettlementKind.Village: return "◌";
                case SettlementKind.Hamlet: return "•";
                case SettlementKind.Inn: return "⌂";
                case SettlementKind.Shrine: return "✦";
                case SettlementKind.Dungeon: return "▼";
                default: return "◈";
            }
        }

        private static Color LocationColor(SettlementKind kind)
        {
            switch (kind)
            {
                case SettlementKind.City: return Gold;
                case SettlementKind.Town: return Parch;
                case SettlementKind.Village: return ParchDim;
                case SettlementKind.Hamlet: return WA(0.76f);
                case SettlementKind.Inn: return Orange;
                case SettlementKind.Shrine: return Violet;
                case SettlementKind.Dungeon: return Health;
                default: return Amber;
            }
        }

        private sealed class MapLocationData
        {
            public MapLocationData(
                string id,
                string name,
                SettlementKind kind,
                GridPosition tile,
                string regionText,
                string templatePackTag,
                bool isCurrent,
                float xPercent,
                float yPercent,
                int distanceFromPlayer)
            {
                Id = id;
                Name = name;
                Kind = kind;
                Tile = tile;
                RegionText = regionText;
                TemplatePackTag = templatePackTag;
                IsCurrent = isCurrent;
                XPercent = xPercent;
                YPercent = yPercent;
                DistanceFromPlayer = distanceFromPlayer;
            }

            public string Id { get; }
            public string Name { get; }
            public SettlementKind Kind { get; }
            public GridPosition Tile { get; }
            public string RegionText { get; }
            public string TemplatePackTag { get; }
            public bool IsCurrent { get; }
            public float XPercent { get; }
            public float YPercent { get; }
            public int DistanceFromPlayer { get; }
        }
    }
}
