// Why this file is intentionally long: it is the single UI Toolkit adapter that maps the small UI abstraction onto PRD-specific runtime panel slots.
using System;
using System.Collections.Generic;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberCrpg.Ui.Backends.UiToolkit
{
    public sealed class UiToolkitPanel : IUiPanel
    {
        private readonly Dictionary<string, VisualElement> _slots = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Action> _buttonHandlers = new Dictionary<string, Action>();
        // Slot-key prefix -> parent container. Lets dynamic slots (e.g. class_button_3) land in a
        // specific column instead of the flat panel root, so grouped layouts don't overlap.
        private readonly List<KeyValuePair<string, VisualElement>> _slotPrefixParents = new List<KeyValuePair<string, VisualElement>>();
        private readonly VisualElement _container;
        private readonly UiTokens _tokens;

        public UiToolkitPanel(string id, VisualElement root, UiTokens tokens)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "Panel" : id;
            _tokens = tokens;
            _container = new VisualElement { name = Id };
            _container.AddToClassList("ember-panel");
            ApplyPanelStyle(_container);
            root?.Add(_container);
            BuildFrameFor(Id);
        }

        public string Id { get; }

        public void SetText(string slot, string text)
        {
            var element = GetOrCreate(slot, CreateForSlot);
            switch (element)
            {
                case Button button:
                    button.text = text ?? string.Empty;
                    // Selection feedback (design system: selection is a gold OUTLINE, not a gold
                    // flood). "[X] ..." rows get a warmer fill + bright gold hairline + ember-gold
                    // text; "[ ] ..." stay muted panel-brown with the faint gold hairline.
                    if ((text ?? string.Empty).StartsWith("[X]", StringComparison.Ordinal))
                    {
                        button.style.backgroundColor = _tokens != null ? _tokens.PanelBrownHover : new Color(0.227f, 0.18f, 0.114f);
                        button.style.color = _tokens != null ? _tokens.EmberGold : new Color(1f, 0.851f, 0.298f);
                        SetHairline(button, _tokens != null ? _tokens.StateSelect : new Color(0.914f, 0.788f, 0.227f));
                    }
                    else if ((text ?? string.Empty).StartsWith("[ ]", StringComparison.Ordinal))
                    {
                        button.style.backgroundColor = _tokens != null ? _tokens.PanelBrown : new Color(0.18f, 0.14f, 0.09f);
                        button.style.color = _tokens != null ? _tokens.Parchment : new Color(0.949f, 0.859f, 0.62f);
                        SetHairline(button, _tokens != null ? _tokens.GoldHairline : new Color(0.949f, 0.859f, 0.62f, 0.22f));
                    }
                    break;
                case TextField field:
                    field.value = text ?? string.Empty;
                    break;
                case Label label:
                    label.text = text ?? string.Empty;
                    break;
                default:
                    element.Clear();
                    element.Add(MakeLabel(text ?? string.Empty, 14, false));
                    break;
            }
        }

        public void SetProgress(string slot, float normalized)
        {
            var bar = GetTyped<ProgressBar>(slot, () => MakeProgress());
            bar.value = Mathf.Clamp01(normalized) * 100f;
        }

        public void LogLine(string slot, UiLogSeverity severity, string line)
        {
            var label = MakeLabel(line ?? string.Empty, 13, false);
            label.style.color = _tokens != null ? _tokens.SeverityColor(severity) : Color.white;
            label.style.whiteSpace = WhiteSpace.Normal;
            var log = GetTyped<ScrollView>(slot, () => new ScrollView(ScrollViewMode.Vertical));
            log.Add(label);
            log.ScrollTo(label);
        }

        public void SetThumbnail(string slot, Texture2D texture)
        {
            GetTyped<Image>(slot, () => new Image()).image = texture;
        }

        // Show/hide the whole panel container. The surface uses this to keep one active panel
        // (full-screen screens shouldn't stack on top of each other).
        public void SetSurfaceVisible(bool visible)
        {
            if (_container != null)
                _container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetThumbnailGrid(string slot, System.Collections.Generic.IReadOnlyList<Texture2D> textures)
        {
            // The slot is expected to be (or become) a VisualElement container. Children are
            // wiped and rebuilt every call so callers can re-publish as new PNGs land on disk.
            var container = GetOrCreate(slot, _ =>
            {
                var v = new VisualElement();
                v.style.flexDirection = FlexDirection.Row;
                v.style.flexWrap = Wrap.Wrap;
                v.style.justifyContent = Justify.Center;
                v.style.marginTop = 8;
                return v;
            });
            container.Clear();
            if (textures == null) return;
            for (int i = 0; i < textures.Count; i++)
            {
                var tex = textures[i];
                if (tex == null) continue;
                var img = new Image { image = tex };
                img.style.width = 40;
                img.style.height = 40;
                img.style.marginLeft = 3;
                img.style.marginRight = 3;
                img.style.marginTop = 3;
                img.style.marginBottom = 3;
                img.scaleMode = ScaleMode.ScaleToFit;
                container.Add(img);
            }
        }

        public void SetVisible(string slot, bool visible)
        {
            GetOrCreate(slot, CreateForSlot).style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetButtonHandler(string slot, Action onClick)
        {
            var button = GetTyped<Button>(slot, () => new Button());
            var key = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            if (_buttonHandlers.TryGetValue(key, out var existing))
                button.clicked -= existing;
            if (onClick == null)
            {
                _buttonHandlers.Remove(key);
                return;
            }

            _buttonHandlers[key] = onClick;
            button.clicked += onClick;
        }

        public void Dispose()
        {
            _container?.RemoveFromHierarchy();
            _slots.Clear();
        }

        private void BuildFrameFor(string id)
        {
            if (id == "LoadingScreen" || id == "BootScreen")
            {
                Register("root", new VisualElement());
                // Same full-bleed setup as TitleMenu so the loading backdrop fills the panel.
                var loadingBackdrop = new Image();
                loadingBackdrop.style.position = Position.Absolute;
                loadingBackdrop.style.left = 0; loadingBackdrop.style.right = 0; loadingBackdrop.style.top = 0; loadingBackdrop.style.bottom = 0;
                loadingBackdrop.scaleMode = ScaleMode.ScaleAndCrop;
                Register("backdrop", loadingBackdrop);
                Register("title", MakeLabel("", 30, true));
                Register("subtitle", MakeLabel("", 16, false));
                Register("area", MakeLabel("", 20, true));
                Register("status", MakeLabel("", 16, true));
                Register("loading", MakeLabel("", 16, true));
                Register("tip", MakeLabel("", 14, false));
                Register("current", MakeLabel("", 14, false));
                Register("progress", MakeProgress());
                Register("fade", MakeProgress());
                Register("thumbnail", new Image());
                Register("caption", MakeLabel("", 12, false));
                Register("log", MakeLog());
                Register("inputBlock", new VisualElement());
                Register("continue", MakeButton("Continue"));
                return;
            }

            if (id == "CharacterCreation")
            {
                var ccHeader = MakeLabel("CHARACTER CREATION", 26, true);
                ccHeader.style.marginBottom = 2;
                Register("header", ccHeader);

                var ccStep = MakeLabel("", 16, true);
                ccStep.style.marginBottom = 8;
                Register("step", ccStep);

                var ccProgress = MakeProgress();
                ccProgress.style.marginTop = 2;
                ccProgress.style.marginBottom = 12;
                Register("progress", ccProgress);

                // Body holds the per-stage prompt text. flexShrink=0 stops the build columns /
                // log (both flexGrow=1) from squeezing it to zero and clipping the text; the Auto
                // scroller hides the scrollbar unless the prompt overflows (the stray up/down
                // widget beside the progress bar was this scrollbar showing on a one-liner).
                var ccBody = new ScrollView(ScrollViewMode.Vertical);
                ccBody.verticalScrollerVisibility = ScrollerVisibility.Auto;
                ccBody.style.flexShrink = 0;
                ccBody.style.maxHeight = 160;
                ccBody.style.marginBottom = 10;
                Register("body", ccBody);

                // Step 4 three-column build area (SINIF / AHLAK / YETENEK). Hidden by default;
                // RenderBuildButtons shows it and routes class_/alignment_/skill_ buttons into the
                // matching column's scroll so they no longer pile into one flat overlapping stack.
                var buildArea = new VisualElement();
                buildArea.style.flexDirection = FlexDirection.Row;
                buildArea.style.flexGrow = 1;
                buildArea.style.display = DisplayStyle.None;
                Register("build_area", buildArea);
                AddBuildColumn(buildArea, "class", "SINIF");
                AddBuildColumn(buildArea, "alignment", "AHLAK");
                AddBuildColumn(buildArea, "skill", "YETENEK");

                // Single-column stage content for the non-build stages (birthsign, world-genesis,
                // ability-roll actions, portrait actions, questions, identity). Routing the dynamic
                // option/action buttons here keeps them ABOVE the Back/Continue footer — the old
                // root-append put choice buttons (e.g. "Roll Again") visually BELOW Continue.
                var stageContent = new VisualElement();
                stageContent.style.flexShrink = 0;
                stageContent.style.marginBottom = 6;
                Register("stage_content", stageContent);
                foreach (var prefix in new[]
                {
                    "birthsign_button_", "mood_button_", "calling_button_", "fate_button_",
                    "identity_button_", "answer", "roll_button", "keep_button", "swap_button",
                    "advanced_button", "portrait_reroll_button", "portrait_lock_button",
                })
                    _slotPrefixParents.Add(new KeyValuePair<string, VisualElement>(prefix, stageContent));

                Register("portraitJson", MakeLabel("", 12, false));
                Register("skills", MakeLabel("", 12, false));
                Register("log", MakeLog());
                Register("back", MakeButton("Back"));
                Register("next", MakeButton("Continue"));
                return;
            }

            if (id == "TitleMenu")
            {
                // HTML-page layout (per approved sketch): three stacked layers —
                //   z0 full-bleed backdrop, z1 dark overlay for readability, z2 centered content.
                // No icons / decoration strip: the menu is splash + clean centred button column.
                var backdrop = new Image();
                backdrop.style.position = Position.Absolute;
                backdrop.style.left = 0; backdrop.style.right = 0; backdrop.style.top = 0; backdrop.style.bottom = 0;
                backdrop.scaleMode = ScaleMode.ScaleAndCrop;
                Register("backdrop", backdrop);

                var overlay = new VisualElement();
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
                overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                _container.Add(overlay);

                var content = new VisualElement();
                content.style.flexGrow = 1;
                content.style.flexDirection = FlexDirection.Column;
                content.style.alignItems = Align.Center;
                content.style.justifyContent = Justify.Center;
                _container.Add(content);

                var title = MakeLabel("EMBER CRPG", 52, true);
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                RegisterInto(content, "title", title);
                var subtitle = MakeLabel("A dark fantasy chronicle", 16, false);
                subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
                subtitle.style.marginBottom = 30;
                RegisterInto(content, "subtitle", subtitle);

                // Fixed-width vertical button column — buttons stretch to 380px, even spacing.
                var buttons = new VisualElement();
                buttons.style.width = 380;
                buttons.style.flexDirection = FlexDirection.Column;
                RegisterInto(buttons, "new_game", MakeButton("New Game"));
                RegisterInto(buttons, "continue", MakeButton("Resume"));
                RegisterInto(buttons, "load", MakeButton("Load Game"));
                RegisterInto(buttons, "options", MakeButton("Options"));
                RegisterInto(buttons, "quit", MakeButton("Exit"));
                content.Add(buttons);

                var status = MakeLabel("", 13, false);
                status.style.unityTextAlign = TextAnchor.MiddleCenter;
                status.style.marginTop = 16;
                RegisterInto(content, "status", status);

                // Version pinned to the bottom-left corner, dim.
                var version = MakeLabel("", 12, false);
                version.style.position = Position.Absolute;
                version.style.left = 16; version.style.bottom = 12;
                version.style.opacity = 0.6f;
                Register("version", version);
                return;
            }

            if (id == "WorldgenView")
            {
                Register("title", MakeLabel("WORLD GENERATION", 24, true));
                Register("log", MakeLog());
                Register("question", MakeLabel("", 16, true));
                Register("answer0", MakeButton("Answer 1"));
                Register("answer1", MakeButton("Answer 2"));
                Register("answer2", MakeButton("Answer 3"));
                Register("answer3", MakeButton("Answer 4"));
                Register("auto", MakeButton("Auto-advance"));
                Register("continue", MakeButton("Continue"));
            }
        }

        private VisualElement CreateForSlot(string slot)
        {
            if (slot.Contains("button") || slot == "next" || slot == "back" || slot == "continue" || slot.StartsWith("answer")) return MakeButton(slot);
            if (slot.Contains("progress")) return MakeProgress();
            if (slot.Contains("thumbnail") || slot.Contains("portrait") || slot.Contains("image")) return new Image();
            if (slot == "log") return MakeLog();
            return MakeLabel(string.Empty, 14, false);
        }

        private T GetTyped<T>(string slot, Func<T> create) where T : VisualElement
        {
            var key = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            if (_slots.TryGetValue(key, out var existing) && existing is T typed) return typed;
            existing?.RemoveFromHierarchy();
            var element = create();
            Register(key, element);
            return element;
        }

        private VisualElement GetOrCreate(string slot, Func<string, VisualElement> create)
        {
            var key = string.IsNullOrWhiteSpace(slot) ? "default" : slot;
            if (_slots.TryGetValue(key, out var element)) return element;
            element = create(key);
            Register(key, element);
            return element;
        }

        private void Register(string key, VisualElement element)
        {
            element.name = key;
            _slots[key] = element;
            ResolveParent(key).Add(element);
        }

        // Route a slot to a registered prefix-parent (column) when its key matches; else panel root.
        private VisualElement ResolveParent(string key)
        {
            for (int i = 0; i < _slotPrefixParents.Count; i++)
                if (key.StartsWith(_slotPrefixParents[i].Key, StringComparison.Ordinal))
                    return _slotPrefixParents[i].Value;
            return _container;
        }

        // Register a slot but parent it under a specific container instead of the panel root,
        // so panels can build a real nested layout (header / button column / footer) instead of
        // dumping every element into one flat stack that visually overlaps.
        private void RegisterInto(VisualElement parent, string key, VisualElement element)
        {
            element.name = key;
            _slots[key] = element;
            parent.Add(element);
        }

        // Build one labelled, independently-scrolling column in the Step 4 build area and register
        // a routing rule so "{key}_button_*" slots land inside this column's scroll view.
        private void AddBuildColumn(VisualElement parent, string key, string headerText)
        {
            var column = new VisualElement();
            column.style.flexGrow = 1;
            column.style.flexBasis = 0;
            column.style.flexDirection = FlexDirection.Column;
            column.style.marginLeft = 4; column.style.marginRight = 4;

            var header = MakeLabel(headerText, 18, true);
            header.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.style.marginBottom = 6;
            RegisterInto(column, key + "_header", header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            column.Add(scroll);
            _slotPrefixParents.Add(new KeyValuePair<string, VisualElement>(key + "_button_", scroll));

            parent.Add(column);
        }

        private ScrollView MakeLog()
        {
            var log = new ScrollView(ScrollViewMode.Vertical);
            log.style.flexGrow = 1;
            log.style.minHeight = 140;
            log.style.maxHeight = 360;
            log.style.backgroundColor = new Color(0.03f, 0.025f, 0.02f, 0.86f);
            log.style.paddingLeft = 10;
            log.style.paddingRight = 10;
            log.style.paddingTop = 8;
            log.style.paddingBottom = 8;
            return log;
        }

        // Themed progress bar: dark warm track, ember-gold fill, rounded, no "%" label.
        private ProgressBar MakeProgress()
        {
            var bar = new ProgressBar { lowValue = 0f, highValue = 100f };
            bar.style.height = 12;
            SetRadius(bar, _tokens != null ? _tokens.RadiusSm : 6f);
            var track = bar.Q(className: "unity-progress-bar__background");
            if (track != null)
            {
                track.style.backgroundColor = _tokens != null ? _tokens.InputBrown : new Color(0.122f, 0.102f, 0.078f);
                SetRadius(track, _tokens != null ? _tokens.RadiusSm : 6f);
            }
            var fill = bar.Q(className: "unity-progress-bar__progress");
            if (fill != null)
                fill.style.backgroundColor = _tokens != null ? _tokens.EmberGold : new Color(1f, 0.851f, 0.298f);
            var label = bar.Q<Label>(className: "unity-progress-bar__title");
            if (label != null) label.style.display = DisplayStyle.None;
            return bar;
        }

        private Button MakeButton(string text)
        {
            var button = new Button { text = text ?? string.Empty };
            // Grow to fit content instead of clipping: long / multi-line labels
            // (class "Name\nBest with: ...", "sleight_of_hand (Agi-based)", birthsign
            // rows) used to overflow a fixed 44px box. minHeight + wrap + padding lets
            // the row expand to the text while keeping the 44px touch target floor.
            button.style.minHeight = 44;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.marginTop = 4;
            button.style.marginBottom = 4;
            button.style.fontSize = _tokens != null ? _tokens.FontSizeBody : 20f;
            button.style.backgroundColor = _tokens != null ? _tokens.PanelBrown : new Color(0.18f, 0.14f, 0.09f);
            button.style.color = _tokens != null ? _tokens.Parchment : new Color(0.949f, 0.859f, 0.62f);
            // Softly-rounded warm furniture (design system): 6px corners + faint gold hairline,
            // never sharp grey chrome. Approximates the leather-gradient direction with a solid
            // warm fill (runtime USS has no linear-gradient).
            SetRadius(button, _tokens != null ? _tokens.RadiusSm : 6f);
            SetHairline(button, _tokens != null ? _tokens.GoldHairline : new Color(0.949f, 0.859f, 0.62f, 0.22f));
            ApplyUiFont(button);
            return button;
        }

        private static Font _uiFont;
        private static bool _uiFontLoaded;

        // Lazy-load the design-system UI face (Jost) from Resources. Null-safe: if the TTF is
        // missing the panels keep TMP's default font, so this never breaks a build.
        private static Font UiFont()
        {
            if (!_uiFontLoaded)
            {
                _uiFontLoaded = true;
                _uiFont = Resources.Load<Font>("Fonts/Jost");
            }
            return _uiFont;
        }

        private static void ApplyUiFont(VisualElement el)
        {
            var font = UiFont();
            if (font != null) el.style.unityFont = font; // StyleFont has an implicit operator from Font
        }

        // Round all four corners to the same radius.
        private static void SetRadius(VisualElement el, float r)
        {
            el.style.borderTopLeftRadius = r;
            el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r;
            el.style.borderBottomRightRadius = r;
        }

        // Apply a 1px hairline border in the given color on all four edges.
        private static void SetHairline(VisualElement el, Color color)
        {
            el.style.borderTopWidth = 1; el.style.borderRightWidth = 1;
            el.style.borderBottomWidth = 1; el.style.borderLeftWidth = 1;
            el.style.borderTopColor = color; el.style.borderRightColor = color;
            el.style.borderBottomColor = color; el.style.borderLeftColor = color;
        }

        private Label MakeLabel(string text, int size, bool strong)
        {
            var label = new Label(text ?? string.Empty);
            label.style.fontSize = size;
            label.style.unityFontStyleAndWeight = strong ? FontStyle.Bold : FontStyle.Normal;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = _tokens != null ? _tokens.Text : Color.white;
            label.style.marginBottom = 6;
            ApplyUiFont(label);
            return label;
        }

        private void ApplyPanelStyle(VisualElement element)
        {
            element.style.flexGrow = 1;
            element.style.paddingLeft = 42;
            element.style.paddingRight = 42;
            element.style.paddingTop = 32;
            element.style.paddingBottom = 32;
            element.style.backgroundColor = _tokens != null ? _tokens.Background : new Color(0.045f, 0.035f, 0.028f);
            element.style.color = _tokens != null ? _tokens.Text : Color.white;
        }
    }
}
