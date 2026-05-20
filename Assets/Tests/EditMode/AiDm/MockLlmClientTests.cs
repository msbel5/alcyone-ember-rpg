using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class MockLlmClientTests
    {
        [Test]
        public void Kind_IsMock()
        {
            Assert.That(new MockLlmClient().Kind, Is.EqualTo(LlmProviderKind.Mock));
        }

        [Test]
        public void Complete_NoScript_ReturnsEmpty()
        {
            var client = new MockLlmClient();
            var request = new LlmRequest("dm", "conv", null, 100, 1UL);
            var response = client.Complete(request);
            Assert.That(response.Text, Is.EqualTo(string.Empty));
            Assert.That(response.TokensUsed, Is.EqualTo(0));
        }

        [Test]
        public void Complete_Scripted_ReturnsScript()
        {
            var client = new MockLlmClient();
            client.Script("dm", "conv", 1UL, new LlmResponse("scripted", null, 5));
            var request = new LlmRequest("dm", "conv", null, 100, 1UL);
            var response = client.Complete(request);
            Assert.That(response.Text, Is.EqualTo("scripted"));
            Assert.That(response.TokensUsed, Is.EqualTo(5));
        }

        [Test]
        public void Complete_DifferentSeed_DifferentScript()
        {
            var client = new MockLlmClient();
            client.Script("dm", "conv", 1UL, new LlmResponse("seed1", null, 1));
            client.Script("dm", "conv", 2UL, new LlmResponse("seed2", null, 1));
            Assert.That(client.Complete(new LlmRequest("dm", "conv", null, 100, 1UL)).Text, Is.EqualTo("seed1"));
            Assert.That(client.Complete(new LlmRequest("dm", "conv", null, 100, 2UL)).Text, Is.EqualTo("seed2"));
        }
    }
}
