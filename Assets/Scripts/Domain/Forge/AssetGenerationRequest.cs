using System;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Forge
{
    public enum AssetSubjectKind
    {
        Npc = 0,
        Item = 1,
        Region = 2,
        Splash = 3,
    }

    public sealed class AssetGenerationRequest
    {
        public AssetGenerationRequest(
            string requestId,
            AssetSubjectKind subject,
            WorldStyle style,
            WorldGenre genre,
            string moodKeyword,
            string promptHash,
            int width,
            int height,
            uint seed,
            string prompt,
            string negativePrompt,
            int timeoutSeconds = 300,
            string modelHint = "",
            int steps = 1)
        {
            if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException("RequestId is required.", nameof(requestId));
            if (string.IsNullOrWhiteSpace(promptHash)) throw new ArgumentException("PromptHash is required.", nameof(promptHash));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            RequestId = requestId.Trim();
            Subject = subject;
            Style = style;
            Genre = genre;
            MoodKeyword = moodKeyword ?? string.Empty;
            PromptHash = promptHash.Trim();
            Width = width;
            Height = height;
            Seed = seed;
            Prompt = prompt ?? string.Empty;
            NegativePrompt = negativePrompt ?? string.Empty;
            TimeoutSeconds = timeoutSeconds;
            ModelHint = modelHint ?? string.Empty;
            Steps = steps < 1 ? 1 : steps;
        }

        public string RequestId { get; }
        public AssetSubjectKind Subject { get; }
        public WorldStyle Style { get; }
        public WorldGenre Genre { get; }
        public string MoodKeyword { get; }
        public string PromptHash { get; }
        public int Width { get; }
        public int Height { get; }
        public uint Seed { get; }
        public string Prompt { get; }
        public string NegativePrompt { get; }
        public int TimeoutSeconds { get; }
        public string ModelHint { get; }

        /// <summary>Diffusion fidelity steps — a CONFIG VARIABLE (default 1), not a hardcoded constant.
        /// The pipeline honours this; set it per AssetKind via ImageGenKindTemplate / ImageGenSpec.Steps.</summary>
        public int Steps { get; }
    }
}

