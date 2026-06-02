using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI;
using WorldEventNarrator = EmberCrpg.Presentation.Visual.WorldEventNarrator;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>Pattern: projector. Why: keep world-state-to-scene sync out of the lifecycle host.</summary>
    public sealed class WorldViewProjector
    {
        private readonly IEmberSimulationClock _clock;
        private readonly IWorldViewReadModel _worldView;
        private readonly ActorView[] _actorViews;
        private readonly WorksiteView[] _worksiteViews;
        private readonly EventLogHudPanel _eventLogHud;
        private readonly WorldEventNarrator _eventNarrator = new WorldEventNarrator();

        public WorldViewProjector(
            IEmberSimulationClock clock,
            IWorldViewReadModel worldView,
            ActorView[] actorViews,
            WorksiteView[] worksiteViews,
            EventLogHudPanel eventLogHud)
        {
            _clock = clock;
            _worldView = worldView;
            _actorViews = actorViews ?? System.Array.Empty<ActorView>();
            _worksiteViews = worksiteViews ?? System.Array.Empty<WorksiteView>();
            _eventLogHud = eventLogHud;
        }

        // Why: reuse the host's pre-existing world-to-scene projection without advancing simulation time.
        public void Project()
        {
            for (int i = 0; i < _actorViews.Length; i++)
            {
                var actor = _actorViews[i];
                ActorViewState state;
                bool resolved = actor.HasDomainActorId
                    ? _worldView.TryReadActor(actor.DomainActorId, out state)
                    : _worldView.TryReadActor(actor.DomainActorKey, out state);
                if (resolved)
                    actor.SetTarget(state);
            }

            for (int i = 0; i < _worksiteViews.Length; i++)
            {
                var worksite = _worksiteViews[i];
                if (_worldView.TryReadWorksite(worksite.name, out var state))
                    worksite.SetState(state);
            }
        }

        // Why: preserve the old tick order exactly: advance, sync scene views, then render the event log.
        public void ProjectTick(int tickIndex)
        {
            _clock.AdvanceTick(tickIndex);
            Project();
            _eventLogHud?.Render(_worldView.RecentWorldEvents(64), _eventNarrator);
        }
    }
}
