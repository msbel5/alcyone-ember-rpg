using System;
using System.Globalization;
using System.Reflection;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Presentation.Ember.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    public sealed partial class SettingsSection
    {
        private static readonly FieldInfo EmberInputActionsField = typeof(EmberInput).GetField("_actions", BindingFlags.NonPublic | BindingFlags.Static);

        // Why: world fallback edits are useful immediately for the next deterministic generation, not the current loaded world.
        private void BuildWorld(Transform parent)
        {
            var world = Section(parent, "World", "Fallback values affect the next world generation only. No regenerate or live world mutation happens here.");
            Editable(world, "Fallback Seed", Read(() => Options.WorldHost.FallbackWorldSeed), "next gen", text => CommitUInt(text, (o, v) => o.WorldHost.FallbackWorldSeed = v, () => Options.WorldHost.FallbackWorldSeed));
            Editable(world, "Population", Read(() => Options.WorldHost.FallbackTargetPopulation), "next gen", text => CommitInt(text, (o, v) => o.WorldHost.FallbackTargetPopulation = Math.Max(1, v), () => Options.WorldHost.FallbackTargetPopulation));
            Editable(world, "Regions", Read(() => Options.WorldHost.FallbackRegionCount), "next gen", text => CommitInt(text, (o, v) => o.WorldHost.FallbackRegionCount = Math.Max(1, v), () => Options.WorldHost.FallbackRegionCount));
            Editable(world, "Factions", Read(() => Options.WorldHost.FallbackFactionCount), "next gen", text => CommitInt(text, (o, v) => o.WorldHost.FallbackFactionCount = Math.Max(1, v), () => Options.WorldHost.FallbackFactionCount));
            Editable(world, "History Years", Read(() => Options.WorldHost.FallbackHistoryYears), "next gen", text => CommitInt(text, (o, v) => o.WorldHost.FallbackHistoryYears = Math.Max(1, v), () => Options.WorldHost.FallbackHistoryYears));
            Editable(world, "Mood", Options.WorldHost.FallbackMood, "next gen", text => CommitText(text, (o, v) => o.WorldHost.FallbackMood = v, () => Options.WorldHost.FallbackMood));
            Editable(world, "Calling", Options.WorldHost.FallbackCalling, "next gen", text => CommitText(text, (o, v) => o.WorldHost.FallbackCalling = v, () => Options.WorldHost.FallbackCalling));
            Editable(world, "Start", Options.WorldHost.FallbackStart, "next gen", text => CommitText(text, (o, v) => o.WorldHost.FallbackStart = v, () => Options.WorldHost.FallbackStart));
            Editable(world, "Spell Slots", Read(() => Options.WorldHost.SpellSlotCount), "live", text => CommitInt(text, (o, v) => o.WorldHost.SpellSlotCount = Math.Max(1, v), () => Options.WorldHost.SpellSlotCount));
            Editable(world, "Quest Guidance", Options.WorldHost.ShowQuestGuidance.ToString(), "live", text => CommitBool(text, (o, v) => o.WorldHost.ShowQuestGuidance = v, () => Options.WorldHost.ShowQuestGuidance));
            Editable(world, "Quest Compass", Options.WorldHost.ShowQuestCompass.ToString(), "live", text => CommitBool(text, (o, v) => o.WorldHost.ShowQuestCompass = v, () => Options.WorldHost.ShowQuestCompass));
        }

        // Why: input edits should also refresh the action map so gameplay reads the new bindings without a restart.
        private void BuildInput(Transform parent)
        {
            var input = Section(parent, "Input", "Keybind edits rebuild the gameplay action map immediately from the current runtime options.");
            Editable(input, "Move Up", Options.Input.MoveUpPath, "live", text => CommitBinding(text, (o, v) => o.Input.MoveUpPath = v, () => Options.Input.MoveUpPath));
            Editable(input, "Move Down", Options.Input.MoveDownPath, "live", text => CommitBinding(text, (o, v) => o.Input.MoveDownPath = v, () => Options.Input.MoveDownPath));
            Editable(input, "Move Left", Options.Input.MoveLeftPath, "live", text => CommitBinding(text, (o, v) => o.Input.MoveLeftPath = v, () => Options.Input.MoveLeftPath));
            Editable(input, "Move Right", Options.Input.MoveRightPath, "live", text => CommitBinding(text, (o, v) => o.Input.MoveRightPath = v, () => Options.Input.MoveRightPath));
            Editable(input, "Look", Options.Input.LookPath, "live", text => CommitBinding(text, (o, v) => o.Input.LookPath = v, () => Options.Input.LookPath));
            Editable(input, "Interact", Options.Input.InteractPath, "live", text => CommitBinding(text, (o, v) => o.Input.InteractPath = v, () => Options.Input.InteractPath));
            Editable(input, "Regen World", Options.Input.RegenWorldPath, "live", text => CommitBinding(text, (o, v) => o.Input.RegenWorldPath = v, () => Options.Input.RegenWorldPath));
            Editable(input, "Toggle Inventory", Options.Input.ToggleInventoryPath, "live", text => CommitBinding(text, (o, v) => o.Input.ToggleInventoryPath = v, () => Options.Input.ToggleInventoryPath));
            Editable(input, "Toggle Colony", Options.Input.ToggleColonyPath, "live", text => CommitBinding(text, (o, v) => o.Input.ToggleColonyPath = v, () => Options.Input.ToggleColonyPath));
            Editable(input, "Pause", Options.Input.PausePath, "live", text => CommitBinding(text, (o, v) => o.Input.PausePath = v, () => Options.Input.PausePath));
            Editable(input, "Look Smooth", Read(() => Options.Input.LookSmoothingAlpha), "live", text => CommitFloat(text, (o, v) => o.Input.LookSmoothingAlpha = Mathf.Clamp01(v), () => Options.Input.LookSmoothingAlpha));
            Editable(input, "Number Slots", Read(() => Options.Input.NumberSlots), "live", text => CommitInt(text, (o, v) => o.Input.NumberSlots = Math.Max(1, v), () => Options.Input.NumberSlots));
            Editable(input, "Function Slots", Read(() => Options.Input.FunctionSlots), "live", text => CommitInt(text, (o, v) => o.Input.FunctionSlots = Math.Max(1, v), () => Options.Input.FunctionSlots));
        }

        // Why: timing rows separate true live-safe values from startup-only settings that would be misleading to fake-apply.
        private void BuildTiming(Transform parent)
        {
            var timing = Section(parent, "Timing", "Live-safe timings apply now. Startup and menu boot delays stay visible but read-only.");
            Editable(timing, "Fate Placeholder", Read(() => Options.WorldHost.FatePlaceholderSeconds), "live", text => CommitFloat(text, (o, v) => o.WorldHost.FatePlaceholderSeconds = Math.Max(0.1f, v), () => Options.WorldHost.FatePlaceholderSeconds));
            Editable(timing, "Fate Resolved", Read(() => Options.WorldHost.FateResolvedSeconds), "live", text => CommitFloat(text, (o, v) => o.WorldHost.FateResolvedSeconds = Math.Max(0.1f, v), () => Options.WorldHost.FateResolvedSeconds));
            Editable(timing, "Escape Hold", Read(() => Options.WorldHost.EscapeHoldQuitSeconds), "live", text => CommitFloat(text, (o, v) => o.WorldHost.EscapeHoldQuitSeconds = Math.Max(0.1f, v), () => Options.WorldHost.EscapeHoldQuitSeconds));
            Editable(timing, "Minutes / Tick", Read(() => Options.Tick.MinutesPerTick), "live", text => CommitInt(text, (o, v) => o.Tick.MinutesPerTick = Math.Max(1, v), () => (int)Options.Tick.MinutesPerTick));
            Editable(timing, "Ticks / Day", Read(() => Options.Tick.TicksPerDay), "live", text => CommitInt(text, (o, v) => o.Tick.TicksPerDay = Math.Max(1, v), () => Options.Tick.TicksPerDay));
            Editable(timing, "Ticks / Hour", Read(() => Options.Tick.TicksPerHour), "live", text => CommitInt(text, (o, v) => o.Tick.TicksPerHour = Math.Max(1, v), () => Options.Tick.TicksPerHour));
            ReadOnly(timing, "Boot Forge Wait", Read(() => Options.Boot.ForgeWaitFrames));
            ReadOnly(timing, "Boot Post Delay", Read(() => Options.Boot.PostGenerationDelayMs));
            ReadOnly(timing, "Menu Refresh", Read(() => Options.Menu.DecorationRefreshSeconds));
            ReadOnly(timing, "Menu Forge Wait", Read(() => Options.Menu.ForgeWaitFrames));
            ReadOnly(timing, "Menu Pre-Scene", Read(() => Options.Menu.PreSceneDelayMs));
            ReadOnly(timing, "Menu Ready Delay", Read(() => Options.Menu.ScenarioReadyDelayMs));
            ReadOnly(timing, "History Unlock", Read(() => Options.CharacterCreation.HistoryUnlockSeconds));
            ReadOnly(timing, "History Chars/S", Read(() => Options.CharacterCreation.HistoryCharsPerSecond));
            ReadOnly(timing, "History Line Delay", Read(() => Options.CharacterCreation.HistoryLineDelaySeconds));
            Editable(timing, "Portrait Forge Wait", Read(() => Options.CharacterCreation.PortraitForgeWaitFrames), "next portrait", text => CommitInt(text, (o, v) => o.CharacterCreation.PortraitForgeWaitFrames = Math.Max(1, v), () => Options.CharacterCreation.PortraitForgeWaitFrames));
            Editable(timing, "Portrait Timeout", Read(() => Options.CharacterCreation.PortraitForgeTimeoutSeconds), "next portrait", text => CommitFloat(text, (o, v) => o.CharacterCreation.PortraitForgeTimeoutSeconds = Math.Max(5f, v), () => Options.CharacterCreation.PortraitForgeTimeoutSeconds));
        }

        private static EmberRuntimeOptions Options => EmberRuntimeOptionsProvider.Current;

        // Why: provider.Set preserves the authoritative source while reusing the repo's normalization path.
        private static void Apply(Action<EmberRuntimeOptions> write)
        {
            var next = Options.Clone();
            write(next);
            EmberRuntimeOptionsProvider.Set(next);
        }

        private static string CommitBinding(string raw, Action<EmberRuntimeOptions, string> write, Func<string> read)
        {
            var text = string.IsNullOrWhiteSpace(raw) ? read() : raw.Trim();
            Apply(options => write(options, text));
            ReloadInputs();
            return read();
        }

        private static string CommitText(string raw, Action<EmberRuntimeOptions, string> write, Func<string> read)
        {
            var text = string.IsNullOrWhiteSpace(raw) ? read() : raw.Trim();
            Apply(options => write(options, text));
            return read();
        }

        private static string CommitInt(string raw, Action<EmberRuntimeOptions, int> write, Func<int> read)
        {
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) Apply(options => write(options, value));
            return Read(read);
        }

        private static string CommitUInt(string raw, Action<EmberRuntimeOptions, uint> write, Func<uint> read)
        {
            if (uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) Apply(options => write(options, value));
            return Read(read);
        }

        private static string CommitFloat(string raw, Action<EmberRuntimeOptions, float> write, Func<float> read)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) Apply(options => write(options, value));
            return Read(read);
        }

        private static string CommitBool(string raw, Action<EmberRuntimeOptions, bool> write, Func<bool> read)
        {
            if (bool.TryParse(raw, out var value)) Apply(options => write(options, value));
            return read().ToString();
        }

        private static string Read<T>(Func<T> read) where T : IFormattable
            => read().ToString(null, CultureInfo.InvariantCulture);

        // Why: keybind paths are baked into EmberInputActions at construction time and need a controlled rebuild.
        private static void ReloadInputs()
        {
            if (EmberInputActionsField?.GetValue(null) is IDisposable actions) actions.Dispose();
            EmberInputActionsField?.SetValue(null, null);
        }

        // Why: framed boxes reuse the host panel language instead of ad-hoc colors.
        private RectTransform Box(string name, Transform parent, bool vertical, bool pad)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            var image = go.GetComponent<Image>();
            image.color = PanelBrown;
            if (_frame != null) { image.sprite = _frame; image.type = Image.Type.Sliced; }
            if (vertical)
            {
                var layout = go.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.padding = pad ? new RectOffset(14, 14, 14, 14) : new RectOffset(0, 0, 0, 0);
                go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return rect;
        }

        // Why: labels need one place to inherit the donor font and outline treatment.
        private TMP_Text Label(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.alignment = align;
            label.color = color;
            label.raycastTarget = false;
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(0, 0, 0, 220);
            return label;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
            => Place(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax);
    }
}
