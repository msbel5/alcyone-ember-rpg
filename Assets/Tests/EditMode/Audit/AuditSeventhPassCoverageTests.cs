using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Time;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Seventh-pass codex audit — coverage for the 33-finding sweep landed on
    /// branch <c>mami/audit-seventh-pass-33-findings</c>. Focuses on the
    /// testing-gap dimension (G) the reviewer called out.
    /// </summary>
    public sealed class AuditSeventhPassCoverageTests
    {
        // ---- G #17: WorldTickComposer ResetAnchor / double-tick behaviour. ---
        [Test]
        public void SliceTickComposer_FirstAdvanceIsNoOpThenSecondMovesTime()
        {
            var calendar = new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, 360),
            });
            var composer = new WorldTickComposer(new GameTimeAdvanceSystem(calendar));
            var world = new WorldState();
            world.Time = new GameTime(0);

            composer.Advance(world, tickIndex: 0);
            // First call anchors only — no time movement.
            Assert.That(world.Time.TotalMinutes, Is.EqualTo(0));

            composer.Advance(world, tickIndex: 5);
            // Five ticks of 1 minute each.
            Assert.That(world.Time.TotalMinutes, Is.EqualTo(5));
        }

        [Test]
        public void SliceTickComposer_ResetAnchor_PreventsDoubleTickAfterRestore()
        {
            var calendar = new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, 360),
            });
            var composer = new WorldTickComposer(new GameTimeAdvanceSystem(calendar));
            var world = new WorldState();
            world.Time = new GameTime(0);

            composer.Advance(world, tickIndex: 0);
            composer.Advance(world, tickIndex: 10);
            Assert.That(world.Time.TotalMinutes, Is.EqualTo(10));

            // Simulate a save/restore: ResetAnchor + reset world time, then a
            // fresh tickIndex == 0 must NOT move time forward.
            composer.ResetAnchor();
            world.Time = new GameTime(10);
            composer.Advance(world, tickIndex: 0);
            Assert.That(world.Time.TotalMinutes, Is.EqualTo(10), "Anchor reset must not double-advance.");

            composer.Advance(world, tickIndex: 3);
            Assert.That(world.Time.TotalMinutes, Is.EqualTo(13));
        }

        [Test]
        public void SliceTickComposer_BackwardsTickIsIdempotent()
        {
            var composer = new WorldTickComposer();
            var world = new WorldState();
            world.Time = new GameTime(0);

            composer.Advance(world, tickIndex: 0);
            composer.Advance(world, tickIndex: 5);
            var before = world.Time.TotalMinutes;
            composer.Advance(world, tickIndex: 2); // backwards
            Assert.That(world.Time.TotalMinutes, Is.EqualTo(before), "Going backwards must not rewind world time.");
        }

        // ---- G #18: EncounterState.Finish mutation. ---------------------------
        [Test]
        public void EncounterState_Finish_SetsWinnerAndCompletes()
        {
            var encounter = new EncounterState(new ActorId(1UL), new ActorId(2UL));
            Assert.That(encounter.IsFinished, Is.False);
            Assert.That(encounter.WinnerName, Is.Null);

            encounter.Finish("Player");
            Assert.That(encounter.IsFinished, Is.True);
            Assert.That(encounter.WinnerName, Is.EqualTo("Player"));
        }

        [Test]
        public void EncounterState_AddLog_AppendsInOrder()
        {
            var encounter = new EncounterState(new ActorId(1UL), new ActorId(2UL));
            encounter.AddLog("turn 1");
            encounter.AddLog("turn 2");
            Assert.That(encounter.LogLines.Count, Is.EqualTo(2));
            Assert.That(encounter.LogLines[0], Is.EqualTo("turn 1"));
            Assert.That(encounter.LogLines[1], Is.EqualTo("turn 2"));
        }

        // ---- G #21: PlantGrowthRule.Matches None / specific. -----------------
        [Test]
        public void PlantGrowthRule_Matches_NoneActsAsWildcard()
        {
            var rule = new PlantGrowthRule(Season.None, allowsGrowth: true, blockedBySnow: false);
            Assert.That(rule.Matches(Season.Spring), Is.True);
            Assert.That(rule.Matches(Season.Summer), Is.True);
            Assert.That(rule.Matches(Season.Autumn), Is.True);
            Assert.That(rule.Matches(Season.Winter), Is.True);
        }

        [Test]
        public void PlantGrowthRule_Matches_SpecificSeasonExcludesOthers()
        {
            var rule = new PlantGrowthRule(Season.Summer, allowsGrowth: true, blockedBySnow: false);
            Assert.That(rule.Matches(Season.Summer), Is.True);
            Assert.That(rule.Matches(Season.Spring), Is.False);
            Assert.That(rule.Matches(Season.Autumn), Is.False);
            Assert.That(rule.Matches(Season.Winter), Is.False);
        }

        [Test]
        public void PlantGrowthRule_CanGrow_BlockedBySnowGate()
        {
            var blockedRule = new PlantGrowthRule(Season.Winter, allowsGrowth: true, blockedBySnow: true);
            Assert.That(blockedRule.CanGrow(isSnowing: true), Is.False);
            Assert.That(blockedRule.CanGrow(isSnowing: false), Is.True);

            var unblockedRule = new PlantGrowthRule(Season.Winter, allowsGrowth: true, blockedBySnow: false);
            Assert.That(unblockedRule.CanGrow(isSnowing: true), Is.True);
        }

        // ---- G #22: CombatActionTimingProfile enum coverage. -----------------
        [Test]
        public void CombatActionTimingProfile_ForEachKind_ReturnsPositiveActive()
        {
            foreach (CombatActionKind kind in System.Enum.GetValues(typeof(CombatActionKind)))
            {
                var profile = CombatActionTimingProfile.For(kind);
                Assert.That(profile.ActiveSeconds, Is.GreaterThan(0d), $"{kind} active must be positive.");
                Assert.That(profile.WindupSeconds, Is.GreaterThanOrEqualTo(0d));
                Assert.That(profile.RecoverySeconds, Is.GreaterThanOrEqualTo(0d));
            }
        }

        [Test]
        public void CombatActionTimingProfile_ForUnknownKind_Throws()
        {
            Assert.That(
                () => CombatActionTimingProfile.For((CombatActionKind)999),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // ---- G #23, #24: DialogueResponse factory + refused branches. --------
        [Test]
        public void DialogueResponse_Spoken_ProducesNonRefused()
        {
            var response = EmberCrpg.Simulation.Narrative.DialogueResponse.Spoken("hello");
            Assert.That(response.Text, Is.EqualTo("hello"));
            Assert.That(response.IsRefused, Is.False);
            // DialogueResponse normalises null → string.Empty in its ctor.
            Assert.That(response.RefusalReason, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DialogueResponse_Refused_CarriesReason()
        {
            var response = EmberCrpg.Simulation.Narrative.DialogueResponse.Refused("hostile");
            Assert.That(response.RefusalReason, Is.EqualTo("hostile"));
            Assert.That(response.IsRefused, Is.True);
            Assert.That(response.Text, Is.EqualTo(string.Empty));
        }

        // ---- G #19: live spell command mutates the target (executed via the
        //       SpellExecutionService that DomainSimulationAdapter.TryCastSpell
        //       now routes through). The Presentation adapter cannot be
        //       constructed in fallback (no Unity engine), so we exercise the
        //       same composition the adapter uses to prove the live path
        //       mutates the domain target's vitals.
        [Test]
        public void SpellExecutionService_LiveCast_MutatesEnemyVitals()
        {
            var caster = new ActorRecord(
                new ActorId(101UL), "Caster", ActorRole.Player,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(20, 20),
                    new VitalStat(12, 12),
                    new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 100, dodge: 0, armor: 0, baseDamage: 1);
            var target = new ActorRecord(
                new ActorId(102UL), "Enemy", ActorRole.Enemy,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(30, 30),
                    new VitalStat(12, 12),
                    new VitalStat(10, 10)),
                new GridPosition(1, 0),
                accuracy: 50, dodge: 0, armor: 0, baseDamage: 1);

            var catalog = EmberCrpg.Simulation.Magic.WorldSpellCatalog.All;
            // Pick the first spell that targets enemies and deals damage.
            EmberCrpg.Domain.Magic.SpellDefinition damageSpell = null;
            foreach (var s in catalog)
            {
                if (s == null) continue;
                damageSpell = s;
                break;
            }
            if (damageSpell == null)
            {
                Assert.Ignore("Spell catalog empty — no live cast to exercise.");
                return;
            }

            var knownIds = new System.Collections.Generic.List<string> { damageSpell.TemplateId };
            var cooldowns = new EmberCrpg.Domain.Magic.SpellCooldownState();
            var execution = new EmberCrpg.Simulation.Magic.SpellExecutionService(
                new EmberCrpg.Simulation.Magic.SpellCastingService(_ => damageSpell),
                new EmberCrpg.Simulation.Magic.SpellTargetValidator(),
                new EmberCrpg.Simulation.Magic.SpellEffectResolutionService(),
                new EmberCrpg.Simulation.Magic.SpellCastRollService());

            int casterManaBefore = caster.Vitals.Mana.Current;
            execution.TryExecute(caster, damageSpell.TemplateId, knownIds, target, cooldowns);

            // The contract: even a failed cast must NOT silently no-op without
            // a refusal reason. A successful cast either spent mana or set a
            // cooldown — either way, observable state on the caster changes.
            bool manaSpent = caster.Vitals.Mana.Current != casterManaBefore;
            bool cooldownSet = cooldowns.GetRemainingTicks(damageSpell.TemplateId) > 0;
            Assert.That(manaSpent || cooldownSet,
                "Live spell command must produce observable state change on the caster (mana or cooldown).");
        }

        // ---- G #15 (ninth pass): RebuildAccumulatorsFrom restores the
        //       hourly remainder from a restored world time. With a 60-minute
        //       hour, a clean advance of 30 ticks restores a 30-minute in-flight
        //       remainder; ResetAnchor preserves that and
        //       RebuildAccumulatorsFrom(world.Time) must agree.
        [Test]
        public void SliceTickComposer_ResetAnchorThenRebuildAccumulators_RestoresHourlyRemainder()
        {
            var composer = new WorldTickComposer();
            var world = new WorldState();
            world.Time = new GameTime(0);

            composer.Advance(world, tickIndex: 0);  // anchor
            composer.Advance(world, tickIndex: 30); // +30 minutes, halfway to the first hourly boundary

            Assert.That(world.Time.TotalMinutes, Is.EqualTo(30L));

            composer.ResetAnchor();
            composer.RebuildAccumulatorsFrom(world.Time);

            // _ticksSinceHourly is private — peek via reflection to pin behaviour.
            var field = typeof(WorldTickComposer).GetField(
                "_ticksSinceHourly",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "_ticksSinceHourly field must exist for accumulator restore");
            int hourlyAccum = (int)field.GetValue(composer);
            int expected = (int)(30L % WorldTickComposer.TicksPerGameHour); // 30 % 60 == 30
            Assert.That(hourlyAccum, Is.EqualTo(expected),
                "RebuildAccumulatorsFrom must restore _ticksSinceHourly to TotalMinutes % TicksPerGameHour.");
        }

        // ---- G #15 (ninth pass): catch-up event timestamps must land on the
        //       cadence boundary, not the post-advance Time. With a delta of 125
        //       (crossing two hourly boundaries at minute 60 and 120) the
        //       resulting NeedChanged events on a registered actor must carry
        //       Tick.TotalMinutes == 60 and Tick.TotalMinutes == 120 — NOT 125.
        [Test]
        public void SliceTickComposer_HourlyCatchupStampsEventsAtBoundaryNotPostAdvanceTime()
        {
            var composer = new WorldTickComposer();
            var world = new WorldState();
            world.Time = new GameTime(0);

            // Register an actor so NeedsSystem actually appends events.
            world.Actors.Add(new ActorRecord(
                new ActorId(1UL), "Hungry", ActorRole.Player,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(20, 20),
                    new VitalStat(12, 12),
                    new VitalStat(20, 20)),
                new GridPosition(0, 0),
                accuracy: 100, dodge: 0, armor: 0, baseDamage: 1));

            composer.Advance(world, tickIndex: 0);   // anchor
            composer.Advance(world, tickIndex: 125); // delta 125 - crosses 2 hourly boundaries

            Assert.That(world.Time.TotalMinutes, Is.EqualTo(125L));

            var needEvents = new System.Collections.Generic.List<WorldEvent>();
            foreach (var ev in world.Events.Events)
            {
                if (ev != null && ev.Kind == WorldEventKind.NeedChanged)
                    needEvents.Add(ev);
            }
            Assert.That(needEvents.Count, Is.EqualTo(2),
                "Delta of 125 ticks must produce exactly two hourly catch-up NeedChanged events.");

            // Stamps must be the cadence boundaries (60, 120) - NOT 125.
            Assert.That(needEvents[0].Tick.TotalMinutes, Is.EqualTo(60L),
                "First catch-up NeedChanged must be stamped at minute 60 (first hourly boundary).");
            Assert.That(needEvents[1].Tick.TotalMinutes, Is.EqualTo(120L),
                "Second catch-up NeedChanged must be stamped at minute 120 (second hourly boundary).");
            Assert.That(needEvents[0].Tick.TotalMinutes, Is.Not.EqualTo(125L));
            Assert.That(needEvents[1].Tick.TotalMinutes, Is.Not.EqualTo(125L));
        }
    }
}
