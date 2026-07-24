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
            // CONSTRAINT: unknown kind NEVER falls back to a guess — loud sentinel + one warn.
            _ => Unknown(kind)
        };

        /// <summary>Stable kind string for ActorViewState.ActionKind; null when the actor carries no action.</summary>
        public static string KindName(ActorActionType kind) => kind switch
        {
            ActorActionType.MoveToFood => "MoveToFood",
            ActorActionType.TakeFood => "TakeFood",
            ActorActionType.ConsumeFood => "ConsumeFood",
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
