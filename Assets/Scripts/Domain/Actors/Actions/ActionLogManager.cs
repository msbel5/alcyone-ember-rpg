using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W32 EAT slice (docs/ruh/w32/04-action-log.md §1/§2.4): the ONE seam every action phase
// transition passes through. Two tiers: every transition -> bounded ActionLogRing (cheap,
// deterministic, save-mapped); ONLY terminal outcomes -> WorldEventLog (the story surfaces
// RumorMill/history/save read). CONSTRAINT: the only caller is ActionAdvancer.TransitionTo —
// systems cannot touch the phase field directly, so "every step is logged" is structural,
// not conventional. Sinks are observers and never affect determinism.
// W-refactor 2026-07-26: Chain/Link/IsChainTerminal delegate to ActionKindDescriptors so
// adding a new ActorActionType is ONE row in the descriptor table (not four scattered
// switches). Fixed: Sleep + PerformWork silently missed IsChainTerminal + the Chain
// ordinal-range check misfiled them as "farm".
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
            {
                var d = ActionKindDescriptors.Get(entry.FromAction);
                world.Events?.Append(new WorldEvent(
                    new GameTime(entry.TickMinutes), WorldEventKind.ActionFailed,
                    new ActorId(entry.ActorId), new SiteId(entry.TargetId),
                    $"{d.ChainName}:{d.LinkName} failed reason={entry.Reason} target=site:{entry.TargetId} t={entry.TickMinutes}"));
            }
            // The terminal-completion event fires whenever a chain-final link succeeds — the
            // "consume/plant/haul/sleep/work" set is single-sourced from ActionKindDescriptors
            // so a new terminal action only needs its descriptor row's IsChainTerminal flag set.
            else if (entry.ToPhase == ActionPhase.Succeeded)
            {
                var d = ActionKindDescriptors.Get(entry.ToAction);
                if (d.IsChainTerminal)
                    world.Events?.Append(new WorldEvent(
                        new GameTime(entry.TickMinutes), WorldEventKind.ActionCompleted,
                        new ActorId(entry.ActorId), new SiteId(entry.TargetId),
                        $"{d.ChainName}:{d.LinkName} completed target=site:{entry.TargetId} t={entry.TickMinutes}"));
            }
            for (var i = 0; i < _sinks.Length; i++)
                _sinks[i]?.OnPhase(entry);
        }
    }
}
