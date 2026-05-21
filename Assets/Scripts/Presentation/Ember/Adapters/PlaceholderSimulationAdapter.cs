using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Self-contained adapter that fabricates deterministic snapshots so the view layer
    /// can be visually verified without a wired domain. Replace via
    /// <see cref="EmberDomainAdapterLocator.Register"/> once Captain's domain stores
    /// expose an integration adapter.
    /// </summary>
    public sealed class PlaceholderSimulationAdapter : IDomainSimulationAdapter, IDialogSource
    {
        private readonly List<JobQueueRow> _jobRows = new List<JobQueueRow>();
        private readonly List<ColonyNeedsRow> _needsRows = new List<ColonyNeedsRow>();
        private readonly List<FactionRow> _factionRows = new List<FactionRow>();
        private readonly List<InventorySlot> _inventorySlots = new List<InventorySlot>();
        private readonly List<string> _spellSlots = new List<string> { "fireball", "heal", "shield", "teleport", "drain" };
        private readonly Dictionary<string, ActorViewState> _actorStates = new Dictionary<string, ActorViewState>();
        private readonly Dictionary<string, WorksiteViewState> _worksiteStates = new Dictionary<string, WorksiteViewState>();

        private int _tick;
        private CombatHudState _combatHud = new CombatHudState(95, 100, 80, 100, 60, 100, "—");
        private string _hudText = "Tick 0   Day 1   Spring";

        private string _currentDialogLine = "Greetings traveler.";
        private List<string> _currentTopics = new List<string> { "Jobs", "Factions", "Rumors" };

        public int TickIndex => _tick;
        public string HudText => _hudText;
        public IReadOnlyList<JobQueueRow> JobQueueRows => _jobRows;
        public IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows => _needsRows;
        public IReadOnlyList<FactionRow> FactionRows => _factionRows;
        public IReadOnlyList<InventorySlot> InventorySlots => _inventorySlots;
        public IReadOnlyList<string> SpellSlots => _spellSlots;
        public CombatHudState CombatHud => _combatHud;

        public void AdvanceTick(int tickIndex)
        {
            _tick = tickIndex;
            UpdateHud();
            UpdateJobs();
            UpdateNeeds();
            UpdateFactions();
            UpdateInventory();
            UpdateCombat();
        }

        public bool TryReadActor(string actorName, out ActorViewState state) =>
            _actorStates.TryGetValue(actorName, out state);

        public bool TryReadWorksite(string siteName, out WorksiteViewState state) =>
            _worksiteStates.TryGetValue(siteName, out state);

        public IDialogSource GetDialogSource(string actorName)
        {
            _currentDialogLine = $"Greetings, I am {actorName}. What brings you here?";
            return this;
        }

        public string GetCurrentLine() => _currentDialogLine;
        public IReadOnlyList<string> GetTopics() => _currentTopics;
        public void SelectTopic(string topicId)
        {
            _currentDialogLine = $"You asked about {topicId}. It is a complex matter indeed.";
        }

        public void LogCombat(string message)
        {
            _combatHud = new CombatHudState(
                _combatHud.Health, _combatHud.HealthMax,
                _combatHud.Stamina, _combatHud.StaminaMax,
                _combatHud.Mana, _combatHud.ManaMax,
                message);
        }

        public void TakePlayerDamage(int amount)
        {
            _combatHud = new CombatHudState(
                Mathf.Max(0, _combatHud.Health - amount), _combatHud.HealthMax,
                _combatHud.Stamina, _combatHud.StaminaMax,
                _combatHud.Mana, _combatHud.ManaMax,
                $"You take {amount} damage!");
        }

        // Codex audit Batch 2 / Finding 3 — placeholder bridge.
        // The placeholder has no real domain state to round-trip. Stash only the
        // tick + HUD line so the save service still observes save/load lifecycle
        // events; a real adapter will overwrite both methods with a full domain
        // snapshot. The format is intentionally version-stamped so a future
        // domain adapter can detect placeholder envelopes and discard them.
        public string ExportStateJson()
        {
            // Codex audit (second pass A-P3): previously only `"` was escaped in
            // the HUD payload, so a HUD line containing a backslash or a
            // control byte (e.g. an embedded newline / tab from a debug build)
            // produced malformed JSON that downstream RestoreStateJson silently
            // dropped. EscapeJsonString handles `"`, `\`, `/`, the four
            // standard control-letter escapes, and everything below 0x20 as
            // `\uXXXX`.
            return "{\"version\":\"placeholder-v1\",\"tick\":" + _tick + ",\"hud\":\"" + EscapeJsonString(_hudText ?? string.Empty) + "\"}";
        }

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new System.Text.StringBuilder(value.Length + 8);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public void RestoreStateJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            // Extract the tick number with a deterministic substring scan; we
            // intentionally avoid JsonUtility here because the placeholder
            // envelope must survive without a Unity dependency.
            const string key = "\"tick\":";
            var idx = json.IndexOf(key, System.StringComparison.Ordinal);
            if (idx < 0) return;
            var start = idx + key.Length;
            var end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
                end++;
            if (end <= start) return;
            if (int.TryParse(json.Substring(start, end - start), out var restoredTick))
                AdvanceTick(restoredTick);
        }

        public string ConsultFate()
        {
            // Codex review (2026-05-21): the placeholder advertises itself as
            // "deterministic" but Random.Range made the roll wall-clock dependent,
            // breaking replay. Derive from the running tick with a fixed salt so
            // the same tick yields the same bucket. 33/33/34 distribution matches
            // the original thresholds.
            int salted = unchecked(_tick * (int)2654435761);
            int roll = (salted & 0x7fffffff) % 100 + 1;
            if (roll <= 33) return "SETBACK: The stars align against you. A cold wind blows.";
            if (roll <= 66) return "NEUTRAL: The path is unclear. The DM watches in silence.";
            return "FAVOURABLE: Fortune smiles upon your endeavor.";
        }

        private void UpdateHud()
{
            var day = 1 + _tick / 240;
            var season = SeasonOf(day);
            var weather = WeatherOf(_tick);
            _hudText = $"Tick {_tick:0000}   Day {day:000}   {season}   {weather}";
        }

        private void UpdateJobs()
        {
            _jobRows.Clear();
            _jobRows.Add(new JobQueueRow("Smith_A",   "smith", (_tick % 16) < 8 ? "active" : "queued", 0));
            _jobRows.Add(new JobQueueRow("Smith_B",   "smith", (_tick % 16) < 8 ? "queued" : "active", 1));
            _jobRows.Add(new JobQueueRow("Farmer",    "field", (_tick % 24) < 12 ? "traveling" : "active", 0));
            _jobRows.Add(new JobQueueRow("Innkeeper", "cook",  "active", 0));
            _jobRows.Add(new JobQueueRow("Merchant",  "trade", (_tick % 30) < 15 ? "idle" : "active", 0));
        }

        private void UpdateNeeds()
        {
            _needsRows.Clear();
            _needsRows.Add(new ColonyNeedsRow("Innkeeper", Bias(_tick, 18,  1, 0.5f), Bias(_tick, 12, 1, 0.4f), Bias(_tick, 10, 1, 0.3f), 78 - Bias(_tick, 0, 0, 0.25f)));
            _needsRows.Add(new ColonyNeedsRow("Beggar",    Bias(_tick, 55,  2, 1.0f), Bias(_tick, 30, 2, 0.8f), Bias(_tick, 40, 1, 0.6f), 42 - Bias(_tick, 0, 0, 0.5f)));
            _needsRows.Add(new ColonyNeedsRow("Guard",     Bias(_tick, 15,  1, 0.4f), Bias(_tick, 20, 1, 0.5f), Bias(_tick, 10, 1, 0.3f), 85 - Bias(_tick, 0, 0, 0.2f)));
            _needsRows.Add(new ColonyNeedsRow("Smith_A",   Bias(_tick, 24,  1, 0.6f), Bias(_tick, 28, 1, 0.7f), Bias(_tick, 14, 1, 0.4f), 70 - Bias(_tick, 0, 0, 0.3f)));
            _needsRows.Add(new ColonyNeedsRow("Smith_B",   Bias(_tick, 22,  1, 0.6f), Bias(_tick, 30, 1, 0.7f), Bias(_tick, 12, 1, 0.4f), 72 - Bias(_tick, 0, 0, 0.3f)));
        }

        private void UpdateFactions()
        {
            _factionRows.Clear();
            _factionRows.Add(new FactionRow("Merchants Guild", 32 + (_tick / 60) % 12, "Friendly"));
            _factionRows.Add(new FactionRow("City Watch",      18,                     "Neutral"));
            _factionRows.Add(new FactionRow("Mages Circle",   -8 - (_tick / 120) % 6,  "Wary"));
            _factionRows.Add(new FactionRow("Bandit Lords",  -40,                      "Hostile"));
            _factionRows.Add(new FactionRow("Temple of Light", 12 + (_tick / 90) % 8,  "Cordial"));
        }

        private void UpdateInventory()
        {
            _inventorySlots.Clear();
            _inventorySlots.Add(new InventorySlot("iron_ingot",       4));
            _inventorySlots.Add(new InventorySlot("steel_longsword",  1));
            _inventorySlots.Add(new InventorySlot("leather_armor",    1));
            _inventorySlots.Add(new InventorySlot("bread",            6));
            _inventorySlots.Add(new InventorySlot("waterskin",        2));
            _inventorySlots.Add(new InventorySlot("healing_potion",   3));
            _inventorySlots.Add(new InventorySlot("torch",            5));
            _inventorySlots.Add(new InventorySlot("iron_shield",      1));
        }

        private void UpdateCombat()
        {
            var pulse = Mathf.Sin(_tick * 0.05f);
            var health = Mathf.Clamp(85 + (int)(pulse * 10f), 0, 100);
            var stamina = Mathf.Clamp(60 + (int)(pulse * 15f), 0, 100);
            var mana = Mathf.Clamp(55 + (int)(pulse * 12f), 0, 100);
            _combatHud = new CombatHudState(health, 100, stamina, 100, mana, 100, ComposeCombatLine(_tick));
        }

        private static string ComposeCombatLine(int tick)
        {
            var step = (tick / 30) % 4;
            switch (step)
            {
                case 0: return "You strike the goblin for 6 damage.";
                case 1: return "Goblin scout shrieks and circles you.";
                case 2: return "You parry the bandit's blade.";
                default: return "Bandit lord raises a torch and roars.";
            }
        }

        private static string SeasonOf(int day) => (((day - 1) / 30) % 4) switch
        {
            0 => "Spring",
            1 => "Summer",
            2 => "Autumn",
            _ => "Winter",
        };

        private static string WeatherOf(int tick) => ((tick / 120) % 4) switch
        {
            0 => "Calm",
            1 => "Light Rain",
            2 => "Overcast",
            _ => "Wind",
        };

        private static int Bias(int tick, int baseValue, int slope, float multiplier) =>
            Mathf.Clamp(baseValue + (int)(slope * multiplier * tick), 0, 100);
    }
}
