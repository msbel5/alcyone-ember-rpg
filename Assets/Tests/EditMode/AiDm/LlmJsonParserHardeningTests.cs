using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using EmberCrpg.Infrastructure.AiDm; // ARCH-05: LLM provider impls
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>
    /// Codex audit (third pass A-P2) regression — the HTTP response parser
    /// used to:
    ///   1. ignore numeric / bool / null scalar values in tool-call parameters
    ///   2. balance brackets without string awareness, corrupting on `]`, `{`,
    ///      `}` inside JSON string values.
    /// These tests inject canned responses through a recording HttpMessageHandler
    /// and assert the LlmResponse.ProposedToolCalls list survives both cases.
    /// </summary>
    public sealed class LlmJsonParserHardeningTests
    {
        private sealed class CannedHandler : HttpMessageHandler
        {
            public string ResponseBody { get; set; } = "{}";
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ResponseBody),
                });
            }
        }

        private static LlmResponse Complete(string responseBody)
        {
            var handler = new CannedHandler { ResponseBody = responseBody };
            var http = new HttpClient(handler);
            var config = new LlmClientConfig(LlmProviderKind.LocalQwen, "http://x", apiKey: null, enabled: true);
            var client = new LocalQwenClient(config, http);
            var req = new LlmRequest(
                systemPromptId: "test",
                conversationId: "c",
                availableTools: new List<ToolDescriptor>(),
                maxTokens: 64,
                seed: 0UL);
            return client.Complete(req);
        }

        [Test]
        public void Parser_NumericScalarParameter_CapturedAsInvariantString()
        {
            var body = @"{
                ""text"":""ok"",
                ""proposed_tool_calls"":[
                    {""tool_id"":""propose_event"",""surface"":""dm"",""parameters"":{""actor_id"":1234,""site_id"":7}}
                ]}";
            var resp = Complete(body);
            Assert.That(resp.ProposedToolCalls.Count, Is.EqualTo(1));
            var call = resp.ProposedToolCalls[0];
            Assert.That(call.Parameters.TryGetValue("actor_id", out var actorId), Is.True);
            Assert.That(actorId, Is.EqualTo("1234"));
            Assert.That(call.Parameters.TryGetValue("site_id", out var siteId), Is.True);
            Assert.That(siteId, Is.EqualTo("7"));
        }

        [Test]
        public void Parser_BoolAndNullScalars_CapturedAsString()
        {
            var body = @"{""text"":""ok"",""proposed_tool_calls"":[
                {""tool_id"":""propose_event"",""surface"":""dm"",""parameters"":{""confirmed"":true,""abort"":false,""marker"":null}}]}";
            var resp = Complete(body);
            Assert.That(resp.ProposedToolCalls.Count, Is.EqualTo(1));
            var p = resp.ProposedToolCalls[0].Parameters;
            Assert.That(p["confirmed"], Is.EqualTo("true"));
            Assert.That(p["abort"], Is.EqualTo("false"));
            Assert.That(p["marker"], Is.EqualTo("null"));
        }

        [Test]
        public void Parser_BracketInStringValue_DoesNotCorruptScan()
        {
            // The closing `]` inside the reason string used to make the scanner
            // think the proposed_tool_calls array ended early, dropping the
            // second tool call. With string-aware bracket walking, both
            // entries land in the result.
            var body = @"{""text"":""ok"",""proposed_tool_calls"":[
                {""tool_id"":""propose_event"",""surface"":""dm"",""parameters"":{""reason"":""guard says ] of payload""}},
                {""tool_id"":""propose_event"",""surface"":""dm"",""parameters"":{""reason"":""follow-up""}}
            ]}";
            var resp = Complete(body);
            Assert.That(resp.ProposedToolCalls.Count, Is.EqualTo(2));
            Assert.That(resp.ProposedToolCalls[0].Parameters["reason"], Is.EqualTo("guard says ] of payload"));
            Assert.That(resp.ProposedToolCalls[1].Parameters["reason"], Is.EqualTo("follow-up"));
        }

        [Test]
        public void Parser_BraceInStringValue_DoesNotCorruptScan()
        {
            var body = @"{""text"":""ok"",""proposed_tool_calls"":[
                {""tool_id"":""propose_event"",""surface"":""dm"",""parameters"":{""reason"":""object with } close""}},
                {""tool_id"":""propose_event"",""surface"":""dm"",""parameters"":{""reason"":""next""}}
            ]}";
            var resp = Complete(body);
            Assert.That(resp.ProposedToolCalls.Count, Is.EqualTo(2));
            Assert.That(resp.ProposedToolCalls[1].Parameters["reason"], Is.EqualTo("next"));
        }

        [Test]
        public void Parser_EscapedQuoteInValue_PreservesParameter()
        {
            var body = @"{""text"":""ok"",""proposed_tool_calls"":[
                {""tool_id"":""propose_event"",""surface"":""dm"",""parameters"":{""reason"":""he said \""hi\"" then""}}
            ]}";
            var resp = Complete(body);
            Assert.That(resp.ProposedToolCalls.Count, Is.EqualTo(1));
            // Escapes are preserved verbatim in our simple parser; downstream
            // validator unescapes as needed. This pins current behavior.
            Assert.That(resp.ProposedToolCalls[0].Parameters["reason"], Does.Contain("hi"));
        }
    }
}
