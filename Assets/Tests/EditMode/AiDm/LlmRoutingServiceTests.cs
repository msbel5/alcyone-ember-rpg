using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class LlmRoutingServiceTests
    {
        private static LlmRequest Req() => new LlmRequest("dm", "conv", null, 100, 1UL);

        [Test]
        public void LocalSucceeds_PicksLocal()
        {
            var service = new LlmRoutingService(
                local: _ => new LlmResponse("local", null, 1),
                cloud: _ => new LlmResponse("cloud", null, 1));
            var response = service.Complete(Req(), out var chosen);
            Assert.That(chosen, Is.EqualTo(LlmProviderKind.LocalQwen));
            Assert.That(response.Text, Is.EqualTo("local"));
        }

        [Test]
        public void LocalReturnsEmpty_FallsBackToCloud()
        {
            var service = new LlmRoutingService(
                local: _ => new LlmResponse(string.Empty, null, 0),
                cloud: _ => new LlmResponse("cloud", null, 1));
            var response = service.Complete(Req(), out var chosen);
            Assert.That(chosen, Is.EqualTo(LlmProviderKind.CloudAnthropic));
            Assert.That(response.Text, Is.EqualTo("cloud"));
        }

        [Test]
        public void LocalThrows_FallsBackToCloud()
        {
            var service = new LlmRoutingService(
                local: _ => throw new System.InvalidOperationException("llama_decode failed: InvalidInputBatch"),
                cloud: _ => new LlmResponse("cloud", null, 1));
            var response = service.Complete(Req(), out var chosen);
            Assert.That(chosen, Is.EqualTo(LlmProviderKind.CloudAnthropic));
            Assert.That(response.Text, Is.EqualTo("cloud"));
        }

        [Test]
        public void LocalNativeFailureText_IsNotUsefulPayload()
        {
            var service = new LlmRoutingService(
                local: _ => new LlmResponse("Native error: llama_decode failed: InvalidInputBatch", null, 0),
                cloud: null);
            var response = service.Complete(Req(), out var chosen);
            Assert.That(chosen, Is.EqualTo(LlmProviderKind.Mock));
            Assert.That(response.Text, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BothNull_ReturnsEmptyAndMockKind()
        {
            var service = new LlmRoutingService(null, null);
            var response = service.Complete(Req(), out var chosen);
            Assert.That(chosen, Is.EqualTo(LlmProviderKind.Mock));
            Assert.That(response.Text, Is.EqualTo(string.Empty));
        }
    }
}
