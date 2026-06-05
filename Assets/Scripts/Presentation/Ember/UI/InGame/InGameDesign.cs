using UnityEngine;
using UnityEngine.UIElements;

namespace EmberCrpg.Presentation.Ember.UI.InGame
{
    /// <summary>
    /// Shared design language for the in-game UI (World HUD + the 16 modal screens), ported from the Claude
    /// Design handoff (Downloads/ingame-ui · ig-ds.jsx). Same tokens + fonts as the character-creation redesign,
    /// extracted here so every in-game view (and the parallel Codex-built screens) share one source of truth.
    /// All values are the design's exact px / colours so the screens map 1:1 inside the self-scaling 1920×1080
    /// stage (see <see cref="InGameStage"/>).
    /// </summary>
    public static class IgDesign
    {
        // ── palette (ig-ds.jsx G) ─────────────────────────────────────────────────────────────────────────
        public static Color C(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);
        public static readonly Color VoidWarm = C(10, 9, 8);     // #0A0908
        public static readonly Color Panel    = C(46, 36, 23);   // #2E2417
        public static readonly Color InputBg    = C(31, 26, 20);   // #1F1A14
        public static readonly Color Gold      = C(255, 217, 76); // #FFD94C
        public static readonly Color Amber     = C(241, 196, 15); // #F1C40F
        public static readonly Color Parch     = C(242, 219, 158);// #F2DB9E
        public static readonly Color ParchDim  = C(230, 217, 179);// #E6D9B3
        public static readonly Color Ink       = C(38, 26, 13);   // #261A0D
        public static readonly Color Bone      = C(255, 255, 255);
        public static readonly Color BoneDim   = C(255, 255, 255, 0.36f);
        public static readonly Color Health    = C(217, 51, 31);  // #D9331F
        public static readonly Color Fatigue   = C(217, 179, 26); // #D9B31A
        public static readonly Color Mana      = C(51, 115, 242); // #3373F2
        public static readonly Color Success   = C(61, 158, 88);  // #3D9E58
        public static readonly Color Orange    = C(240, 168, 32); // #F0A820
        public static readonly Color Violet    = C(139, 92, 246); // #8B5CF6
        public static readonly Color EmberGlow     = C(255, 107, 53); // #FF6B35
        public static readonly Color WorldSea  = C(14, 42, 61);   // #0E2A3D
        public static readonly Color WorldLandA = C(35, 110, 60);
        public static readonly Color WorldLandB = C(38, 95, 50);
        public static readonly Color WorldLandC = C(42, 105, 55);
        public static readonly Color WorldLandD = C(40, 95, 52);
        public static Color PA(float a) => C(242, 219, 158, a);   // parchment α
        public static Color WA(float a) => C(255, 255, 255, a);   // white α
        public static Color GA(float a) => C(255, 217, 76, a);    // gold α
        public static Color Dark(float a) => C(20, 16, 10, a);    // tile base
        public static Color Alpha(Color color, float a) => new Color(color.r, color.g, color.b, a);

        public static Color Stat(string abbr)
        {
            switch (abbr)
            {
                case "MIG": return C(217, 51, 31);
                case "AGI": return Orange;
                case "END": return Success;
                case "MND": return C(51, 115, 242);
                case "INS": return Violet;
                case "PRE": return C(255, 217, 76);
                default: return Gold;
            }
        }

        public static Color School(string school)
        {
            switch (school)
            {
                case "Destruction": return Health;
                case "Restoration": return Success;
                case "Illusion": return Violet;
                case "Conjuration": return Orange;
                case "Mysticism": return Mana;
                case "Alteration": return Gold;
                default: return Gold;
            }
        }

        // ── fonts (Jost UI, Spectral serif; Cinzel → Spectral fallback) ───────────────────────────────────
        private static Font _jost, _spectral;
        private static bool _loaded;
        private static void Ensure()
        {
            if (_loaded) return;
            _loaded = true;
            _jost = Resources.Load<Font>("Fonts/Jost");
            _spectral = Resources.Load<Font>("Fonts/Spectral-Regular");
        }
        public static Font Sans  { get { Ensure(); return _jost; } }       // ig G.f
        public static Font Serif { get { Ensure(); return _spectral; } }   // ig G.fn / G.fe

