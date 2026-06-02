using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.Configuration
{
    public sealed class BootRuntimeOptions
    {
        public string NextSceneDefault { get; set; } = "MainMenu";
        public int ForgeWaitFrames { get; set; } = 180;
        public int CoreTopUpMaxEntries { get; set; } = 3;
        public int TestTopUpMaxEntries { get; set; } = 6;
        public int PostGenerationDelayMs { get; set; } = 2500;
    }

    public sealed class MenuRuntimeOptions
    {
        public string FirstSceneDefault { get; set; } = "CharacterCreation";
        public float DecorationRefreshSeconds { get; set; } = 2f;
        public int ForgeWaitFrames { get; set; } = 180;
        public int PreSceneDelayMs { get; set; } = 350;
        public int ScenarioReadyDelayMs { get; set; } = 300;
        public IReadOnlyList<string> ScenarioManifestIds { get; set; } = new[] { "dice", "skill", "new_game", "logo_" };
    }

    public sealed class WorldHostRuntimeOptions
    {
        public IReadOnlyList<string> DefaultTopics { get; set; } = new[] { "rumors", "work", "trade", "fate" };
        public float FatePlaceholderSeconds { get; set; } = 3f;
        public float FateResolvedSeconds { get; set; } = 7f;
        public float EscapeHoldQuitSeconds { get; set; } = 1f;
        public int SpellSlotCount { get; set; } = 5;
        public uint FallbackWorldSeed { get; set; } = 1u;
        public int FallbackTargetPopulation { get; set; } = 100000;
        public int FallbackRegionCount { get; set; } = 3;
        public int FallbackFactionCount { get; set; } = 3;
        public int FallbackHistoryYears { get; set; } = 50;
        public string FallbackMood { get; set; } = "grim";
        public string FallbackCalling { get; set; } = "wanderer";
        public string FallbackStart { get; set; } = "crossroads";
    }

    public sealed class InputRuntimeOptions
    {
        public string MoveUpPath { get; set; } = "<Keyboard>/w";
        public string MoveDownPath { get; set; } = "<Keyboard>/s";
        public string MoveLeftPath { get; set; } = "<Keyboard>/a";
        public string MoveRightPath { get; set; } = "<Keyboard>/d";
        public string MoveUpAltPath { get; set; } = "<Keyboard>/upArrow";
        public string MoveDownAltPath { get; set; } = "<Keyboard>/downArrow";
        public string MoveLeftAltPath { get; set; } = "<Keyboard>/leftArrow";
        public string MoveRightAltPath { get; set; } = "<Keyboard>/rightArrow";
        public string LookPath { get; set; } = "<Mouse>/delta";
        public string JumpPath { get; set; } = "<Keyboard>/space";
        public string SprintPath { get; set; } = "<Keyboard>/leftShift";
        public string InteractPath { get; set; } = "<Keyboard>/e";
        public string ToggleCursorPath { get; set; } = "<Keyboard>/f1";
        public string RegenWorldPath { get; set; } = "<Keyboard>/r";
        public string ToggleMapPath { get; set; } = "<Keyboard>/tab";
        public string ToggleColonyPath { get; set; } = "<Keyboard>/c";
        public string SaveQuickPath { get; set; } = "<Keyboard>/f5";
        public string LoadQuickPath { get; set; } = "<Keyboard>/f9";
        public string PausePath { get; set; } = "<Keyboard>/escape";
        public string AttackPath { get; set; } = "<Mouse>/leftButton";
        public string SecondaryPath { get; set; } = "<Mouse>/rightButton";
        public string MeleeSwingPath { get; set; } = "<Keyboard>/f";
        public float LookSmoothingAlpha { get; set; } = 0.5f;
        public int NumberSlots { get; set; } = 9;
        public int FunctionSlots { get; set; } = 12;
    }

    public sealed class TickRuntimeOptions
    {
        public long MinutesPerTick { get; set; } = 1L;
        // TicksPerHour/Day are DERIVED from MinutesPerTick in Normalize() (single source of truth) so the
        // schedule clock (GameTime.Hour — 60-min hours, 1440-min days) can never desync from the daily/HUD
        // counters. These defaults match MinutesPerTick=1: a real 24h day = 1440 ticks, an hour = 60 ticks.
        // They were historically 240/10, which made a "day" only 4 game-hours while the clock used 24.
        public int TicksPerDay { get; set; } = 1440;
        public int TicksPerHour { get; set; } = 60;
        public int LowStockThreshold { get; set; } = 4;
        public int HighStockThreshold { get; set; } = 64;
        public int PriceStep { get; set; } = 1;
    }

    public sealed class CombatRuntimeOptions
    {
        public float MeleeRange { get; set; } = 2f;
        public int MeleeRawDamage { get; set; } = 10;
        public int MeleeCounterDamage { get; set; } = 2;
    }

    public sealed class InteractionRuntimeOptions
    {
        public float InteractDistance { get; set; } = 5f;
        public float DofFocusDistance { get; set; } = 2f;
    }

    public sealed class CharacterCreationRuntimeOptions
    {
        public float HistoryUnlockSeconds { get; set; } = 8f;
        public float HistoryCharsPerSecond { get; set; } = 30f;
        public float HistoryLineDelaySeconds { get; set; } = 0.3f;
    }

    public sealed class EmberRuntimeOptions
    {
        public BootRuntimeOptions Boot { get; set; } = new BootRuntimeOptions();
        public MenuRuntimeOptions Menu { get; set; } = new MenuRuntimeOptions();
        public WorldHostRuntimeOptions WorldHost { get; set; } = new WorldHostRuntimeOptions();
        public InputRuntimeOptions Input { get; set; } = new InputRuntimeOptions();
        public TickRuntimeOptions Tick { get; set; } = new TickRuntimeOptions();
        public CombatRuntimeOptions Combat { get; set; } = new CombatRuntimeOptions();
        public InteractionRuntimeOptions Interaction { get; set; } = new InteractionRuntimeOptions();
        public CharacterCreationRuntimeOptions CharacterCreation { get; set; } = new CharacterCreationRuntimeOptions();

        public static EmberRuntimeOptions CreateDefault()
        {
            return new EmberRuntimeOptions();
        }

        public EmberRuntimeOptions WithMenu(MenuRuntimeOptions menu)
        {
            var clone = Clone();
            clone.Menu = menu ?? new MenuRuntimeOptions();
            return clone;
        }

        public EmberRuntimeOptions Clone()
        {
            return new EmberRuntimeOptions
            {
                Boot = new BootRuntimeOptions
                {
                    NextSceneDefault = Boot.NextSceneDefault,
                    ForgeWaitFrames = Boot.ForgeWaitFrames,
                    CoreTopUpMaxEntries = Boot.CoreTopUpMaxEntries,
                    TestTopUpMaxEntries = Boot.TestTopUpMaxEntries,
                    PostGenerationDelayMs = Boot.PostGenerationDelayMs,
                },
                Menu = new MenuRuntimeOptions
                {
                    FirstSceneDefault = Menu.FirstSceneDefault,
                    DecorationRefreshSeconds = Menu.DecorationRefreshSeconds,
                    ForgeWaitFrames = Menu.ForgeWaitFrames,
                    PreSceneDelayMs = Menu.PreSceneDelayMs,
                    ScenarioReadyDelayMs = Menu.ScenarioReadyDelayMs,
                    ScenarioManifestIds = Menu.ScenarioManifestIds == null ? Array.Empty<string>() : new List<string>(Menu.ScenarioManifestIds),
                },
                WorldHost = new WorldHostRuntimeOptions
                {
                    DefaultTopics = WorldHost.DefaultTopics == null ? Array.Empty<string>() : new List<string>(WorldHost.DefaultTopics),
                    FatePlaceholderSeconds = WorldHost.FatePlaceholderSeconds,
                    FateResolvedSeconds = WorldHost.FateResolvedSeconds,
                    EscapeHoldQuitSeconds = WorldHost.EscapeHoldQuitSeconds,
                    SpellSlotCount = WorldHost.SpellSlotCount,
                    FallbackWorldSeed = WorldHost.FallbackWorldSeed,
                    FallbackTargetPopulation = WorldHost.FallbackTargetPopulation,
                    FallbackRegionCount = WorldHost.FallbackRegionCount,
                    FallbackFactionCount = WorldHost.FallbackFactionCount,
                    FallbackHistoryYears = WorldHost.FallbackHistoryYears,
                    FallbackMood = WorldHost.FallbackMood,
                    FallbackCalling = WorldHost.FallbackCalling,
                    FallbackStart = WorldHost.FallbackStart,
                },
                Input = new InputRuntimeOptions
                {
                    MoveUpPath = Input.MoveUpPath,
                    MoveDownPath = Input.MoveDownPath,
                    MoveLeftPath = Input.MoveLeftPath,
                    MoveRightPath = Input.MoveRightPath,
                    MoveUpAltPath = Input.MoveUpAltPath,
                    MoveDownAltPath = Input.MoveDownAltPath,
                    MoveLeftAltPath = Input.MoveLeftAltPath,
                    MoveRightAltPath = Input.MoveRightAltPath,
                    LookPath = Input.LookPath,
                    JumpPath = Input.JumpPath,
                    SprintPath = Input.SprintPath,
                    InteractPath = Input.InteractPath,
                    ToggleCursorPath = Input.ToggleCursorPath,
                    RegenWorldPath = Input.RegenWorldPath,
                    ToggleMapPath = Input.ToggleMapPath,
                    ToggleColonyPath = Input.ToggleColonyPath,
                    SaveQuickPath = Input.SaveQuickPath,
                    LoadQuickPath = Input.LoadQuickPath,
                    PausePath = Input.PausePath,
                    AttackPath = Input.AttackPath,
                    SecondaryPath = Input.SecondaryPath,
                    MeleeSwingPath = Input.MeleeSwingPath,
                    LookSmoothingAlpha = Input.LookSmoothingAlpha,
                    NumberSlots = Input.NumberSlots,
                    FunctionSlots = Input.FunctionSlots,
                },
                Tick = new TickRuntimeOptions
                {
                    MinutesPerTick = Tick.MinutesPerTick,
                    TicksPerDay = Tick.TicksPerDay,
                    TicksPerHour = Tick.TicksPerHour,
                    LowStockThreshold = Tick.LowStockThreshold,
                    HighStockThreshold = Tick.HighStockThreshold,
                    PriceStep = Tick.PriceStep,
                },
                Combat = new CombatRuntimeOptions
                {
                    MeleeRange = Combat.MeleeRange,
                    MeleeRawDamage = Combat.MeleeRawDamage,
                    MeleeCounterDamage = Combat.MeleeCounterDamage,
                },
                Interaction = new InteractionRuntimeOptions
                {
                    InteractDistance = Interaction.InteractDistance,
                    DofFocusDistance = Interaction.DofFocusDistance,
                },
                CharacterCreation = new CharacterCreationRuntimeOptions
                {
                    HistoryUnlockSeconds = CharacterCreation.HistoryUnlockSeconds,
                    HistoryCharsPerSecond = CharacterCreation.HistoryCharsPerSecond,
                    HistoryLineDelaySeconds = CharacterCreation.HistoryLineDelaySeconds,
                },
            };
        }
    }

    public static class EmberRuntimeOptionsProvider
    {
        private static EmberRuntimeOptions _current = EmberRuntimeOptions.CreateDefault();
        public static EmberRuntimeOptions Current => _current;

        public static void Set(EmberRuntimeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _current = Normalize(options.Clone());
        }

        public static void ResetToDefaults()
        {
            _current = EmberRuntimeOptions.CreateDefault();
        }

        private static EmberRuntimeOptions Normalize(EmberRuntimeOptions options)
        {
            options.Tick.MinutesPerTick = Math.Max(1, options.Tick.MinutesPerTick);
            // Single source of truth: ticks-per-hour/day FOLLOW MinutesPerTick + GameTime's fixed calendar
            // (60-min hours, 1440-min days) so the schedule clock (GameTime.Hour) and the daily/HUD counters
            // can never drift apart — the historic 240/10 desync that made a "day" 4 game-hours and broke NPC
            // day/night routing. Change MinutesPerTick (or the tick interval) to retune the day length.
            options.Tick.TicksPerHour = (int)Math.Max(1L, EmberCrpg.Domain.Core.GameTime.MinutesPerHour / options.Tick.MinutesPerTick);
            options.Tick.TicksPerDay = (int)Math.Max(1L, EmberCrpg.Domain.Core.GameTime.MinutesPerDay / options.Tick.MinutesPerTick);
            options.Tick.LowStockThreshold = Math.Max(1, options.Tick.LowStockThreshold);
            options.Tick.HighStockThreshold = Math.Max(2, options.Tick.HighStockThreshold);
            options.Tick.PriceStep = Math.Max(1, options.Tick.PriceStep);

            options.WorldHost.FatePlaceholderSeconds = Math.Max(0.1f, options.WorldHost.FatePlaceholderSeconds);
            options.WorldHost.FateResolvedSeconds = Math.Max(0.1f, options.WorldHost.FateResolvedSeconds);
            options.WorldHost.EscapeHoldQuitSeconds = Math.Max(0.1f, options.WorldHost.EscapeHoldQuitSeconds);
            options.WorldHost.SpellSlotCount = Math.Max(1, options.WorldHost.SpellSlotCount);
            options.WorldHost.DefaultTopics = options.WorldHost.DefaultTopics ?? Array.Empty<string>();

            options.Interaction.InteractDistance = Math.Max(0.5f, options.Interaction.InteractDistance);
            options.Interaction.DofFocusDistance = Math.Max(0.2f, options.Interaction.DofFocusDistance);

            options.CharacterCreation.HistoryUnlockSeconds = Math.Max(0f, options.CharacterCreation.HistoryUnlockSeconds);
            options.CharacterCreation.HistoryCharsPerSecond = Math.Max(1f, options.CharacterCreation.HistoryCharsPerSecond);
            options.CharacterCreation.HistoryLineDelaySeconds = Math.Max(0f, options.CharacterCreation.HistoryLineDelaySeconds);

            options.Input.MoveUpPath = string.IsNullOrWhiteSpace(options.Input.MoveUpPath) ? "<Keyboard>/w" : options.Input.MoveUpPath;
            options.Input.MoveDownPath = string.IsNullOrWhiteSpace(options.Input.MoveDownPath) ? "<Keyboard>/s" : options.Input.MoveDownPath;
            options.Input.MoveLeftPath = string.IsNullOrWhiteSpace(options.Input.MoveLeftPath) ? "<Keyboard>/a" : options.Input.MoveLeftPath;
            options.Input.MoveRightPath = string.IsNullOrWhiteSpace(options.Input.MoveRightPath) ? "<Keyboard>/d" : options.Input.MoveRightPath;
            options.Input.LookPath = string.IsNullOrWhiteSpace(options.Input.LookPath) ? "<Mouse>/delta" : options.Input.LookPath;
            options.Input.ToggleColonyPath = string.IsNullOrWhiteSpace(options.Input.ToggleColonyPath) ? "<Keyboard>/c" : options.Input.ToggleColonyPath;
            options.Input.NumberSlots = Math.Max(1, options.Input.NumberSlots);
            options.Input.FunctionSlots = Math.Max(1, options.Input.FunctionSlots);
            options.Input.LookSmoothingAlpha = Clamp01(options.Input.LookSmoothingAlpha);
            return options;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
