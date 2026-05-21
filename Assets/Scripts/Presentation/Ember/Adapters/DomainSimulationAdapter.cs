using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Codex audit (fourth pass A-P1): the adapter previously inherited the
    /// default no-op implementations of <see cref="IPlayerCommandSink"/>'s
    /// TryCastSpell / TryMeleeStrike / TryInteract, so player input only ever
    /// logged text — combat / spells / dialog never mutated world state. It
    /// also fabricated FactionRows ("Neutral") and read SpellSlots off the
    /// cooldown tracker (empty on a fresh world). This rewrite:
    ///
    /// 1. implements each command concretely against SliceWorldState
    ///    (vitals damage, mana/cooldown gate via SpellResolver, dialog topic
    ///    routing);
    /// 2. reads FactionRows from <see cref="FactionStore.ReputationRows"/>
    ///    relative to the player faction;
    /// 3. exposes a known-spell list from <see cref="EmberCrpg.Simulation.Magic.SliceSpellCatalog.All"/>
    ///    instead of mining the cooldown state;
    /// 4. mutates the player <see cref="ActorRecord"/>'s vitals on
    ///    TakePlayerDamage instead of holding a UI-only counter;
    /// 5. retains a single <see cref="EmberCrpg.Presentation.Ember.Save.JsonSliceSaveService"/>
    ///    so worksite/job/soil/plant process sidecars survive the
    ///    Export/Restore round-trip.
    /// </summary>
    public sealed class DomainSimulationAdapter : IDomainSimulationAdapter, IDialogSourcePortrait
    {
        private readonly SliceWorldState _world;
        private readonly EmberCrpg.Presentation.Ember.Save.JsonSliceSaveService _saveService;
        private readonly EmberCrpg.Simulation.Composition.SliceTickComposer _tickComposer;
        private int _tick;
        private string _lastCombatLine = string.Empty;
        private string _activeDialogActor = string.Empty;
        private string _currentDialogLine = string.Empty;
        private string _currentPortrait = "portrait_npc_placeholder";

        public DomainSimulationAdapter(SliceWorldState world)
{
            _world = world ?? throw new System.ArgumentNullException(nameof(world));
            // Codex audit (fourth pass A-P2): retain ONE save service so the
            // sidecar process state (worksites / jobs / soils / plants) lives
            // across Export/Restore cycles. Previously a fresh service was
            // constructed each call, dropping the sidecar.
            // Codex audit (sixth pass A-P1 #6): supply a recipe resolver so
            // LoadFromJson does not throw on saves that carry active recipe
            // work orders. ProductionRecipeRegistry.Resolve is the canonical
            // catalog lookup. If the project later adds catalog scopes per
            // scene, this seam swaps to a scene-bound resolver.
            _saveService = new EmberCrpg.Presentation.Ember.Save.JsonSliceSaveService(
                EmberCrpg.Data.Recipes.ProductionRecipeRegistry.Resolve);
            // Codex audit (sixth pass A-P0 #1): wire the per-tick composer so
            // the live game's simulation actually moves forward.
            _tickComposer = new EmberCrpg.Simulation.Composition.SliceTickComposer();

            // Codex audit (seventh pass A-P2 #5): SliceWorldFactory authors
            // `_world.Sites` (SiteRecord definitions) but the runtime
            // WorksiteStore is empty until a save loads — so TryReadWorksite
            // returned `isActive=false` even when the scene had worksites
            // authored. Seed the WorksiteStore with one Active record per
            // authored Site so the HUD and view layer immediately see them.
            // Worksite kind defaults to Generic for now; specific kinds are
            // assigned by the recipe system as work orders attach.
            if (_saveService.Worksites != null && _world.Sites != null)
            {
                foreach (var site in _world.Sites.Records)
                {
                    // Skip if a record with the same SiteId+Position already
                    // exists (idempotent re-seed safe).
                    bool exists = false;
                    foreach (var record in _saveService.Worksites.Records)
                    {
                        var position = CenterOf(site);
                        if (record.SiteId.Equals(site.Id) && record.Position.Equals(position))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists) continue;
                    _saveService.Worksites.Add(new EmberCrpg.Domain.Process.WorksiteRecord(
                        site.Id, CenterOf(site), WorksiteKindFor(site.Name), isActive: true));
                }
            }
        }

        // ----- IEmberSimulationClock -----
        public void AdvanceTick(int tickIndex)
        {
            _tick = tickIndex;
            _tickComposer.Advance(_world, tickIndex);
        }
        public int TickIndex => _tick;

        // ----- IEmberHudReadModel -----
        public string HudText
        {
            get
            {
                var day = 1 + _tick / 240;
                return $"Tick {_tick:0000}   Day {day:000}";
            }
        }

        public CombatHudState CombatHud
        {
            get
            {
                var player = _world.Actors.FirstByRole(ActorRole.Player);
                if (player == null) return new CombatHudState(0, 100, 0, 100, 0, 100, _lastCombatLine);
                var v = player.Vitals;
                return new CombatHudState(
                    v.Health.Current, v.Health.Max,
                    v.Fatigue.Current, v.Fatigue.Max,
                    v.Mana.Current, v.Mana.Max,
                    _lastCombatLine);
            }
        }

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
                // Codex audit (fourth pass A-P2): the cooldown tracker only
                // contains spells the actor has CAST (which seeds nothing on
                // a fresh world). The known-spell catalog is the right source.
                //
                // Codex review on PR #196 (P1): MUST preserve catalog index
                // order so slot N in the HUD matches slot N in TryCastSpell.
                // Previously this method sorted alphabetically (flame_bolt /
                // mending_touch / ember_ward becomes ember_ward / flame_bolt /
                // mending_touch), but TryCastSpell still resolved by raw
                // `SliceSpellCatalog.All[index]`, so pressing slot 0 would
                // cast a different spell than the one displayed.
                return EmberCrpg.Simulation.Magic.SliceSpellCatalog.All
                    .Select(s => s.TemplateId)
                    .ToList();
            }
        }

        public bool TryReadActor(string actorName, out ActorViewState state)
        {
            state = default;
            if (string.IsNullOrEmpty(actorName)) return false;
            foreach (var actor in _world.Actors.Records)
            {
                if (string.Equals(actor.Name, actorName, System.StringComparison.Ordinal))
                {
                    state = new ActorViewState(
                        new UnityEngine.Vector3(actor.Position.X, 0f, actor.Position.Y),
                        UnityEngine.Quaternion.identity,
                        visible: true);
                    return true;
                }
            }
            return false;
        }

        public bool TryReadWorksite(string siteName, out WorksiteViewState state)
        {
            state = default;
            // Codex audit (fifth pass A-P1): previously returned the
            // synthetic `(isActive: true, queueDepth: 0)` for any site
            // name match — the view never reflected the actual worksite
            // store. Now derive isActive from the WorksiteStore and
            // queueDepth from the JobBoard's request count at that site.
            if (string.IsNullOrEmpty(siteName)) return false;
            EmberCrpg.Domain.Core.SiteId siteId = default;
            foreach (var site in _world.Sites.Records)
            {
                if (string.Equals(site.Name, siteName, System.StringComparison.Ordinal))
                {
                    siteId = site.Id;
                    break;
                }
            }
            if (siteId.IsEmpty) return false;

            var worksites = _saveService.Worksites;
            bool isActive = false;
            if (worksites != null)
            {
                foreach (var record in worksites.Records)
                {
                    if (record.SiteId.Equals(siteId) && record.IsActive)
                    {
                        isActive = true;
                        break;
                    }
                }
            }

            int queueDepth = 0;
            var jobs = _saveService.Jobs;
            if (jobs != null)
            {
                foreach (var req in jobs.Requests)
                {
                    if (req.SiteId.Equals(siteId)) queueDepth++;
                }
            }

            state = new WorksiteViewState(isActive: isActive, queueDepth: queueDepth);
            return true;
        }

        public IDialogSource GetDialogSource(string actorName)
        {
            _activeDialogActor = actorName ?? string.Empty;
            _currentDialogLine = string.IsNullOrEmpty(_activeDialogActor) ? string.Empty
                : $"You speak with {_activeDialogActor}.";
            return this;
        }

        public void SeedWorld(string mood, string calling, string startLocation)
        {
            UnityEngine.Debug.Log($"Domain Seeded: Mood={mood}, Calling={calling}");
        }

        // ----- IDialogSource -----
        public string GetCurrentLine() => _currentDialogLine;
        public string GetPortraitName() => _currentPortrait;
        public IReadOnlyList<string> GetTopics() => _world.Topics?.Select(t => t.Id).ToList() ?? new List<string>();

        public void SelectTopic(string topicId)
        {
            // Codex audit (fourth pass A-P1): previously no-op. Now produces a
            // deterministic acknowledgement line and appends a dialogue-seen
            // event to the WorldEventLog so the deterministic replay surface
            // sees the topic selection. (ActorRecord.Memory is a
            // MemoryComponent which records facts via Add; the topic-seen
            // marker lives on the broader dialogue tracking surface, not
            // directly on MemoryComponent.)
            if (string.IsNullOrEmpty(topicId)) return;
            _currentDialogLine = $"{_activeDialogActor} considers \"{topicId}\".";
            var actor = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, _activeDialogActor, System.StringComparison.Ordinal));
            if (actor != null && _world.Events != null)
            {
                _world.Events.Append(new WorldEvent(
                    _world.Time,
                    WorldEventKind.ActorTalked,
                    actor.Id,
                    default,
                    $"topic_selected id:{topicId}"));
            }
        }

        // ----- IPlayerCommandSink -----
        public void LogCombat(string message) => _lastCombatLine = message ?? string.Empty;

        public void TakePlayerDamage(int amount)
        {
            if (amount <= 0) return;
            // Codex audit (fourth pass A-P2): previously held a transient
            // _playerDamageTaken counter that the HUD subtracted from. Now
            // we mutate the real player ActorRecord vitals so save/load
            // preserves the damage and other systems see the new HP.
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null) return;
            player.ApplyVitals(player.Vitals.WithHealth(player.Vitals.Health.Damage(amount)));
            _lastCombatLine = $"You take {amount} damage!";
        }

        public bool TryCastSpell(int spellSlotIndex)
        {
            // Codex audit (fourth pass A-P1): concrete spell command via
            // SliceSpellCatalog + the EffectDefinition resolver path. Failure
            // surfaces a deterministic refusal reason in LogCombat.
            var spells = EmberCrpg.Simulation.Magic.SliceSpellCatalog.All;
            if (spellSlotIndex < 0 || spellSlotIndex >= spells.Count)
            {
                LogCombat("No such spell slot.");
                return false;
            }
            var spell = spells[spellSlotIndex];
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null)
            {
                LogCombat("No caster.");
                return false;
            }
            // Mana gate: pure read; if insufficient mana, refusal.
            if (player.Vitals.Mana.Current < spell.ManaCost)
            {
                LogCombat($"{spell.DisplayName ?? spell.TemplateId}: insufficient mana.");
                return false;
            }
            // Codex audit (seventh pass A-P1 #2): the previous pass routed
            // only TryPrepareCast + CommitPreparedCast, so mana/cooldown
            // updated but the spell's actual effects (damage, heal, buff)
            // never landed on a target. Switch to SpellExecutionService,
            // which composes Cast → Target → Effect → CastRoll, so the live
            // command performs real domain mutation. Target picker selects
            // the closest hostile actor (or the caster for self-buffs); if
            // no hostile target exists, fall back to the caster so single-
            // target effects still resolve.
            var knownIds = new List<string>(spells.Count);
            foreach (var s in spells) knownIds.Add(s.TemplateId);

            var requestedTarget = SelectSpellTarget(spell, player);

            var executionService = new EmberCrpg.Simulation.Magic.SpellExecutionService(
                new EmberCrpg.Simulation.Magic.SpellCastingService(_ => spell),
                new EmberCrpg.Simulation.Magic.SpellTargetValidator(),
                new EmberCrpg.Simulation.Magic.SpellEffectResolutionService(),
                new EmberCrpg.Simulation.Magic.SpellCastRollService());
            var executed = executionService.TryExecute(
                player, spell.TemplateId, knownIds, requestedTarget, _world.PlayerSpellCooldowns);
            if (!executed.Success)
            {
                LogCombat(executed.Message ?? $"{spell.DisplayName ?? spell.TemplateId}: failed.");
                return false;
            }

            _world.Events?.Append(new WorldEvent(
                _world.Time,
                WorldEventKind.SpellResolved,
                player.Id,
                ResolveCombatSiteId(player, requestedTarget),
                $"slice_spell_cast id:{spell.TemplateId} mana:{executed.ManaSpent}"));
            LogCombat(executed.Message);
            return true;
        }

        public bool TryMeleeStrike(string targetActorName, int rawDamage)
        {
            // Codex audit (fourth pass A-P1): concrete melee command. Resolves
            // the target by stable actor name on SliceWorldState and applies
            // damage; emits a CombatResolved event so the deterministic log
            // captures the strike.
            if (rawDamage <= 0) { LogCombat("Strike whiffs."); return false; }
            var target = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, targetActorName, System.StringComparison.Ordinal));
            if (target == null)
            {
                LogCombat($"No target: {targetActorName ?? string.Empty}");
                return false;
            }
            // Codex audit (sixth pass A-P0 #4): previously bypassed the
            // CombatActionResolver chain entirely (auto-hit, no armor / dodge /
            // accuracy / stamina). Route through CombatActionResolver so the
            // hit roll, damage roll, armor mitigation, stamina cost, and the
            // canonical CombatResolved event all match the deterministic
            // kernel. The action template is a synthetic "melee_swing"
            // CombatActionDef; the band-width matches the existing baseline
            // (rawDamage parameter).
            var attacker = _world.Actors.FirstByRole(ActorRole.Player) ?? target;
            var meleeAction = new CombatActionDef(
                id: new CombatActionId("melee_swing"),
                staminaCost: 0,
                hitFormulaKey: "accuracy_vs_dodge",
                damageFormulaKey: "base_minus_armor",
                animationTag: "melee_swing");
            var rng = new EmberCrpg.Simulation.Rng.XorShiftRng((uint)(_tick == 0 ? 1 : _tick));
            // Codex audit (seventh pass A-P2 #6): previously hard-coded
            // SiteId(1UL) so every combat event was logged under a synthetic
            // location. Derive the site from the actual world: closest
            // authored site to the attacker, falling back to the first
            // site, falling back to SiteId.Empty so the event log stays
            // honest if no sites exist (e.g. tutorial / dialog-only scenes).
            var siteId = ResolveCombatSiteId(attacker, target);
            if (_world.Events == null)
            {
                // Defensive: events log is required by CombatActionResolver.
                target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Damage(rawDamage)));
                LogCombat($"You strike {target.Name} for {rawDamage}.");
                return true;
            }
            var resolver = new EmberCrpg.Simulation.Combat.CombatActionResolver(
                new EmberCrpg.Simulation.Combat.CombatHitRollService(),
                new EmberCrpg.Simulation.Combat.CombatDamageService());
            var outcome = resolver.Resolve(meleeAction, attacker, target, damageBandWidth: rawDamage / 2,
                rng: rng, now: _world.Time, siteId: siteId, events: _world.Events);
            LogCombat(outcome.Hit
                ? $"You strike {target.Name} for {outcome.Damage}."
                : $"You miss {target.Name}.");
            return outcome.Hit;
        }

        private EmberCrpg.Domain.Core.SiteId ResolveCombatSiteId(ActorRecord attacker, ActorRecord target)
        {
            if (_world.Sites == null) return default;
            ActorRecord anchor = attacker ?? target;
            if (anchor != null)
            {
                int bestDistance = int.MaxValue;
                EmberCrpg.Domain.Core.SiteId bestId = default;
                foreach (var site in _world.Sites.Records)
                {
                    var sitePosition = CenterOf(site);
                    var dx = sitePosition.X - anchor.Position.X;
                    var dz = sitePosition.Y - anchor.Position.Y;
                    int d = dx * dx + dz * dz;
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        bestId = site.Id;
                    }
                }
                if (!bestId.IsEmpty) return bestId;
            }
            // Fallback: first authored site, then default.
            foreach (var site in _world.Sites.Records)
            {
                return site.Id;
            }
            return default;
        }

        private ActorRecord SelectSpellTarget(EmberCrpg.Domain.Magic.SpellDefinition spell, ActorRecord player)
        {
            if (spell == null || player == null) return player;
            if (spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.CasterSelf
                || spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.AreaAroundCaster)
                return player;

            ActorRecord best = null;
            var bestDistance = int.MaxValue;
            foreach (var candidate in _world.Actors.Records)
            {
                if (candidate == null || candidate.Id.Equals(player.Id) || !candidate.IsAlive)
                    continue;
                if (candidate.Role != ActorRole.Enemy)
                    continue;

                var distance = player.Position.ManhattanDistanceTo(candidate.Position);
                if (spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.Touch && distance != 1)
                    continue;
                if ((spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.SingleTarget
                        || spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.AreaAtRange)
                    && spell.RangeInTiles > 0
                    && distance > spell.RangeInTiles)
                    continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best ?? player;
        }

        private static GridPosition CenterOf(SiteRecord site)
        {
            if (site == null) return default;
            return new GridPosition(
                (site.MinBound.X + site.MaxBound.X) / 2,
                (site.MinBound.Y + site.MaxBound.Y) / 2);
        }

        private static WorksiteKind WorksiteKindFor(string siteName)
        {
            if (string.Equals(siteName, "Furnace", System.StringComparison.Ordinal)
                || string.Equals(siteName, "Forge", System.StringComparison.Ordinal))
                return WorksiteKind.Furnace;
            if (string.Equals(siteName, "Hearth", System.StringComparison.Ordinal))
                return WorksiteKind.Bakery;
            if (string.Equals(siteName, "HarvestShed", System.StringComparison.Ordinal))
                return WorksiteKind.Field;
            return WorksiteKind.Generic;
        }

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
            GetDialogSource(match.Name);
            return true;
        }

        // ----- IConsultFateOracle -----
        public string ConsultFate()
        {
            // Codex audit (sixth pass A-P2 #3, #5): unify the consult-fate
            // bucket distribution with PlaceholderSimulationAdapter via the
            // canonical Domain.AiDm.ConsultFateOutcomeBucket (35/35/30).
            // Also use uint Knuth multiplier explicitly so the cast is not
            // a foot-gun.
            uint salted = (uint)_tick * 2654435761u;
            int roll = (int)(salted % 100u) + 1;
            var bucket = EmberCrpg.Domain.AiDm.ConsultFateOutcomeBucket.FromRoll(roll);
            if (bucket.Equals(EmberCrpg.Domain.AiDm.ConsultFateOutcomeBucket.Setback))
                return "SETBACK: The stars align against you.";
            if (bucket.Equals(EmberCrpg.Domain.AiDm.ConsultFateOutcomeBucket.Neutral))
                return "NEUTRAL: The DM watches in silence.";
            return "FAVOURABLE: Fortune smiles.";
        }

        // ----- IEmberSaveBridge -----
        public string ExportStateJson()
        {
            // Codex review PR #195 (P2): rethrow so EmberSaveService can show
            // "Save partial: domain export failed." instead of swallowing.
            return _saveService.SaveToJson(_world);
        }

        public void RestoreStateJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            // Codex audit (sixth pass A-P2 #14): the previous field-by-field
            // copy silently dropped any new field added to SliceWorldState.
            // Mirror EVERY public instance field via reflection so the copy
            // stays in lockstep with the type. Properties (the obsolete
            // role accessors) are intentionally skipped — they delegate to
            // the actor store which is itself a field.
            var restored = _saveService.LoadFromJson(json);
            if (restored == null) return;

            var type = typeof(SliceWorldState);
            foreach (var field in type.GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                var value = field.GetValue(restored);
                field.SetValue(_world, value);
            }

            // Reset the tick composer anchor so the next AdvanceTick does
            // not double-advance the just-restored time.
            _tickComposer.ResetAnchor();
        }
    }
}
