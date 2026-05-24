using System;

namespace EmberCrpg.Domain.Generation
{
    public sealed class ManifestEntry
    {
        public ManifestEntry(string id, string category, string expectedPath, string staticPromptKey, int width, int height, bool requiresGeneration, int timeoutSeconds = 300, string modelHint = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Manifest id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Manifest category is required.", nameof(category));
            if (string.IsNullOrWhiteSpace(expectedPath)) throw new ArgumentException("Manifest path is required.", nameof(expectedPath));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            Id = id.Trim();
            Category = category.Trim();
            ExpectedPath = expectedPath.Trim().Replace('\\', '/');
            StaticPromptKey = staticPromptKey == null ? string.Empty : staticPromptKey.Trim();
            Width = width;
            Height = height;
            RequiresGeneration = requiresGeneration;
            TimeoutSeconds = timeoutSeconds;
            ModelHint = modelHint == null ? string.Empty : modelHint.Trim();
        }

        public string Id { get; }
        public string Category { get; }
        public string ExpectedPath { get; }
        public string StaticPromptKey { get; }
        public int Width { get; }
        public int Height { get; }
        public bool RequiresGeneration { get; }
        public int TimeoutSeconds { get; }
        public string ModelHint { get; }
    }
}
