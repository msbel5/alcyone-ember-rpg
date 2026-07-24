using EmberCrpg.Domain.Actors;

// Design note:
// W32-03 §3: type-keyed advancer lookup. Registration order cannot affect outcomes (lookup by
// ActorActionType), so determinism is structural. New verb = 1 enum member + 1 advancer + 1 line.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Fixed ActorActionType -&gt; advancer table.</summary>
    public sealed class ActionAdvancerRegistry
    {
        private readonly ActionAdvancer[] _byKind;

        public ActionAdvancerRegistry(params ActionAdvancer[] advancers)
        {
            _byKind = new ActionAdvancer[(int)ActorActionType.ConsumeFood + 1];
            foreach (var advancer in advancers)
                _byKind[(int)advancer.Handles] = advancer;
        }

        public ActionAdvancer For(ActorActionType kind) => _byKind[(int)kind];
    }
}
