using System;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>Stable string provider kind for LLM routing. Phase 12 Atom 1.</summary>
    public readonly struct LlmProviderKind : IEquatable<LlmProviderKind>
    {
        private readonly string _code;
        private LlmProviderKind(string code) { _code = code; }

        public static LlmProviderKind LocalQwen { get; } = new LlmProviderKind("local_qwen");
        public static LlmProviderKind CloudAnthropic { get; } = new LlmProviderKind("cloud_anthropic");
        public static LlmProviderKind CloudOpenAi { get; } = new LlmProviderKind("cloud_openai");
        public static LlmProviderKind Mock { get; } = new LlmProviderKind("mock");

        public static LlmProviderKind FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return default;
            var normalized = code.Trim();
            if (normalized == LocalQwen.Code) return LocalQwen;
            if (normalized == CloudAnthropic.Code) return CloudAnthropic;
            if (normalized == CloudOpenAi.Code) return CloudOpenAi;
            if (normalized == Mock.Code) return Mock;
            return new LlmProviderKind(normalized);
        }

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
