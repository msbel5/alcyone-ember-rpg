using System;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>Stable string provider kind for LLM routing. Faz 12 Atom 1.</summary>
    public readonly struct LlmProviderKind : IEquatable<LlmProviderKind>
    {
        private readonly string _code;
        private LlmProviderKind(string code) { _code = code; }

        public static LlmProviderKind LocalQwen { get; } = new LlmProviderKind("local_qwen");
        public static LlmProviderKind CloudAnthropic { get; } = new LlmProviderKind("cloud_anthropic");
        public static LlmProviderKind CloudOpenAi { get; } = new LlmProviderKind("cloud_openai");
        public static LlmProviderKind Mock { get; } = new LlmProviderKind("mock");

        public string Code => _code ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(_code);

        public bool Equals(LlmProviderKind other) => Code == other.Code;
        public override bool Equals(object obj) => obj is LlmProviderKind o && Equals(o);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(LlmProviderKind a, LlmProviderKind b) => a.Equals(b);
        public static bool operator !=(LlmProviderKind a, LlmProviderKind b) => !a.Equals(b);
    }
}
