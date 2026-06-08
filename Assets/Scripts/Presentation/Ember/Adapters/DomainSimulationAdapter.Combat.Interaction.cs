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
        public bool TryInteract(string targetTag)
        {
            // Codex audit (fourth pass A-P1): concrete interact verb. Routes
            // through GetDialogSource so the dialog panel binds to a domain-
            // backed source. Returns true when we found an actor matching the
            // tag (display name); the panel still has to be authored in the
            // scene, but the data hookup is real.
            if (string.IsNullOrEmpty(targetTag))
            {
                LogCombat("Nothing to interact with.");
                return false;
            }
            var match = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, targetTag, System.StringComparison.Ordinal));
            if (match == null) return false;
            return TryInteract(match.Id);
        }

        public bool TryInteract(ActorId actorId)
        {
            if (actorId.IsEmpty)
            {
                LogCombat("Nothing to interact with.");
                return false;
            }

            if (_world.Actors == null || !_world.Actors.TryGet(actorId, out var actor) || actor == null)
            {
                LogCombat($"No target: actor#{actorId.Value}");
                return false;
            }

            GetDialogSource(actor.Id);
            return true;
        }

    }
}
