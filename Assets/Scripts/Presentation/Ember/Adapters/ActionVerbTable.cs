using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.Diagnostics;

// Design note:
// W32 DOC5 §3.1: the ONE translation table from action identity to the on-screen verb.
// CONSTRAINT: pure static data — no clock, no position, no needs. A verb may ONLY be
// derived from the action type. Adding an hour/position input here recreates RUH_TESHIS §2.9.
// New verb = new action type + one row here (never a new guess branch in the projection).
namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>Presentation dictionary: ActorActionType -> verbatim activity verb.</summary>
    // Public (not internal): ActivityLabelTruthTests pins the table from the test assembly —
    // the RUH_TESHIS §10 "label == CurrentAction" contract needs a callable surface.
    public static class ActionVerbTable
    {
        public static string Verb(ActorActionType kind) => kind switch
        {
            ActorActionType.MoveToFood => "seeking food",
            ActorActionType.TakeFood => "taking food",
            ActorActionType.ConsumeFood => "eating",
            // W33 FARM: real actions now own the field verbs the projection used to guess.
            ActorActionType.MoveToPlot => "to the field",
            ActorActionType.PlantSeed => "planting",
            ActorActionType.HarvestCrop => "harvesting",
            ActorActionType.HaulCrop => "hauling",
            // W34 SLEEP: the night verbs' owners are real actions now — the projection's
            // hour+position guesses died; the on-screen words stay VERBATIM (playtest continuity).
            ActorActionType.MoveToBed => "heading home",
            ActorActionType.Sleep => "sleeping",
            // W34 WORK: the bench verb is REAL for the first time — projection guesswork's
            // last castle falls (RUH_TESHIS §2.9); the label IS CurrentAction, verbatim.
            ActorActionType.MoveToWorksite => "to work",
            ActorActionType.PerformWork => "working",
            // CONSTRAINT: unknown kind NEVER falls back to a guess — loud sentinel + one warn.
            _ => Unknown(kind)
        };

        /// <summary>Stable kind string for ActorViewState.ActionKind; null when the actor carries no action.</summary>
        public static string KindName(ActorActionType kind) => kind switch
        {
            ActorActionType.MoveToFood => "MoveToFood",
            ActorActionType.TakeFood => "TakeFood",
            ActorActionType.ConsumeFood => "ConsumeFood",
            ActorActionType.MoveToPlot => "MoveToPlot",
            ActorActionType.PlantSeed => "PlantSeed",
            ActorActionType.HarvestCrop => "HarvestCrop",
            ActorActionType.HaulCrop => "HaulCrop",
            ActorActionType.MoveToBed => "MoveToBed",
            ActorActionType.Sleep => "Sleep",
            ActorActionType.MoveToWorksite => "MoveToWorksite",
            ActorActionType.PerformWork => "PerformWork",
            _ => null
        };

        private static readonly EmberLogger Log = EmberLog.For("projection");
        private static readonly HashSet<ActorActionType> _warned = new HashSet<ActorActionType>(); // presentation-only state

        private static string Unknown(ActorActionType kind)
        {
            if (_warned.Add(kind))
                Log.Warn($"no verb for action kind '{kind}'");
            return "(" + kind + ")"; // stays visible on screen so a missing row is caught in playtest
        }
    }
}
