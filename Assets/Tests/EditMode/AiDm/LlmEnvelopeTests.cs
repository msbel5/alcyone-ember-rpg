using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class LlmEnvelopeTests
    {
        [Test]
        public void LlmProviderKind_FourStableCodes()
        {
            Assert.That(LlmProviderKind.LocalQwen.Code, Is.EqualTo("local_qwen"));
            Assert.That(LlmProviderKind.CloudAnthropic.Code, Is.EqualTo("cloud_anthropic"));
            Assert.That(LlmProviderKind.CloudOpenAi.Code, Is.EqualTo("cloud_openai"));
            Assert.That(LlmProviderKind.Mock.Code, Is.EqualTo("mock"));
        }

        [Test]
        public void LlmRequest_HappyPath()
        {
            var request = new LlmRequest("dm_default", "conv_1", null, maxTokens: 512, seed: 42UL);
            Assert.That(request.SystemPromptId, Is.EqualTo("dm_default"));
            Assert.That(request.ConversationId, Is.EqualTo("conv_1"));
            Assert.That(request.AvailableTools, Is.Empty);
            Assert.That(request.MaxTokens, Is.EqualTo(512));
            Assert.That(request.Seed, Is.EqualTo(42UL));
        }

        [Test]
        public void LlmRequest_RejectsInvalid()
        {
            Assert.Throws<System.ArgumentException>(() => new LlmRequest("", "conv", null, 100, 1));
            Assert.Throws<System.ArgumentException>(() => new LlmRequest("p", "", null, 100, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LlmRequest("p", "c", null, 0, 1));
        }

        [Test]
        public void LlmResponse_HappyPath()
        {
            var response = new LlmResponse("hello", null, 12);
            Assert.That(response.Text, Is.EqualTo("hello"));
            Assert.That(response.ProposedToolCalls, Is.Empty);
            Assert.That(response.TokensUsed, Is.EqualTo(12));
        }

        [Test]
        public void LlmResponse_RejectsNegativeTokens()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new LlmResponse("t", null, -1));
        }
    }
}