        // ── element helpers ───────────────────────────────────────────────────────────────────────────────
        public static Label Text(string text, Font font, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var l = new Label(text);
            l.style.fontSize = size; l.style.color = color;
            l.style.unityFontStyleAndWeight = style;
            l.style.marginTop = 0; l.style.marginBottom = 0; l.style.marginLeft = 0; l.style.marginRight = 0;
            l.style.paddingTop = 0; l.style.paddingBottom = 0;
            ApplyFont(l, font);
            return l;
        }

        public static VisualElement Row()
        {
            var e = new VisualElement(); e.style.flexDirection = FlexDirection.Row;
            return e;
        }

        public static void Radius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        public static void Border(VisualElement e, Color color, float width)
        {
            e.style.borderTopWidth = width; e.style.borderBottomWidth = width;
            e.style.borderLeftWidth = width; e.style.borderRightWidth = width;
            e.style.borderTopColor = color; e.style.borderBottomColor = color;
            e.style.borderLeftColor = color; e.style.borderRightColor = color;
        }

        public static void ResetButton(Button b)
        {
            b.style.marginTop = 0; b.style.marginBottom = 0; b.style.marginLeft = 0; b.style.marginRight = 0;
            Radius(b, 0); Border(b, Color.clear, 0);
            b.style.backgroundColor = Color.clear;
            b.style.paddingTop = 0; b.style.paddingBottom = 0; b.style.paddingLeft = 0; b.style.paddingRight = 0;
        }

        public static void ApplyFont(VisualElement e, Font font)
        {
            if (font == null) return;
            e.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
        }

        public static void StyleScroll(ScrollView sv)
        {
            if (sv == null) return;
            if (sv.contentViewport != null)
                sv.contentViewport.style.paddingRight = 12;
            StyleScroller(sv.verticalScroller, true);
            StyleScroller(sv.horizontalScroller, false);
        }

        public static VisualElement EmptyState(string eyebrow, string message, string detail = null)
        {
            var wrap = new VisualElement();
            wrap.style.flexGrow = 1;
            wrap.style.alignItems = Align.Center;
            wrap.style.justifyContent = Justify.Center;
            wrap.style.paddingLeft = 32;
            wrap.style.paddingRight = 32;
            wrap.style.paddingTop = 32;
            wrap.style.paddingBottom = 32;

            var card = new VisualElement();
            card.style.maxWidth = 620;
            card.style.paddingLeft = 28;
            card.style.paddingRight = 28;
            card.style.paddingTop = 24;
            card.style.paddingBottom = 24;
            card.style.backgroundColor = Alpha(InputBg, 0.88f);
            Border(card, PA(0.12f), 1);
            Radius(card, 14);
            card.style.alignItems = Align.Center;
            wrap.Add(card);

            if (!string.IsNullOrWhiteSpace(eyebrow))
            {
                var top = Text(eyebrow.ToUpperInvariant(), Sans, 10, Gold, FontStyle.Bold);
                top.style.letterSpacing = 2f;
                top.style.unityTextAlign = TextAnchor.MiddleCenter;
                top.style.marginBottom = 10;
                card.Add(top);
            }

            var body = Text(message ?? string.Empty, Serif, 20, Parch, FontStyle.Italic);
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(body);

            if (!string.IsNullOrWhiteSpace(detail))
            {
                var note = Text(detail, Sans, 12, PA(0.42f));
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.unityTextAlign = TextAnchor.MiddleCenter;
                note.style.marginTop = 12;
                card.Add(note);
            }

            return wrap;
        }

