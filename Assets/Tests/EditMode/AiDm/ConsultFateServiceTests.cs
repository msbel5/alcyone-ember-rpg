using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>
    /// Phase 10 — Codex audit Batch 2 / Finding 1 regression coverage.
    /// `ConsultFateService.Resolve` derived the fate roll from `(int)(seed % 100UL)`
    /// which produces 0..99, but `ConsultFateOutcomeBucket.FromRoll` requires 1..100
    /// and throws on 0. Any seed that is a multiple of 100 (including the obvious
    /// initial value `seed: 0`) crashed the deterministic narrator. These tests
    /// pin the off-by-one fix and the canonical bucket distribution.
    /// </summary>
    public sealed class ConsultFateServiceTests
    {
        private static ConsultFateService NewService()
        {
            var router = new ToolCallRouter(new ToolCallValidator());
            return new ConsultFateService(new LlmProposalValidator(new ToolCallValidator()), router);
        }

        private static (LlmRequest req, LlmResponse resp, ToolRegistry reg) NewInputs()
        {
            var registry = new ToolRegistry();
            var request = new LlmRequest(
                systemPromptId: "consult_fate",
                conversationId: "fate",
                availableTools: new List<ToolDescriptor>(),
                maxTokens: 64,
                seed: 0);
            var response = new LlmResponse("fate text", null, 0);
            return (request, response, registry);
        }

        [Test]
        public void Resolve_SeedZero_DoesNotThrow_AndReturnsSetbackBucket()
        {
            var svc = NewService();
            var (req, resp, reg) = NewInputs();
            var events = new WorldEventLog();

            // seed=0 -> roll=1 (Setback) after the +1 off-by-one fix.
            // Before the fix this threw ArgumentOutOfRangeException("Roll must be 1..100.").
            var result = svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 0UL);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Bucket, Is.EqualTo(ConsultFateOutcomeBucket.Setback));
        }

        [Test]
        public void Resolve_SeedMultipleOfHundred_DoesNotThrow()
        {
            var svc = NewService();
            var (req, resp, reg) = NewInputs();
            var events = new WorldEventLog();

            // 100 % 100 = 0 -> would crash without the +1 fix.
            var hundred = svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 100UL);
            var thousand = svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 1000UL);

            Assert.That(hundred.Bucket, Is.EqualTo(ConsultFateOutcomeBucket.Setback));
            Assert.That(thousand.Bucket, Is.EqualTo(ConsultFateOutcomeBucket.Setback));
        }

        [Test]
        public void Resolve_SeedMapsToBucketBoundaries()
        {
            var svc = NewService();
            var (req, resp, reg) = NewInputs();
            var events = new WorldEventLog();

            // Buckets: 1..35 Setback, 36..70 Neutral, 71..100 Favourable.
            // After the +1 fix: seed%100=34 -> roll=35 Setback, seed%100=35 -> roll=36 Neutral,
            // seed%100=69 -> roll=70 Neutral, seed%100=70 -> roll=71 Favourable.
            Assert.That(svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 34UL).Bucket,
                Is.EqualTo(ConsultFateOutcomeBucket.Setback));
            Assert.That(svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 35UL).Bucket,
                Is.EqualTo(ConsultFateOutcomeBucket.Neutral));
            Assert.That(svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 69UL).Bucket,
                Is.EqualTo(ConsultFateOutcomeBucket.Neutral));
            Assert.That(svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 70UL).Bucket,
                Is.EqualTo(ConsultFateOutcomeBucket.Favourable));
            Assert.That(svc.Resolve(req, resp, reg, new GameTime(0), new SiteId(1UL), events, new ToolCallTracer(), seed: 99UL).Bucket,
                Is.EqualTo(ConsultFateOutcomeBucket.Favourable));
        }
    }
}
