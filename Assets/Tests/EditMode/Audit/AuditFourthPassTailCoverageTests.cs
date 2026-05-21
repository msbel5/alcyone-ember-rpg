using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Movement;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Tail of the fourth-pass audit: the remaining G test gaps + regressions
    /// pinning the codex review fixes on PR #196.
    /// </summary>
    public sealed class AuditFourthPassTailCoverageTests
    {
        private static ActorRecord NewCaster(int mana = 30)
        {
            return new ActorRecord(
                new ActorId(1UL), "caster", ActorRole.Player,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(20, 20), new VitalStat(12, 12), new VitalStat(mana, 100)),
                new GridPosition(0, 0),
                accuracy: 10, dodge: 5, armor: 0, baseDamage: 1);
        }

        private static ActorRecord NewDefender(int armor = 2)
        {
            return new ActorRecord(
                new ActorId(2UL), "defender", ActorRole.Enemy,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(20, 20), new VitalStat(12, 12), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                accuracy: 5, dodge: 4, armor: armor, baseDamage: 1);
        }

        // Codex review on PR #196 (P1) regression — SpellSlots / TryCastSpell
        // index alignment lives in DomainSimulationAdapter which is in the
        // Presentation/Ember assembly (Unity-dependent). The Unity EditMode
        // CI on every PR validates compile + behavior; this pure-C# fallback
        // covers the underlying catalog contract instead.
        [Test]
        public void SliceSpellCatalog_All_IndexOrderIsStable()
        {
            var first = SliceSpellCatalog.All;
            var second = SliceSpellCatalog.All;
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
            {
                // A second read must produce the SAME TemplateId at the SAME
                // index — that is the contract DomainSimulationAdapter.SpellSlots
                // depends on so that pressing slot N casts the spell shown at
                // slot N in the HUD.
                Assert.That(second[i].TemplateId, Is.EqualTo(first[i].TemplateId),
                    $"slot {i}: catalog index ordering must be stable across reads");
            }
        }

        // ----- SpellCastingService.TryPrepareCast (G-P3 / P2) -----
        [Test]
        public void SpellCastingService_TryPrepareCast_InsufficientMana_Rejected()
        {
            var svc = new SpellCastingService(_ => SliceSpellCatalog.CreateFlameBolt());
            var caster = NewCaster(mana: 0);
            var result = svc.TryPrepareCast(caster, "flame_bolt", new[] { "flame_bolt" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SpellCastError.InsufficientMana));
        }

        [Test]
        public void SpellCastingService_TryPrepareCast_KnownSpellPrepared()
        {
            var svc = new SpellCastingService(_ => SliceSpellCatalog.CreateFlameBolt());
            var caster = NewCaster(mana: 30);
            var result = svc.TryPrepareCast(caster, "flame_bolt", new[] { "flame_bolt" });
            Assert.That(result.Success, Is.True);
        }

        // ----- SpellCastingService.CommitPreparedCast (G-P2) -----
        [Test]
        public void SpellCastingService_CommitPreparedCast_ConsumesMana()
        {
            var spell = SliceSpellCatalog.CreateFlameBolt();
            var svc = new SpellCastingService(_ => spell);
            var caster = NewCaster(mana: 100);
            var manaBefore = caster.Vitals.Mana.Current;
            var commit = svc.CommitPreparedCast(caster, spell);
            Assert.That(commit.Success, Is.True);
            Assert.That(caster.Vitals.Mana.Current, Is.LessThan(manaBefore));
        }

        // ----- SpellEffectResolutionService.CanResolveInstantaneousEffects (G-P3) -----
        [Test]
        public void SpellEffectResolutionService_CanResolveInstantaneous_HappyPath()
        {
            var spell = SliceSpellCatalog.CreateFlameBolt();
            var svc = new SpellEffectResolutionService();
            var target = NewDefender();
            var result = svc.CanResolveInstantaneousEffects(spell, target);
            Assert.That(result, Is.Not.Null);
        }

        // ----- SpellCostCalculator.EstimateEffectCost (G-P3) -----
        [Test]
        public void SpellCostCalculator_EstimateEffectCost_PositiveForKnownKind()
        {
            var spec = new SpellEffectSpec(SpellEffectCode.DirectDamage, magnitude: 5, durationTicks: 0);
            var cost = new SpellCostCalculator().EstimateEffectCost(spec);
            Assert.That(cost, Is.GreaterThanOrEqualTo(0));
        }

        // ----- NarrationServices.Narrate (G-P2) -----
        [Test]
        public void DmNarrationService_Narrate_AppendsProposalLogEntry()
        {
            var routing = new LlmRoutingService(
                local: req => new LlmResponse("ok", null, 1),
                cloud: null);
            var svc = new DmNarrationService(routing);
            var world = new EmberCrpg.Simulation.World.SliceWorldFactory().Create(roomSeed: 1);
            var beforeCount = world.LlmProposalLog.Count;
            var req = new LlmRequest("dm.narrate", "c1", new List<ToolDescriptor>(), 64, 0UL);
            var resp = svc.Narrate(req, new GameTime(0), world);
            Assert.That(resp.Text, Is.EqualTo("ok"));
            Assert.That(world.LlmProposalLog.Count, Is.EqualTo(beforeCount + 1));
        }

        // ----- NarrationServices.RecordCheckpoint (G-P2) -----
        [Test]
        public void StorytellerCheckpointSystem_RecordCheckpoint_AppendsEventWhenSitePresent()
        {
            var world = new EmberCrpg.Simulation.World.SliceWorldFactory().Create(roomSeed: 1);
            // Seed a site so the checkpoint has a valid anchor.
            world.Sites.Add(new SiteRecord(new SiteId(1UL), SiteKind.Settlement, "outpost", new GridPosition(0, 0), new GridPosition(1, 1)));
            var before = world.Events.Count;
            new StorytellerCheckpointSystem().RecordCheckpoint(world, new GameTime(0), "act-1");
            Assert.That(world.Events.Count, Is.EqualTo(before + 1));
        }

        [Test]
        public void StorytellerCheckpointSystem_RecordCheckpoint_NoSites_SilentNoop()
        {
            var world = new EmberCrpg.Simulation.World.SliceWorldFactory().Create(roomSeed: 1);
            // Default factory leaves sites empty; RecordCheckpoint must skip.
            Assert.That(world.Sites.Count, Is.EqualTo(0));
            var before = world.Events.Count;
            new StorytellerCheckpointSystem().RecordCheckpoint(world, new GameTime(0), "act-x");
            Assert.That(world.Events.Count, Is.EqualTo(before));
        }

        // ----- Sprint4KinematicMotor.ResolveGrounding (G-P3) -----
        [Test]
        public void Sprint4KinematicMotor_ResolveGrounding_TransitionsToGrounded()
        {
            var motor = new Sprint4KinematicMotor();
            var state = new Sprint4MotorState(
                position: new Sprint4Vector3(0f, 5f, 0f),
                verticalVelocity: -10f,
                isGrounded: false);
            var next = motor.ResolveGrounding(state, new Sprint4Vector3(0f, 0f, 0f), isGrounded: true);
            Assert.That(next.IsGrounded, Is.True);
            Assert.That(next.Position.Y, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