        private static void StyleScroller(Scroller scroller, bool vertical)
        {
            if (scroller == null) return;

            scroller.lowButton.style.display = DisplayStyle.None;
            scroller.highButton.style.display = DisplayStyle.None;
            scroller.style.backgroundColor = Color.clear;
            scroller.style.borderLeftWidth = 0;
            scroller.style.borderRightWidth = 0;
            scroller.style.borderTopWidth = 0;
            scroller.style.borderBottomWidth = 0;
            scroller.style.position = Position.Absolute;
            scroller.style.paddingLeft = 0;
            scroller.style.paddingRight = 0;
            scroller.style.paddingTop = 0;
            scroller.style.paddingBottom = 0;

            if (vertical)
            {
                scroller.style.width = 6;
                scroller.style.minWidth = 6;
                scroller.style.maxWidth = 6;
                scroller.style.top = 4;
                scroller.style.bottom = 4;
                scroller.style.right = 4;
                scroller.style.left = StyleKeyword.Auto;
            }
            else
            {
                scroller.style.height = 6;
                scroller.style.minHeight = 6;
                scroller.style.maxHeight = 6;
                scroller.style.left = 4;
                scroller.style.right = 4;
                scroller.style.bottom = 4;
                scroller.style.top = StyleKeyword.Auto;
            }

            var slider = scroller.slider;
            if (slider != null)
            {
                slider.style.backgroundColor = Alpha(Ink, 0.12f);
                slider.style.borderLeftWidth = 0;
                slider.style.borderRightWidth = 0;
                slider.style.borderTopWidth = 0;
                slider.style.borderBottomWidth = 0;
                Radius(slider, 999);

                if (vertical)
                {
                    slider.style.minWidth = 6;
                    slider.style.maxWidth = 6;
                }
                else
                {
                    slider.style.minHeight = 6;
                    slider.style.maxHeight = 6;
                }
            }

            var tracker = scroller.Q("unity-tracker");
            if (tracker != null)
            {
                tracker.style.backgroundColor = Color.clear;
                tracker.style.borderLeftWidth = 0;
                tracker.style.borderRightWidth = 0;
                tracker.style.borderTopWidth = 0;
                tracker.style.borderBottomWidth = 0;
                Radius(tracker, 999);
            }

            var dragger = scroller.Q("unity-dragger");
            if (dragger == null) return;

            dragger.style.backgroundColor = Alpha(Gold, 0.45f);
            dragger.style.borderLeftWidth = 0;
            dragger.style.borderRightWidth = 0;
            dragger.style.borderTopWidth = 0;
            dragger.style.borderBottomWidth = 0;
            Radius(dragger, 999);

            if (vertical)
            {
                dragger.style.minWidth = 6;
                dragger.style.maxWidth = 6;
            }
            else
            {
                dragger.style.minHeight = 6;
                dragger.style.maxHeight = 6;
            }

            var draggerBorder = scroller.Q("unity-dragger-border");
            if (draggerBorder != null)
            {
                draggerBorder.style.backgroundColor = Color.clear;
                draggerBorder.style.borderLeftWidth = 0;
                draggerBorder.style.borderRightWidth = 0;
                draggerBorder.style.borderTopWidth = 0;
                draggerBorder.style.borderBottomWidth = 0;
            }

            var draggerAnchor = scroller.Q("unity-dragger-anchor");
            if (draggerAnchor != null)
            {
                draggerAnchor.style.backgroundColor = Color.clear;
                draggerAnchor.style.borderLeftWidth = 0;
                draggerAnchor.style.borderRightWidth = 0;
                draggerAnchor.style.borderTopWidth = 0;
                draggerAnchor.style.borderBottomWidth = 0;
            }
        }
    }

    /// <summary>
    /// A 1920×1080 design stage that scales to fit the panel (the same trick the char-creation redesign uses),
    /// so every in-game view authors at design px and scales uniformly. World-layer views (HUD) live directly on
    /// it; modal views overlay it. One per in-game UI root.
    /// </summary>
    public sealed class InGameStage
    {
        public readonly VisualElement Canvas;   // the 1920×1080 design surface; add views here

        public InGameStage(VisualElement root)
        {
            Canvas = new VisualElement { name = "InGameStage" };
            var s = Canvas.style;
            s.position = Position.Absolute;
            s.width = 1920; s.height = 1080;
            s.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50), 0f);
            // The HUD must NOT eat clicks on the empty world; children re-enable picking where needed.
            Canvas.pickingMode = PickingMode.Ignore;
            root.Add(Canvas);
            Fit();
        }

        /// <summary>Scale + centre the 1920×1080 stage to fit the screen. Uses Screen size (always valid, no
        /// layout-timing race) and is called every frame by the controller so it tracks resolution changes.</summary>
        public void Fit()
        {
            float w = Screen.width, h = Screen.height;
            if (w <= 1f || h <= 1f) return;
            float scale = Mathf.Min(w / 1920f, h / 1080f);
            Canvas.style.scale = new Scale(new Vector2(scale, scale));
            Canvas.style.left = (w - 1920f) / 2f;
            Canvas.style.top = (h - 1080f) / 2f;
        }
    }
}
