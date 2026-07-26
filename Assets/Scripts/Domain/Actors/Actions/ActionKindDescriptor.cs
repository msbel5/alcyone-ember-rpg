// Design note:
// The ONE row per ActorActionType. Adding a value used to require touching four parallel
// switches (Verb + KindName in Presentation, Chain + Link + IsChainTerminal in the log
// manager) — the W34 additions (Sleep, PerformWork) proved this fragile: IsChainTerminal
// missed them (no ActionCompleted event fired) and Chain's ordinal-range check misfiled
// them as "farm". The descriptor array is now the ONE exhaustiveness gate: unlisted values
// fall through to the sentinel row and the runtime tools warn.
// CONSTRAINT: pure static data — no clock, no position, no needs. Do NOT localise verbs
// here (a future i18n layer plugs in above the descriptor via a resource id).
namespace EmberCrpg.Domain.Actors.Actions
{
    /// <summary>Per-ActorActionType metadata: verb (screen label), kind name (stable id),
    /// chain family, link name within the chain, and terminal-of-chain flag.</summary>
    public readonly struct ActionKindDescriptor
    {
        public ActionKindDescriptor(string verb, string kindName, string chainName, string linkName, bool isChainTerminal)
        {
            Verb = verb;
            KindName = kindName;
            ChainName = chainName;
            LinkName = linkName;
            IsChainTerminal = isChainTerminal;
        }

        /// <summary>On-screen activity verb (RUH_TESHIS §10 "label == CurrentAction").</summary>
        public string Verb { get; }
        /// <summary>Stable, projection-friendly name for ActorViewState.ActionKind; null for None.</summary>
        public string KindName { get; }
        /// <summary>"eat" | "farm" | "sleep" | "work" — used in ActionLog messages.</summary>
        public string ChainName { get; }
        /// <summary>"move" | "take" | "consume" | "plant" | "harvest" | "haul" | "sleep" | "work".</summary>
        public string LinkName { get; }
        /// <summary>True when a Succeeded transition should append WorldEventKind.ActionCompleted.</summary>
        public bool IsChainTerminal { get; }
    }

    /// <summary>Descriptor lookup indexed by (int)ActorActionType. The ONE registry — a
    /// missing row surfaces as the sentinel None descriptor so the caller can log/warn.</summary>
    public static class ActionKindDescriptors
    {
        private static readonly ActionKindDescriptor None      = new ActionKindDescriptor(null, null, "none", "none", false);

        // Rows in enum-declaration order. Adding a value = append here + extend the switch.
        private static readonly ActionKindDescriptor[] _rows = new[]
        {
            None,                                                                                                // 0 None
            new ActionKindDescriptor("seeking food", "MoveToFood",     "eat",   "move",    false),               // 1
            new ActionKindDescriptor("taking food",  "TakeFood",       "eat",   "take",    false),               // 2
            new ActionKindDescriptor("eating",       "ConsumeFood",    "eat",   "consume", true),                // 3
            new ActionKindDescriptor("to the field", "MoveToPlot",     "farm",  "move",    false),               // 4
            new ActionKindDescriptor("planting",     "PlantSeed",      "farm",  "plant",   true),                // 5
            new ActionKindDescriptor("harvesting",   "HarvestCrop",    "farm",  "harvest", false),               // 6
            new ActionKindDescriptor("hauling",      "HaulCrop",       "farm",  "haul",    true),                // 7
            new ActionKindDescriptor("heading home", "MoveToBed",      "sleep", "move",    false),               // 8
            new ActionKindDescriptor("sleeping",     "Sleep",          "sleep", "sleep",   true),                // 9
            new ActionKindDescriptor("to work",      "MoveToWorksite", "work",  "move",    false),               // 10
            new ActionKindDescriptor("working",      "PerformWork",    "work",  "work",    true),                // 11
            // W36 GUARD+COMBAT slice: guard beat + enemy approach->strike loop.
            // OnWatch is terminal (arrival succeeds, chain ends Idle); StrikeQuarry is NOT terminal
            // when the target survives — NextLink loops it back to Hunt for another approach.
            new ActionKindDescriptor("on watch",     "OnWatch",        "watch", "watch",   true),                // 12
            new ActionKindDescriptor("hunting",      "Hunt",           "hunt",  "move",    false),               // 13
            new ActionKindDescriptor("striking",     "StrikeQuarry",   "hunt",  "strike",  false),               // 14
        };

        /// <summary>Descriptor for `kind`, or the sentinel None descriptor when out of range.</summary>
        public static ActionKindDescriptor Get(ActorActionType kind)
        {
            int i = (int)kind;
            return (uint)i < (uint)_rows.Length ? _rows[i] : None;
        }
    }
}
