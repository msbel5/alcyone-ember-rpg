using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.UI;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // ----- IWorldViewReadModel -----
        public IReadOnlyList<JobQueueRow> JobQueueRows
        {
            get
            {
                // Codex audit (fourth pass A-P1): previously returned empty.
                // Job sidecar state lives on the save service; expose any
                // tracked jobs. When no jobs are seeded the list stays empty
                // but the panel is no longer locked to fabricated zero rows.
                var jobs = _saveService.Jobs;
                if (jobs == null) return System.Array.Empty<JobQueueRow>();
                var rows = new List<JobQueueRow>();
                foreach (var req in jobs.Requests)
                {
                    var claim = jobs.GetClaimedBy(req.Id);
                    var actorName = claim.IsEmpty ? string.Empty : (_world.Actors.Get(claim)?.Name ?? string.Empty);
                    rows.Add(new JobQueueRow(actorName, req.Kind.ToString(), jobs.GetStatus(req.Id).Code, jobs.GetQueueIndex(req.Id)));
                }
                return rows;
            }
        }

        public IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows
        {
            get
            {
                var rows = new List<ColonyNeedsRow>();
                foreach (var actor in _world.Actors.Records.OrderBy(a => a.Id.Value))
                {
                    rows.Add(new ColonyNeedsRow(
                        actor.Name ?? string.Empty,
                        actor.Needs.Hunger.Value,
                        actor.Needs.Fatigue.Value,
                        actor.Needs.Thirst.Value,
                        actor.Mood.Value));
                }
                return rows;
            }
        }

        public IReadOnlyList<FactionRow> FactionRows
        {
            get
            {
                // Codex audit (fourth pass A-P1): previously hardcoded Neutral.
                // Use the player's faction (first non-empty actor faction id)
                // as the reference vantage point, then list every other
                // faction with its reputation relative to that vantage.
                var rows = new List<FactionRow>();
                if (_world.Factions == null) return rows;

                // Codex audit (seventh pass A-P2 #7): previously emitted every
                // faction at reputation 0 / Neutral, hiding the real diplomacy
                // state held in FactionStore.ReputationRows. Use the FIRST
                // authored faction as the deterministic player vantage and
                // emit each OTHER faction with its real reputation relative
                // to the vantage; the vantage itself is reported at 0 so the
                // HUD still includes the player's home faction. When
                // ActorRecord.FactionId lands the vantage will switch to the
                // player's actual home faction.
                FactionId vantage = default;
                foreach (var faction in _world.Factions.Records)
                {
                    vantage = faction.Id;
                    break;
                }
                foreach (var faction in _world.Factions.Records)
                {
                    int reputation = 0;
                    if (!faction.Id.Equals(vantage))
                    {
                        reputation = _world.Factions.GetReputation(vantage, faction.Id).Value;
                    }
                    var label = FactionRelationKind.FromReputation(reputation).ToString();
                    rows.Add(new FactionRow(faction.Name ?? string.Empty, reputation, label));
                }
                return rows;
            }
        }

        public IReadOnlyList<InventorySlot> InventorySlots
        {
            get
            {
                if (_world.PlayerInventory == null) return System.Array.Empty<InventorySlot>();
                var rows = new List<InventorySlot>();
                foreach (var item in _world.PlayerInventory.Items)
                {
                    rows.Add(new InventorySlot(item.TemplateId ?? string.Empty, item.Quantity));
                }
                return rows;
            }
        }

        public IReadOnlyList<string> SpellSlots
        {
            get
            {
                if (_world.PlayerKnownSpellIds != null && _world.PlayerKnownSpellIds.Count > 0)
                    return _world.PlayerKnownSpellIds;

                return EmberCrpg.Simulation.Magic.WorldSpellCatalog.All.Select(s => s.TemplateId).ToList();
            }
        }

    }
}
