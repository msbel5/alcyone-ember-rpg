using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>
    /// Codex audit (G/P2) regression — `LlmClients.CompleteHttp` is an external
    /// boundary (sends an HTTP POST, parses the response). Previously
    /// untested. These tests inject a recording <see cref="HttpMessageHandler"/>
    /// so we can pin the request body shape and the error-mapping behaviour
    /// without an actual provider.
    /// </summary>
    public sealed class LlmHttpBoundaryTests
    {
        private sealed class RecordingHandler : HttpMessageHandler
        {
            public string CapturedUri { get; private set; }
            public string CapturedBody { get; private set; }
            public string CapturedAuthHeader { get; private set; }
            public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;
            public string ResponseBody { get; set; } = "{\"text\":\"hello\",\"tokens_used\":7}";

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CapturedUri = request.RequestUri?.ToString() ?? string.Empty;
                CapturedBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
                if (request.Headers.TryGetValues("Authorization", out var auth))
                {
                    foreach (var v in auth)
                    {
                        CapturedAuthHeader = v;
                        break;
                    }
                }
                var response = new HttpResponseMessage(ResponseStatus)
                {
                    Content = new StringContent(ResponseBody),
                };
                return Task.FromResult(response);
            }
        }

        private static LlmRequest NewRequest() => new LlmRequest(
            systemPromptId: "test_prompt",
            conversationId: "conv-1",
            availableTools: new List<ToolDescriptor>(),
            maxTokens: 64,
            seed: 42UL);

        [Test]
        public void Complete_DisabledConfig_ReturnsEmpty_NoHttpCall()
        {
            var handler = new RecordingHandler();
            var http = new HttpClient(handler);
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, "http://localhost", apiKey: null, enabled: false);
            var client = new LocalQwenClient(config, http);

            var response = client.Complete(NewRequest());

            Assert.That(response.Text, Is.EqualTo(string.Empty));
            Assert.That(response.TokensUsed, Is.EqualTo(0));
            Assert.That(handler.CapturedUri, Is.Null, "disabled config must not POST anywhere");
        }

        [Test]
        public void Complete_EmptyEndpoint_ReturnsEmpty_NoHttpCall()
        {
            var handler = new RecordingHandler();
            var http = new HttpClient(handler);
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, endpointUrl: "  ", apiKey: null, enabled: true);
            var client = new LocalQwenClient(config, http);

            var response = client.Complete(NewRequest());

            Assert.That(response.Text, Is.EqualTo(string.Empty));
            Assert.That(handler.CapturedUri, Is.Null);
        }

        [Test]
        public void Complete_PostsExpectedBodyAndParsesResponse()
        {
            var handler = new RecordingHandler();
            var http = new HttpClient(handler);
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, "http://test/v1", apiKey: null, enabled: true);
            var client = new LocalQwenClient(config, http);

            var response = client.Complete(NewRequest());

            Assert.That(handler.CapturedUri, Is.EqualTo("http://test/v1"));
            Assert.That(handler.CapturedBody, Does.Contain("\"system_prompt_id\":\"test_prompt\""));
            Assert.That(handler.CapturedBody, Does.Contain("\"conversation_id\":\"conv-1\""));
            Assert.That(handler.CapturedBody, Does.Contain("\"max_tokens\":64"));
            Assert.That(handler.CapturedBody, Does.Contain("\"seed\":42"));
            Assert.That(handler.CapturedBody, Does.Contain("\"available_tools\":["));
            Assert.That(response.Text, Is.EqualTo("hello"));
            Assert.That(response.TokensUsed, Is.EqualTo(7));
        }

        [Test]
        public void Complete_WithApiKey_AddsBearerAuthHeader()
        {
            var handler = new RecordingHandler();
            var http = new HttpClient(handler);
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, "http://test/v1", apiKey: "secret-key", enabled: true);
            var client = new LocalQwenClient(config, http);

            client.Complete(NewRequest());

            Assert.That(handler.CapturedAuthHeader, Is.EqualTo("Bearer secret-key"));
        }

        [Test]
        public void Complete_NonSuccessStatus_ReturnsEmpty()
        {
            var handler = new RecordingHandler { ResponseStatus = HttpStatusCode.InternalServerError };
            var http = new HttpClient(handler);
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, "http://test/v1", apiKey: null, enabled: true);
            var client = new LocalQwenClient(config, http);

            var response = client.Complete(NewRequest());

            Assert.That(response.Text, Is.EqualTo(string.Empty));
            Assert.That(response.TokensUsed, Is.EqualTo(0));
        }

        [Test]
        public void Complete_UnicodeEscapeRoundtripsThroughExtractStringField()
        {
            // Codex audit Batch FINAL (A/P3): the response parser must decode
            // \uXXXX. é = é.
            var handler = new RecordingHandler
            {
                ResponseBody = "{\"text\":\"caf\\u00e9\",\"tokens_used\":2}",
            };
            var http = new HttpClient(handler);
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, "http://test/v1", apiKey: null, enabled: true);
            var client = new LocalQwenClient(config, http);

            var response = client.Complete(NewRequest());

            Assert.That(response.Text, Is.EqualTo("café"));
        }

        [Test]
        public void Complete_NullRequest_Throws()
        {
            var http = new HttpClient(new RecordingHandler());
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, "http://test/v1", apiKey: null, enabled: true);
            var client = new LocalQwenClient(config, http);

            Assert.Throws<ArgumentNullException>(() => client.Complete(null));
        }
    }
}
