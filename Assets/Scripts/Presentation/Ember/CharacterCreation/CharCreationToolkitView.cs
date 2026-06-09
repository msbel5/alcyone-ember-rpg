using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EmberCrpg.Ui.Foundation;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    /// <summary>
    /// Redesigned character-creation view — a 1:1 port of the Claude Design handoff in
    /// <c>Reports/cc-design-ref/</c> (cc-ds.jsx + cc-screens-a/b/c.jsx). Every one of the 11 steps is a distinct
    /// hand-built <see cref="VisualElement"/> screen sharing the same chrome (breadcrumb + step title + STEP n/11
    /// + 11-segment progress bar + back/continue footer). Colours, type sizes, spacing and states match the design
    /// tokens exactly; Jost/Spectral fonts are loaded from Resources/Fonts. The controller
    /// (<see cref="CharacterCreationController"/>) drives it with real catalog data + state via the Render* methods.
    /// </summary>
    public sealed class CharCreationToolkitView
    {
        public const int TotalSteps = 11;

        public Action OnBack;
        public Action OnNext;

        // ── design palette (cc-ds.jsx DS, 0-255 → linear Color) ───────────────────────────────────────────────
        private static Color C(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);
        private static readonly Color VoidWarm = C(10, 9, 8);       // #0A0908 page bg
        private static readonly Color Panel    = C(46, 36, 23);     // #2E2417
        private static readonly Color Input    = C(31, 26, 20);     // #1F1A14
        private static readonly Color Gold      = C(255, 217, 76);  // #FFD94C
        private static readonly Color Amber     = C(241, 196, 15);  // #F1C40F
        private static readonly Color Parch     = C(242, 219, 158); // #F2DB9E
        private static readonly Color ParchDim  = C(230, 217, 179); // #E6D9B3
        private static readonly Color Ink       = C(38, 26, 13);    // #261A0D
        private static readonly Color Bone      = C(255, 255, 255); // #FFFFFF
        private static readonly Color Dark      = C(22, 17, 10);    // tile base rgba(22,17,10)
        private static Color PA(float a) => C(242, 219, 158, a);    // parchment α
        private static Color WA(float a) => C(255, 255, 255, a);    // white α
        private static Color GA(float a) => C(255, 217, 76, a);     // gold α
        private static Color DA(float a) => C(22, 17, 10, a);       // dark α
        private static Color PanelA(float a) => C(46, 36, 23, a);
        private static Color CardBg => C(16, 12, 8, 0.72f);

        // stat accent colours (cc-screens-b STAT_COLOR)
        private static Color StatColor(string abbr)
        {
            switch (abbr)
            {
                case "MIG": return C(217, 51, 31);
                case "AGI": return C(240, 168, 32);
                case "END": return C(61, 158, 88);
                case "MND": return C(51, 115, 242);
                case "INS": return C(139, 92, 246);
                case "PRE": return C(255, 217, 76);
                default: return Gold;
            }
        }

        // fonts: Jost (UI/sans, DS.f), Spectral (serif body/italic, DS.fn); Cinzel (DS.fe) → Spectral fallback.
        private static Font _jost, _spectral;
        private static bool _fontsLoaded;
        private static void EnsureFonts()
        {
            if (_fontsLoaded) return;
            _fontsLoaded = true;
            _jost = Resources.Load<Font>("Fonts/Jost");
            _spectral = Resources.Load<Font>("Fonts/Spectral-Regular");
        }
        private static Font Sans => _jost;        // DS.f
        private static Font Serif => _spectral;   // DS.fn / DS.fe (Cinzel falls back to Spectral)

        // ── widgets ───────────────────────────────────────────────────────────────────────────────────────────
        private readonly VisualElement _root;
        private readonly VisualElement _container;
        private readonly Label _breadcrumb;
        private readonly Label _stepTitle;
        private readonly Label _stepCounter;
        private readonly VisualElement _segments;
        private readonly VisualElement _body;
        private readonly VisualElement _footer;
        private readonly Button _backBtn;
        private readonly Button _nextBtn;
        private Action _primaryAction;

        public CharCreationToolkitView(VisualElement root, UiTokens tokens)
        {
            EnsureFonts();

            _root = root;
            // The design is authored on a fixed 1920×1080 "Stage" that scales to fit the window (cc-app.jsx
            // Stage). Replicate that here — a 1920×1080 canvas scaled by min(w/1920, h/1080) and centred — so
            // every design pixel value maps 1:1 and the whole screen scales uniformly. Scoped to THIS view, so
            // it never disturbs the other UI-Toolkit panels' scaling.
            _container = new VisualElement { name = "CharCreationRedesign" };
            var s = _container.style;
            s.position = Position.Absolute;
            s.width = 1920; s.height = 1080;
            s.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50), 0f);
            s.backgroundColor = VoidWarm;
            s.flexDirection = FlexDirection.Column;

            // ── chrome: breadcrumb + (title / step n/11) + progress (padding 26 68 0) ──
            var chrome = new VisualElement();
            chrome.style.paddingTop = 26; chrome.style.paddingLeft = 68; chrome.style.paddingRight = 68;
            chrome.style.flexShrink = 0;

            _breadcrumb = Text("CHARACTER CREATION", Sans, 11, WA(0.26f));
            _breadcrumb.style.letterSpacing = 3.3f;          // 0.30em
            _breadcrumb.style.marginBottom = 15;
            chrome.Add(_breadcrumb);

            var titleRow = Row();
            titleRow.style.justifyContent = Justify.SpaceBetween;
            titleRow.style.alignItems = Align.FlexEnd;
            titleRow.style.marginBottom = 13;
            _stepTitle = Text("Name", Sans, 21, Parch, FontStyle.Bold);
            _stepTitle.style.letterSpacing = 0.5f;
            titleRow.Add(_stepTitle);
            _stepCounter = Text("STEP 1 / 11", Sans, 11, PA(0.32f));
            _stepCounter.style.letterSpacing = 1.1f;
            titleRow.Add(_stepCounter);
            chrome.Add(titleRow);

            _segments = Row();
            _segments.style.height = 3;
            for (int i = 0; i < TotalSteps; i++)
            {
                var seg = new VisualElement();
                seg.style.flexGrow = 1; seg.style.height = 3;
                seg.style.marginRight = i < TotalSteps - 1 ? 3 : 0;
                Radius(seg, 2);
                seg.style.backgroundColor = PA(0.10f);
                _segments.Add(seg);
            }
            chrome.Add(_segments);
            _container.Add(chrome);

            // ── body host (flex 1) ──
            _body = new VisualElement();
            _body.style.flexGrow = 1;
            _body.style.flexDirection = FlexDirection.Column;
            _container.Add(_body);

            // ── footer: back (ghost) + Continue (gold CTA), padding 18 68 30 ──
            _footer = Row();
            _footer.style.justifyContent = Justify.SpaceBetween;
            _footer.style.alignItems = Align.Center;
            _footer.style.paddingTop = 18; _footer.style.paddingBottom = 30;
            _footer.style.paddingLeft = 68; _footer.style.paddingRight = 68;
            _footer.style.flexShrink = 0;
            _backBtn = GhostButton("← back", () => OnBack?.Invoke());
            _footer.Add(_backBtn);
            _nextBtn = new Button(() => _primaryAction?.Invoke());
            StyleCta(_nextBtn);
            _footer.Add(_nextBtn);
            _container.Add(_footer);

            root?.Add(_container);
            root?.RegisterCallback<GeometryChangedEvent>(_ => FitStage());
            FitStage();
            SetVisible(false);
        }

        // Scale + centre the 1920×1080 stage to fit the actual panel (window) size, like the design's Stage.
        private void FitStage()
        {
            if (_root == null) return;
            float w = _root.resolvedStyle.width, h = _root.resolvedStyle.height;
            if (w <= 1f || h <= 1f) { var rc = _root.contentRect; w = rc.width; h = rc.height; }
            if (w <= 1f || h <= 1f) return;
            float scale = Mathf.Min(w / 1920f, h / 1080f);
            _container.style.scale = new Scale(new Vector2(scale, scale));
            _container.style.left = (w - 1920f) / 2f;
            _container.style.top = (h - 1080f) / 2f;
        }

        public void SetVisible(bool visible) =>
            _container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // ── shared chrome / footer update ─────────────────────────────────────────────────────────────────────
        private VisualElement BeginScreen(int step, string title, string nextLabel, bool canBack, bool nextEnabled,
                                          Action primary = null, bool showFooter = true)
        {
            _stepTitle.text = title;
            _stepCounter.text = $"STEP {step + 1} / {TotalSteps}";
            for (int i = 0; i < _segments.childCount; i++)
                _segments[i].style.backgroundColor = i <= step ? Gold : PA(0.10f);

            _footer.style.display = showFooter ? DisplayStyle.Flex : DisplayStyle.None;
            if (showFooter)
            {
                _primaryAction = primary ?? OnNext;
                _nextBtn.text = (nextLabel ?? "Continue").ToUpperInvariant() + "  →";
                _backBtn.style.visibility = canBack ? Visibility.Visible : Visibility.Hidden;
                SetCtaEnabled(nextEnabled);
            }

            _body.Clear();
            return _body;
        }

        // ── SCREEN 1 — NAME ───────────────────────────────────────────────────────────────────────────────────
        public void RenderName(int step, string currentName, Action<string> onChanged, bool canBack)
        {
            bool valid = !string.IsNullOrWhiteSpace(currentName) && currentName.Trim().Length >= 2;
            var body = BeginScreen(step, "Name", "Continue", canBack, valid);
            var host = Host(24, 12, Align.Center, Justify.Center);
            var col = Column(620);

            col.Add(Prompt("What name will they remember?", 26, 6));
            col.Add(Flavor("The world will carve it into its memory.", PA(0.50f), 30));

            var field = new TextField { value = currentName ?? string.Empty, maxLength = 40 };
            field.style.height = 72; field.style.marginBottom = 8;
            Radius(field, 10); Border(field, PA(0.30f), 2);
            field.style.backgroundColor = Input;
            var inner = field.Q("unity-text-input");
            if (inner != null)
            {
                inner.style.backgroundColor = Input;
                inner.style.color = Bone;
                inner.style.fontSize = 24;
                inner.style.paddingLeft = 22; inner.style.paddingRight = 22;
                inner.style.unityTextAlign = TextAnchor.MiddleLeft;
                ApplyFont(inner, Sans);
                Border(inner, Color.clear, 0);
            }
            field.RegisterValueChangedCallback(e =>
            {
                onChanged?.Invoke(e.newValue);
                bool ok = !string.IsNullOrWhiteSpace(e.newValue) && e.newValue.Trim().Length >= 2;
                SetCtaEnabled(ok);
                Border(field, ok ? Gold : PA(0.30f), 2);
            });
            col.Add(field);

            var count = Text($"{(currentName ?? string.Empty).Length} / 40", Sans, 10, PA(0.25f));
            count.style.letterSpacing = 0.8f;
            count.style.unityTextAlign = TextAnchor.MiddleRight;
            count.style.marginBottom = 20;
            col.Add(count);

            var sug = Text("SUGGESTIONS", Sans, 11, PA(0.32f));
            sug.style.letterSpacing = 1.5f; sug.style.marginBottom = 11;
            col.Add(sug);

            var chips = Row(); chips.style.flexWrap = Wrap.Wrap;
            foreach (var name in new[] { "Ash-Born Commander", "Cinder Vey", "Mora of the Red Road" })
            {
                bool sel = string.Equals(currentName, name, StringComparison.Ordinal);
                var chip = new Button(() => { field.value = name; }) { text = name };
                StyleChip(chip, sel);
                chips.Add(chip);
            }
            col.Add(chips);

            host.Add(col); body.Add(host);
        }

        // ── SCREENS 2-4 — choice list (Mood / Calling / Fate) ─────────────────────────────────────────────────
        public void RenderChoiceList(int step, string title, string prompt, string flavor,
                                     IList<string> choices, int selectedIndex, Action<int> onSelect, bool canBack)
        {
            var body = BeginScreen(step, title, "Continue", canBack, selectedIndex >= 0);
            var host = Host(28, 12, Align.Center, Justify.FlexStart);
            var col = Column(720);
            col.Add(Prompt(prompt, 26, 8));
            if (!string.IsNullOrEmpty(flavor)) col.Add(Flavor(flavor, PA(0.55f), 28));

            for (int i = 0; i < choices.Count; i++)
            {
                int idx = i;
                string letter = ((char)('A' + i)).ToString();
                col.Add(ChoiceTile(letter, choices[i], selectedIndex == i, () => onSelect?.Invoke(idx), 10));
            }
            host.Add(col); body.Add(host);
        }

        // ── SCREEN 5 — Trials of Character (one question per screen) ───────────────────────────────────────────
        public void RenderTrials(int step, int qIndex, int total, bool[] answered, string prompt,
                                 IList<string> choices, int selectedChoice, Action<int> onSelect, Action<int> onJump)
        {
            int answeredCount = 0;
            for (int i = 0; i < answered.Length; i++) if (answered[i]) answeredCount++;
            // PersonalityQuestions never advances via the footer (auto-advances on pick); keep it disabled.
            var body = BeginScreen(step, "Trials of Character", $"{answeredCount} / {total} answered", true, false);
            var host = Host(20, 12, Align.Center, Justify.FlexStart);
            var col = Column(720);

            var progRow = Row(); progRow.style.alignItems = Align.Center; progRow.style.marginBottom = 22;
            var qLabel = Text($"QUESTION {qIndex + 1} / {total}", Sans, 12, Gold, FontStyle.Bold);
            qLabel.style.letterSpacing = 2.4f; qLabel.style.marginRight = 18;
            progRow.Add(qLabel);
            var dots = Row(); dots.style.alignItems = Align.Center;
            for (int i = 0; i < total; i++)
            {
                int idx = i;
                bool done = i < answered.Length && answered[i];
                bool cur = i == qIndex;
                var dot = new VisualElement();
                dot.style.width = cur ? 20 : 8; dot.style.height = 8; Radius(dot, 4);
                dot.style.marginRight = 5;
                dot.style.backgroundColor = done ? Amber : PA(0.12f);
                if (done && !cur && onJump != null)
                    dot.RegisterCallback<ClickEvent>(_ => onJump(idx));
                dots.Add(dot);
            }
            progRow.Add(dots);
            col.Add(progRow);

            col.Add(Prompt(prompt, 24, 28));
            for (int i = 0; i < choices.Count; i++)
            {
                int idx = i;
                string letter = ((char)('A' + i)).ToString();
                col.Add(ChoiceTile(letter, choices[i], selectedChoice == i, () => onSelect?.Invoke(idx), 10));
            }
            host.Add(col); body.Add(host);
        }

        // ── SCREEN 6 — Birthsign (4-col grid of 12 cards) ─────────────────────────────────────────────────────
        public void RenderBirthsign(int step, IList<(string name, string stat, int bonus)> signs,
                                    int selectedIndex, Action<int> onSelect)
        {
            var body = BeginScreen(step, "Birthsign", "Continue", true, selectedIndex >= 0);
            var host = Host(18, 10, Align.Center, Justify.FlexStart);
            var col = Column(1020);
            var flavor = Flavor("Under which sign were you born? It marks your blood with a gift.", PA(0.55f), 22);
            flavor.style.unityFontStyleAndWeight = FontStyle.Italic; flavor.style.fontSize = 18;
            flavor.style.unityTextAlign = TextAnchor.MiddleCenter; ApplyFont(flavor, Serif);
            col.Add(flavor);

            var grid = Row(); grid.style.flexWrap = Wrap.Wrap;
            for (int i = 0; i < signs.Count; i++)
            {
                int idx = i;
                var (name, stat, bonus) = signs[i];
                bool sel = selectedIndex == i;
                var col4 = StatColor(stat);

                var card = new Button(() => onSelect?.Invoke(idx));
                ResetButton(card);
                // 4 per row: width 25% minus the 12px gaps (3 gaps / 4 cols = 9px each)
                card.style.width = Length.Percent(24.1f);   // 4 cols, 1.2% gaps → sums to 100%
                card.style.paddingTop = 20; card.style.paddingBottom = 20;
                card.style.paddingLeft = 14; card.style.paddingRight = 14;
                card.style.marginBottom = 12;
                MarginGapPct(card, i, 4, 1.2f);
                Radius(card, 14);
                card.style.backgroundColor = sel ? GA(0.18f) : DA(0.65f);
                Border(card, sel ? Gold : PA(0.15f), sel ? 2 : 1);
                card.style.alignItems = Align.Center;

                var ring = new VisualElement();
                ring.style.width = 38; ring.style.height = 38; Radius(ring, 19);
                Border(ring, col4, 2);
                ring.style.backgroundColor = sel ? new Color(col4.r, col4.g, col4.b, 0.125f) : Color.clear;
                ring.style.alignItems = Align.Center; ring.style.justifyContent = Justify.Center;
                ring.style.marginBottom = 9;
                if (sel)
                {
                    var pip = new VisualElement();
                    pip.style.width = 11; pip.style.height = 11; Radius(pip, 6);
                    pip.style.backgroundColor = col4;
                    ring.Add(pip);
                }
                card.Add(ring);

                var nm = Text(name, Serif, 14, sel ? Parch : ParchDim, FontStyle.Bold);
                nm.style.unityTextAlign = TextAnchor.MiddleCenter; nm.style.marginBottom = 9;
                nm.style.whiteSpace = WhiteSpace.Normal;
                card.Add(nm);

                var badge = Text($"{stat} +{bonus}", Sans, 12, col4, FontStyle.Bold);
                badge.style.letterSpacing = 1.2f;
                badge.style.backgroundColor = new Color(col4.r, col4.g, col4.b, 0.094f);
                Radius(badge, 5);
                badge.style.paddingLeft = 9; badge.style.paddingRight = 9;
                badge.style.paddingTop = 3; badge.style.paddingBottom = 3;
                card.Add(badge);

                grid.Add(card);
            }
            col.Add(grid);
            host.Add(col); body.Add(host);
        }

        // ── SCREEN 7 — Abilities (dice theater) ───────────────────────────────────────────────────────────────
        public void RenderAbilities(int step, IList<int> rollPool,
                                    IList<(string id, string label, int val, string roll)> stats,
                                    bool canKeep, Action onRollAgain, Action onKeep)
        {
            // Footer CTA "Keep This Roll" → keep + advance (ComputeCanAdvance requires _rollKept).
            var body = BeginScreen(step, "Abilities", "Keep This Roll", true, true,
                primary: () => { onKeep?.Invoke(); OnNext?.Invoke(); });
            var host = Host(18, 10, Align.Center, Justify.Center);
            var col = Column(820);
            col.style.alignItems = Align.Center;

            var flavor = Flavor("4d6 — drop the lowest. The dice fall; fate speaks in numbers.", PA(0.52f), 10);
            flavor.style.fontSize = 18; flavor.style.unityFontStyleAndWeight = FontStyle.Italic;
            flavor.style.unityTextAlign = TextAnchor.MiddleCenter; ApplyFont(flavor, Serif);
            col.Add(flavor);

            var poolRow = Row(); poolRow.style.alignItems = Align.Center; poolRow.style.marginBottom = 28;
            var poolLabel = Text("ROLL POOL:", Sans, 11, PA(0.32f));
            poolLabel.style.letterSpacing = 1.3f; poolLabel.style.marginRight = 8;
            poolRow.Add(poolLabel);
            foreach (var v in rollPool)
            {
                var die = Text(v.ToString(), Sans, 15, Parch, FontStyle.Bold);
                die.style.width = 36; die.style.height = 36; Radius(die, 7);
                die.style.backgroundColor = PanelA(0.72f); Border(die, PA(0.20f), 1);
                die.style.unityTextAlign = TextAnchor.MiddleCenter; die.style.marginRight = 8;
                poolRow.Add(die);
            }
            col.Add(poolRow);

            var grid = Row(); grid.style.flexWrap = Wrap.Wrap; grid.style.width = Length.Percent(100);
            grid.style.maxWidth = 780; grid.style.justifyContent = Justify.Center;
            for (int i = 0; i < stats.Count; i++)
            {
                var (id, label, val, roll) = stats[i];
                var block = new VisualElement();
                block.style.width = Length.Percent(32f);    // 3 cols, 2% gaps → sums to 100%
                block.style.paddingTop = 22; block.style.paddingBottom = 22;
                block.style.paddingLeft = 16; block.style.paddingRight = 16;
                MarginGapPct(block, i, 3, 2f); block.style.marginBottom = 14;
                Radius(block, 14);
                block.style.backgroundColor = DA(0.72f); Border(block, PA(0.16f), 1);
                block.style.alignItems = Align.Center;

                var sid = Text(id, Sans, 10, StatColor(id), FontStyle.Bold);
                sid.style.letterSpacing = 1.8f; sid.style.marginBottom = 2;
                block.Add(sid);
                var slabel = Text(label, Sans, 9, PA(0.35f));
                slabel.style.letterSpacing = 0.7f; slabel.style.marginBottom = 7;
                block.Add(slabel);
                var big = Text(val.ToString(), Sans, 52, Gold, FontStyle.Bold);
                big.style.marginBottom = 6;
                block.Add(big);
                var brk = Text(roll ?? string.Empty, Sans, 10, PA(0.28f));
                brk.style.letterSpacing = 0.7f;
                block.Add(brk);

                grid.Add(block);
            }
            col.Add(grid);

            var roll2 = new Button(() => onRollAgain?.Invoke()) { text = "Roll Again" };
            ResetButton(roll2);
            roll2.style.marginTop = 24;
            roll2.style.fontSize = 13; roll2.style.color = ParchDim; ApplyFont(roll2, Sans);
            roll2.style.unityFontStyleAndWeight = FontStyle.Bold; roll2.style.letterSpacing = 1.3f;
            roll2.style.backgroundColor = PanelA(0.72f); Border(roll2, PA(0.24f), 1); Radius(roll2, 8);
            roll2.style.paddingLeft = 30; roll2.style.paddingRight = 30;
            roll2.style.paddingTop = 13; roll2.style.paddingBottom = 13;
            col.Add(roll2);

            host.Add(col); body.Add(host);
        }

        // ── SCREEN 8 — Build (Class / Alignment / Skills, 3 columns) ──────────────────────────────────────────
        public void RenderBuild(int step,
            IList<(string name, string best, bool rec)> classes, int selClass,
            IList<string> aligns, int selAlign,
            IList<(string name, string attr)> skills, ISet<int> selSkills,
            bool canContinue, Action<int> onClass, Action<int> onAlign, Action<int> onSkill)
        {
            var body = BeginScreen(step, "Class, Alignment & Skills", "Continue", true, canContinue);
            var outer = new VisualElement();
            outer.style.flexGrow = 1; outer.style.flexDirection = FlexDirection.Column;
            outer.style.paddingTop = 16; outer.style.paddingLeft = 68; outer.style.paddingRight = 68;

            outer.Add(Flavor("Choose a class, an alignment, and 1–5 skills. Continue unlocks when all three are set.",
                PA(0.50f), 18));

            var cols = Row(); cols.style.flexGrow = 1;

            // Class column
            var classCol = BuildColumn(1f);
            classCol.Add(ColHeader("CLASS"));
            var classScroll = ColScroll();
            for (int i = 0; i < classes.Count; i++)
            {
                int idx = i; var (name, best, rec) = classes[i]; bool sel = selClass == i;
                var btn = new Button(() => onClass?.Invoke(idx)); ResetButton(btn);
                ListRow(btn, sel, 10);
                var nm = Text(name + (rec ? " (Rec.)" : ""), Sans, 15, sel ? Parch : ParchDim, FontStyle.Bold);
                btn.Add(nm);
                var bestL = Text("Best: " + best, Sans, 11, PA(0.36f));
                bestL.style.letterSpacing = 0.5f; bestL.style.marginTop = 3;
                btn.Add(bestL);
                classScroll.Add(btn);
            }
            classCol.Add(classScroll);
            cols.Add(classCol);

            // Alignment column
            var alignCol = BuildColumn(1f);
            alignCol.Add(ColHeader("ALIGNMENT"));
            var alignScroll = ColScroll();
            for (int i = 0; i < aligns.Count; i++)
            {
                int idx = i; bool sel = selAlign == i;
                var btn = new Button(() => onAlign?.Invoke(idx)); ResetButton(btn);
                ListRow(btn, sel, 10);
                btn.Add(Text(aligns[i], Sans, 14, sel ? Parch : ParchDim));
                alignScroll.Add(btn);
            }
            alignCol.Add(alignScroll);
            cols.Add(alignCol);

            // Skills column (flex 1.15)
            var skillCol = BuildColumn(1.15f);
            var skillHead = Row(); skillHead.style.justifyContent = Justify.SpaceBetween;
            skillHead.style.alignItems = Align.Center; skillHead.style.marginBottom = 12;
            skillHead.Add(ColHeader("SKILLS", 0));
            int count = selSkills.Count;
            var counter = Text($"{count} / 5", Sans, 12, count == 5 ? Amber : PA(0.38f), FontStyle.Bold);
            counter.style.letterSpacing = 0.8f; Radius(counter, 5);
            counter.style.paddingLeft = 10; counter.style.paddingRight = 10;
            counter.style.paddingTop = 2; counter.style.paddingBottom = 2;
            counter.style.backgroundColor = count == 5 ? C(241, 196, 15, 0.12f) : PanelA(0.4f);
            skillHead.Add(counter);
            skillCol.Add(skillHead);
            var skillScroll = ColScroll();
            for (int i = 0; i < skills.Count; i++)
            {
                int idx = i; var (name, attr) = skills[i];
                bool sel = selSkills.Contains(i);
                bool maxed = count >= 5 && !sel;
                var btn = new Button(() => { if (!maxed) onSkill?.Invoke(idx); }); ResetButton(btn);
                btn.style.width = Length.Percent(100); btn.style.marginBottom = 4;
                btn.style.backgroundColor = sel ? GA(0.11f) : DA(0.5f);
                Border(btn, sel ? Amber : PA(0.11f), sel ? 2 : 1); Radius(btn, 8);
                btn.style.paddingLeft = 14; btn.style.paddingRight = 14;
                btn.style.paddingTop = 10; btn.style.paddingBottom = 10;
                btn.style.flexDirection = FlexDirection.Row;
                btn.style.justifyContent = Justify.SpaceBetween; btn.style.alignItems = Align.Center;
                btn.style.opacity = maxed ? 0.38f : 1f;
                btn.Add(Text(name, Sans, 14, sel ? Parch : ParchDim));
                var attrL = Text(attr, Sans, 11, sel ? Amber : PA(0.30f), FontStyle.Bold);
                attrL.style.letterSpacing = 0.8f;
                btn.Add(attrL);
                skillScroll.Add(btn);
            }
            skillCol.Add(skillScroll);
            cols.Add(skillCol);

            outer.Add(cols);
            body.Add(outer);
        }

        // ── SCREEN 9 — Portrait (fixed 340×440 frame) ─────────────────────────────────────────────────────────
        public void RenderPortrait(int step, bool ready, Texture2D tex, int rerollsLeft, Action onReroll)
        {
            var body = BeginScreen(step, "Portrait", "Accept Portrait", true, true);
            var host = Host(16, 10, Align.Center, Justify.Center);
            var col = Column(620); col.style.alignItems = Align.Center;

            var flavor = Flavor("The Forge paints your likeness. Keep it, or roll the embers again.", PA(0.52f), 22);
            flavor.style.fontSize = 18; flavor.style.unityFontStyleAndWeight = FontStyle.Italic;
            flavor.style.unityTextAlign = TextAnchor.MiddleCenter; ApplyFont(flavor, Serif);
            col.Add(flavor);

            var frame = new VisualElement();
            frame.style.width = 340; frame.style.height = 440; Radius(frame, 16);
            frame.style.backgroundColor = Input;
            Border(frame, ready ? Gold : PA(0.22f), 2);
            frame.style.alignItems = Align.Center; frame.style.justifyContent = Justify.Center;
            frame.style.overflow = Overflow.Hidden;
            if (ready && tex != null)
            {
                var img = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit };
                img.style.width = Length.Percent(100); img.style.height = Length.Percent(100);
                frame.Add(img);
            }
            else
            {
                var ring = new VisualElement();
                ring.style.width = 80; ring.style.height = 80; Radius(ring, 40);
                Border(ring, PA(0.22f), 2);
                frame.Add(ring);
                var wait = Flavor("Forging your likeness…", PA(0.30f), 0);
                wait.style.fontSize = 14; wait.style.unityFontStyleAndWeight = FontStyle.Italic;
                wait.style.marginTop = 14; wait.style.unityTextAlign = TextAnchor.MiddleCenter;
                frame.Add(wait);
            }
            col.Add(frame);

            var caption = Flavor(ready ? "Your likeness, drawn from the embers."
                                       : "Generating your likeness from the embers…",
                                 ready ? ParchDim : PA(0.32f), 0);
            caption.style.fontSize = 15; caption.style.unityFontStyleAndWeight = FontStyle.Italic;
            caption.style.marginTop = 16; caption.style.unityTextAlign = TextAnchor.MiddleCenter;
            col.Add(caption);

            if (ready)
            {
                var reroll = new Button(() => onReroll?.Invoke())
                { text = "Reroll Portrait" };   // unlimited rerolls
                ResetButton(reroll);
                reroll.style.marginTop = 14;
                reroll.style.fontSize = 12; reroll.style.color = ParchDim;
                ApplyFont(reroll, Sans); reroll.style.unityFontStyleAndWeight = FontStyle.Bold;
                reroll.style.letterSpacing = 1.4f;
                reroll.style.backgroundColor = C(46, 36, 23, 0.6f);
                Border(reroll, PA(0.28f), 1); Radius(reroll, 8);
                reroll.style.paddingLeft = 24; reroll.style.paddingRight = 24;
                reroll.style.paddingTop = 10; reroll.style.paddingBottom = 10;
                col.Add(reroll);
            }

            host.Add(col); body.Add(host);
        }

        // ── SCREEN 10 — The World Awakens (fixed 2:1 map box) ─────────────────────────────────────────────────
        public void RenderWorldReveal(int step, bool ready, Texture2D map, string narrative,
                                      bool canContinue, string nextLabel)
        {
            var body = BeginScreen(step, "The World Awakens", nextLabel ?? "Enter the World", true, canContinue);
            var host = Host(20, 10, Align.Center, Justify.FlexStart);
            var col = Column(640);
            col.style.alignItems = Align.Center;
            col.style.flexGrow = 1; col.style.minHeight = 0;   // fill the height so the narrative can scroll inside it

            // The overland sampler is 128×64 (2:1). Keep this frame 2:1 too, and stretch instead of crop so the
            // reveal map and in-game map use the same projection.
            var box = new VisualElement();
            box.style.width = 640; box.style.height = 320; box.style.flexShrink = 0; Radius(box, 14);
            box.style.backgroundColor = Input; box.style.overflow = Overflow.Hidden;
            Border(box, ready ? Amber : PA(0.18f), 2);
            box.style.alignItems = Align.Center; box.style.justifyContent = Justify.Center;
            box.style.marginBottom = 14;
            if (ready && map != null)
            {
                var img = new Image { image = map, scaleMode = ScaleMode.StretchToFill };
                img.style.width = Length.Percent(100); img.style.height = Length.Percent(100);
                box.Add(img);
            }
            else
            {
                var label = Flavor(ready ? "" : "The ages unfold…", PA(0.24f), 0);
                label.style.fontSize = 14; label.style.unityFontStyleAndWeight = FontStyle.Italic;
                box.Add(label);
            }
            col.Add(box);

            var caption = Flavor(ready ? "The world you have shaped." : "Tectonic plates forming. Oceans filling…",
                                 ready ? ParchDim : PA(0.32f), 12);
            caption.style.fontSize = 14; caption.style.unityFontStyleAndWeight = FontStyle.Italic;
            caption.style.unityTextAlign = TextAnchor.MiddleCenter; caption.style.flexShrink = 0;
            col.Add(caption);

            if (!string.IsNullOrEmpty(narrative))
            {
                // The narrative scrolls in the remaining space, but starts at the first line. Auto-following
                // the latest line made the deterministic chronicle look out of order and hid the genesis lines.
                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.style.flexGrow = 1; scroll.style.minHeight = 0; scroll.style.maxHeight = 400;
                scroll.style.width = Length.Percent(100); scroll.style.maxWidth = 640; scroll.style.alignSelf = Align.Center;
                var card = new VisualElement();
                card.style.backgroundColor = CardBg; Border(card, PA(0.14f), 1); Radius(card, 12);
                card.style.paddingTop = 20; card.style.paddingBottom = 20;
                card.style.paddingLeft = 24; card.style.paddingRight = 24;
                var p = Text(narrative, Serif, 16, ParchDim);
                p.style.whiteSpace = WhiteSpace.Normal;
                card.Add(p);
                scroll.Add(card);
                col.Add(scroll);
                scroll.schedule.Execute(() => EmberCrpg.Presentation.Ember.UI.InGame.IgDesign.StyleScroll(scroll)).StartingIn(0);
                scroll.schedule.Execute(() => scroll.scrollOffset = Vector2.zero).StartingIn(0);
            }

            host.Add(col); body.Add(host);
        }

        // ── SCREEN 11 — Dossier (two-column summary + Begin CTA) ──────────────────────────────────────────────
        public void RenderDossier(int step, string name, string className, string signLine,
            IList<(string id, int val)> stats, IList<(string label, string value)> kv,
            Texture2D portrait, Action onBegin)
        {
            var body = BeginScreen(step, "Dossier", "Begin Your Story", true, true, showFooter: false);
            var host = Host(20, 10, Align.Center, Justify.FlexStart);

            var row = Row(); row.style.width = Length.Percent(100); row.style.maxWidth = 940;
            row.style.alignSelf = Align.Center;

            // left: portrait + identity
            var left = new VisualElement(); left.style.width = 280; left.style.flexShrink = 0;
            left.style.marginRight = 28;
            var frame = new VisualElement();
            frame.style.width = 280; frame.style.height = 340; Radius(frame, 14);
            frame.style.backgroundColor = Input; Border(frame, Amber, 2);
            frame.style.overflow = Overflow.Hidden; frame.style.marginBottom = 14;
            if (portrait != null)
            {
                var img = new Image { image = portrait, scaleMode = ScaleMode.ScaleToFit };
                img.style.width = Length.Percent(100); img.style.height = Length.Percent(100);
                frame.Add(img);
            }
            left.Add(frame);
            var idCard = Card();
            var nm = Text(string.IsNullOrEmpty(name) ? "Adventurer" : name, Serif, 20, Parch, FontStyle.Bold);
            nm.style.marginBottom = 6; left.Add(idCard); idCard.Add(nm);
            var cls = Text(className ?? "—", Sans, 13, Gold); cls.style.letterSpacing = 1f;
            cls.style.marginBottom = 4; idCard.Add(cls);
            var sa = Text(signLine ?? "—", Serif, 13, PA(0.48f), FontStyle.Italic); idCard.Add(sa);
            row.Add(left);

            // right: attributes + dossier
            var right = new VisualElement(); right.style.flexGrow = 1;
            var attrCard = Card(); attrCard.style.marginBottom = 14;
            var attrHead = Text("ATTRIBUTES", Sans, 11, Gold, FontStyle.Bold);
            attrHead.style.letterSpacing = 2.2f; attrHead.style.marginBottom = 14; attrCard.Add(attrHead);
            var attrGrid = Row(); attrGrid.style.flexWrap = Wrap.Wrap;
            for (int i = 0; i < stats.Count; i++)
            {
                var (id, val) = stats[i];
                var cell = new VisualElement(); cell.style.width = Length.Percent(33);
                cell.style.alignItems = Align.Center; cell.style.marginBottom = 8;
                var sid = Text(id, Sans, 10, StatColor(id), FontStyle.Bold); sid.style.letterSpacing = 1.2f;
                cell.Add(sid);
                cell.Add(Text(val.ToString(), Sans, 28, Parch, FontStyle.Bold));
                attrGrid.Add(cell);
            }
            attrCard.Add(attrGrid); right.Add(attrCard);

            var dosCard = Card(); dosCard.style.flexGrow = 1;
            var dosHead = Text("DOSSIER", Sans, 11, Gold, FontStyle.Bold);
            dosHead.style.letterSpacing = 2.2f; dosHead.style.marginBottom = 12; dosCard.Add(dosHead);
            foreach (var (label, value) in kv) dosCard.Add(KvRow(label, value));
            right.Add(dosCard);
            row.Add(right);

            host.Add(row);

            // Begin Your Story — full CTA (replaces footer)
            var ctaWrap = Row(); ctaWrap.style.justifyContent = Justify.Center;
            ctaWrap.style.paddingTop = 16; ctaWrap.style.paddingBottom = 28; ctaWrap.style.flexShrink = 0;
            var begin = new Button(() => onBegin?.Invoke()) { text = "BEGIN YOUR STORY" };
            ResetButton(begin);
            begin.style.fontSize = 18; begin.style.color = Ink; ApplyFont(begin, Serif);
            begin.style.unityFontStyleAndWeight = FontStyle.Bold; begin.style.letterSpacing = 4f;
            begin.style.backgroundColor = Gold; Radius(begin, 10);
            begin.style.paddingLeft = 80; begin.style.paddingRight = 80;
            begin.style.paddingTop = 18; begin.style.paddingBottom = 18;

            body.Add(host);
            body.Add(ctaWrap); ctaWrap.Add(begin);
        }

        // ── shared element builders ───────────────────────────────────────────────────────────────────────────
        private VisualElement ChoiceTile(string letter, string text, bool selected, Action onClick, float gapBelow)
        {
            var btn = new Button(() => onClick?.Invoke()); ResetButton(btn);
            btn.style.width = Length.Percent(100); btn.style.marginBottom = gapBelow;
            btn.style.flexDirection = FlexDirection.Row; btn.style.alignItems = Align.Center;
            btn.style.paddingTop = 14; btn.style.paddingBottom = 14;
            btn.style.paddingLeft = 20; btn.style.paddingRight = 20;
            Radius(btn, 10);
            btn.style.backgroundColor = selected ? GA(0.12f) : DA(0.55f);
            Border(btn, selected ? Gold : PA(0.17f), selected ? 2 : 1);

            var badge = Text(letter, Sans, 12, selected ? Ink : C(241, 196, 15, 0.48f), FontStyle.Bold);
            badge.style.width = 28; badge.style.height = 28; Radius(badge, 5);
            badge.style.unityTextAlign = TextAnchor.MiddleCenter; badge.style.marginRight = 16;
            badge.style.flexShrink = 0;
            badge.style.backgroundColor = selected ? Gold : Color.clear;
            Border(badge, selected ? Color.clear : C(241, 196, 15, 0.26f), selected ? 0 : 1);
            btn.Add(badge);

            var label = Text(text, Serif, 17, selected ? Parch : ParchDim);
            label.style.whiteSpace = WhiteSpace.Normal; label.style.flexGrow = 1;
            label.style.flexShrink = 1;
            btn.Add(label);
            return btn;
        }

        private static VisualElement BuildColumn(float flex)
        {
            var c = new VisualElement(); c.style.flexGrow = flex; c.style.flexShrink = 1;
            c.style.flexBasis = 0; c.style.marginRight = 16;
            c.style.flexDirection = FlexDirection.Column;
            return c;
        }

        private static ScrollView ColScroll()
        {
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.style.flexGrow = 1;
            sv.mode = ScrollViewMode.Vertical;
            sv.schedule.Execute(() => EmberCrpg.Presentation.Ember.UI.InGame.IgDesign.StyleScroll(sv)).StartingIn(0);
            return sv;
        }

        private Label ColHeader(string text, float marginBottom = 12)
        {
            var l = Text(text, Sans, 11, Gold, FontStyle.Bold);
            l.style.letterSpacing = 2.4f; l.style.marginBottom = marginBottom; l.style.flexShrink = 0;
            return l;
        }

        private void ListRow(Button btn, bool sel, float radius)
        {
            btn.style.width = Length.Percent(100); btn.style.marginBottom = 5;
            btn.style.backgroundColor = sel ? GA(0.11f) : DA(0.62f);
            Border(btn, sel ? Gold : PA(0.13f), sel ? 2 : 1); Radius(btn, radius);
            btn.style.paddingLeft = 16; btn.style.paddingRight = 16;
            btn.style.paddingTop = 13; btn.style.paddingBottom = 13;
            btn.style.flexDirection = FlexDirection.Column; btn.style.alignItems = Align.FlexStart;
        }

        private VisualElement Card()
        {
            var c = new VisualElement();
            c.style.backgroundColor = CardBg; Border(c, PA(0.15f), 1); Radius(c, 12);
            c.style.paddingTop = 16; c.style.paddingBottom = 16;
            c.style.paddingLeft = 18; c.style.paddingRight = 18;
            return c;
        }

        private VisualElement KvRow(string label, string value)
        {
            var row = Row(); row.style.paddingTop = 7; row.style.paddingBottom = 7;
            row.style.borderBottomWidth = 1; row.style.borderBottomColor = PA(0.07f);
            var l = Text(label.ToUpperInvariant(), Sans, 11, PA(0.38f), FontStyle.Bold);
            l.style.letterSpacing = 1.1f; l.style.width = 120; l.style.flexShrink = 0;
            l.style.marginRight = 10;
            row.Add(l);
            var v = Text(string.IsNullOrEmpty(value) ? "—" : value, Serif, 14, ParchDim);
            v.style.whiteSpace = WhiteSpace.Normal; v.style.flexGrow = 1; v.style.flexShrink = 1;
            row.Add(v);
            return row;
        }

        // ── primitives ────────────────────────────────────────────────────────────────────────────────────────
        private VisualElement Host(float padTop, float padBottom, Align hAlign, Justify vJustify)
        {
            var c = new VisualElement();
            c.style.flexGrow = 1; c.style.flexDirection = FlexDirection.Column;
            c.style.paddingTop = padTop; c.style.paddingBottom = padBottom;
            c.style.paddingLeft = 68; c.style.paddingRight = 68;
            c.style.alignItems = hAlign; c.style.justifyContent = vJustify;
            return c;
        }

        private VisualElement Column(float maxWidth)
        {
            var e = new VisualElement();
            e.style.width = Length.Percent(100); e.style.maxWidth = maxWidth;
            e.style.flexShrink = 0;
            return e;
        }

        private Label Prompt(string text, int size, float marginBottom)
        {
            var l = Text(text, Serif, size, ParchDim, FontStyle.Italic);
            l.style.whiteSpace = WhiteSpace.Normal; l.style.marginBottom = marginBottom;
            return l;
        }

        private Label Flavor(string text, Color color, float marginBottom)
        {
            var l = Text(text, Serif, 15, color);
            l.style.whiteSpace = WhiteSpace.Normal; l.style.marginBottom = marginBottom;
            return l;
        }

        private static Label Text(string text, Font font, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var l = new Label(text);
            l.style.fontSize = size; l.style.color = color;
            l.style.unityFontStyleAndWeight = style;
            l.style.marginTop = 0; l.style.marginBottom = 0; l.style.marginLeft = 0; l.style.marginRight = 0;
            l.style.paddingTop = 0; l.style.paddingBottom = 0;
            ApplyFont(l, font);
            return l;
        }

        private static VisualElement Row()
        {
            var e = new VisualElement(); e.style.flexDirection = FlexDirection.Row;
            return e;
        }

        private Button GhostButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text }; ResetButton(b);
            b.style.fontSize = 14; b.style.color = WA(0.32f); ApplyFont(b, Sans);
            b.style.letterSpacing = 0.6f;
            b.style.paddingTop = 8; b.style.paddingBottom = 8;
            return b;
        }

        private void StyleCta(Button b)
        {
            ResetButton(b);
            b.style.fontSize = 13; b.style.color = Ink; ApplyFont(b, Sans);
            b.style.unityFontStyleAndWeight = FontStyle.Bold; b.style.letterSpacing = 2.1f;
            b.style.backgroundColor = Gold; Radius(b, 8);
            b.style.paddingLeft = 38; b.style.paddingRight = 38;
            b.style.paddingTop = 14; b.style.paddingBottom = 14;
        }

        private void SetCtaEnabled(bool enabled)
        {
            _nextBtn.SetEnabled(enabled);
            _nextBtn.style.backgroundColor = enabled ? Gold : C(36, 27, 14, 0.45f);
            _nextBtn.style.color = enabled ? Ink : PA(0.20f);
        }

        private void StyleChip(Button b, bool selected)
        {
            ResetButton(b);
            b.style.fontSize = 15; ApplyFont(b, Serif);
            b.style.unityFontStyleAndWeight = FontStyle.Italic;
            b.style.color = selected ? Parch : ParchDim;
            b.style.backgroundColor = selected ? GA(0.11f) : C(30, 23, 14, 0.6f);
            Border(b, selected ? Amber : PA(0.18f), 1); Radius(b, 8);
            b.style.marginRight = 10; b.style.marginTop = 10;
            b.style.paddingLeft = 18; b.style.paddingRight = 18;
            b.style.paddingTop = 9; b.style.paddingBottom = 9;
        }

        // even spacing inside a flex-wrap grid: right margin (PERCENT, so widths+gaps sum to exactly 100% and
        // never overflow → wrap early) except on the last column of each row.
        private static void MarginGapPct(VisualElement el, int index, int cols, float gapPct)
        {
            el.style.marginRight = (index % cols) < cols - 1 ? Length.Percent(gapPct) : Length.Percent(0f);
        }

        private static void ResetButton(Button b)
        {
            b.style.marginTop = 0; b.style.marginBottom = 0; b.style.marginLeft = 0; b.style.marginRight = 0;
            b.style.borderTopLeftRadius = 0; b.style.borderTopRightRadius = 0;
            b.style.borderBottomLeftRadius = 0; b.style.borderBottomRightRadius = 0;
            Border(b, Color.clear, 0);
            b.style.backgroundColor = Color.clear;
            b.style.paddingTop = 0; b.style.paddingBottom = 0; b.style.paddingLeft = 0; b.style.paddingRight = 0;
        }

        private static void Radius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        private static void Border(VisualElement e, Color color, float width)
        {
            e.style.borderTopWidth = width; e.style.borderBottomWidth = width;
            e.style.borderLeftWidth = width; e.style.borderRightWidth = width;
            e.style.borderTopColor = color; e.style.borderBottomColor = color;
            e.style.borderLeftColor = color; e.style.borderRightColor = color;
        }

        private static void ApplyFont(VisualElement e, Font font)
        {
            if (font == null) return;
            e.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
        }
    }
}
