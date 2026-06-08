using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Data.Quests;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Quest;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using EmberCrpg.Presentation.Ember.Worldgen;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        private void HydrateHistory(EmberCrpg.Simulation.Worldgen.GeneratedWorld generated)
        {
            if (_world.Events == null) _world.Events = new WorldEventLog();
            var fallbackSite = StartingSettlement.IsEmpty ? FirstSiteId() : SettlementSiteId(StartingSettlement);
            var minYear = generated.History.Count == 0 ? 0 : generated.History.Min(history => history.Year);
            foreach (var history in generated.History)
            {
                _world.Events.Append(new WorldEvent(
                    new GameTime((long)(history.Year - minYear) * GameTime.MinutesPerYear),
                    ToRuntimeEventKind(history.Kind),
                    default,
                    fallbackSite,
                    history.Detail));
            }
        }

        private void MovePlayerToStartingSettlement()
        {
            if (StartingSettlement.IsEmpty || _world.Actors == null) return;
            if (!_world.Actors.TryFirstByRole(ActorRole.Player, out var player) || player == null) return;
            player.MoveTo(CenterOfSite(SettlementSiteId(StartingSettlement)));
        }

        private GridPosition CenterOfSite(SiteId siteId)
        {
            if (_world.Sites != null && _world.Sites.TryGet(siteId, out var site))
                return CenterOf(site);
            return default;
        }

        private SiteId FirstSiteId()
        {
            if (_world.Sites != null)
            {
                foreach (var site in _world.Sites.Records)
                    return site.Id;
            }
            return new SiteId(1UL);
        }

        private static SiteId RegionSiteId(RegionId id) => new SiteId(RegionSiteOffset + id.Value);
        private static SiteId SettlementSiteId(SettlementId id) => new SiteId(SettlementSiteOffset + id.Value);

    }
}
