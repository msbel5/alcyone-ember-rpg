using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W32 EAT slice (docs/ruh/w32/04-action-log.md §1/§2.4): the ONE seam every action phase
// transition passes through. Two tiers: every transition -> bounded ActionLogRing (cheap,
// deterministic, save-mapped); ONLY terminal outcomes -> WorldEventLog (the story surfaces
// RumorMill/history/save read). CONSTRAINT: the only caller is ActionAdvancer.TransitionTo —
// systems cannot touch the phase field directly, so "every step is logged" is structural,
// not conventional. Sinks are observers and never affect determinism.
namespace EmberCrpg.Domain.Actors.Actions
{
    /// <summary>Single gate from action phase transitions to ring, terminal events, and sinks.</summary>
    public sealed class ActionLogManager
    {
        private readonly IActionLogSink[] _sinks;

        public ActionLogManager(params IActionLogSink[] sinks)
        {
            _sinks = sinks ?? System.Array.Empty<IActionLogSink>();
        }

        public void Record(WorldState world, in ActionLogEntry entry)
        {
            world.ActionLog?.Push(entry);
            if (entry.ToPhase == ActionPhase.Failed)
                world.Events?.Append(new WorldEvent(
                    new GameTime(entry.TickMinutes), WorldEventKind.ActionFailed,
                    new ActorId(entry.ActorId), new SiteId(entry.TargetId),
                    $"{Chain(entry.FromAction)}:{Link(entry.FromAction)} failed reason={entry.Reason} target=site:{entry.TargetId} t={entry.TickMinutes}"));
            // W33: the terminal-completion event generalizes from "== ConsumeFood" to every
            // chain-final link (PlantSeed and HaulCrop end their chains); the EAT line stays
            // byte-identical — RumorMill/Gate meal counters keep reading it unchanged.
            else if (entry.ToPhase == ActionPhase.Succeeded && IsChainTerminal(entry.ToAction))
                world.Events?.Append(new WorldEvent(
                    new GameTime(entry.TickMinutes), WorldEventKind.ActionCompleted,
                    new ActorId(entry.ActorId), new SiteId(entry.TargetId),
                    $"{Chain(entry.ToAction)}:{Link(entry.ToAction)} completed target=site:{entry.TargetId} t={entry.TickMinutes}"));
            for (var i = 0; i < _sinks.Length; i++)
                _sinks[i]?.OnPhase(entry);
        }

        private static bool IsChainTerminal(ActorActionType action)
            => action == ActorActionType.ConsumeFood
            || action == ActorActionType.PlantSeed
            || action == ActorActionType.HaulCrop;

        // Enum layout truth: 1..3 are the EAT chain, 4..7 the FARM chain (append-only order).
        private static string Chain(ActorActionType action)
            => action >= ActorActionType.MoveToPlot ? "farm" : "eat";

        private static string Link(ActorActionType action) => action switch
        {
            ActorActionType.MoveToFood => "move",
            ActorActionType.TakeFood => "take",
            ActorActionType.ConsumeFood => "consume",
            ActorActionType.MoveToPlot => "move",
            ActorActionType.PlantSeed => "plant",
            ActorActionType.HarvestCrop => "harvest",
            ActorActionType.HaulCrop => "haul",
            _ => "none",
        };
    }
}
