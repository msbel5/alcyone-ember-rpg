using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // Ninth-pass FOUNDATION worldgen: SeedWorld now runs the deterministic
// WorldgenService (Assets/Scripts/Simulation/Worldgen/) so the
        // mood/calling/start tuple from the main-menu wizard actually
        // produces a ~50-region, ~200-settlement, ~750-NPC world instead
        // of vanishing into a log line. The generated bundle is held on
        // the adapter so subsequent reads (UI panels, save/load) can
        // inspect it through the IDomainSimulationAdapter handle.
        public EmberCrpg.Simulation.Worldgen.GeneratedWorld GeneratedWorld { get; private set; }

        /// <summary>The starting region selected from the wizard's start-location string. Empty when no world has been seeded.</summary>
        public RegionId StartingRegion { get; private set; }
        public SettlementId StartingSettlement { get; private set; }
        public FactionId StartingFaction { get; private set; }


    }
}
