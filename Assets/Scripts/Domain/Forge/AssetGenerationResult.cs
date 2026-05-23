using System;

namespace EmberCrpg.Domain.Forge
{
    public sealed class AssetGenerationResult
    {
        public AssetGenerationResult(
            string requestId,
            byte[] imageBytes,
            string mimeType,
            long generationTimeMs,
            bool success,
            string failureReason)
        {
            if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException("RequestId is required.", nameof(requestId));
            if (generationTimeMs < 0) throw new ArgumentOutOfRangeException(nameof(generationTimeMs));

            RequestId = requestId.Trim();
            ImageBytes = imageBytes == null ? new byte[0] : (byte[])imageBytes.Clone();
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? "image/png" : mimeType.Trim();
            GenerationTimeMs = generationTimeMs;
            Success = success;
            FailureReason = failureReason ?? string.Empty;
        }

        public string RequestId { get; }
        public byte[] ImageBytes { get; }
        public string MimeType { get; }
        public long GenerationTimeMs { get; }
        public bool Success { get; }
        public string FailureReason { get; }

        public static AssetGenerationResult Failed(string requestId, string reason)
        {
            return new AssetGenerationResult(requestId, null, "image/png", 0, false, reason);
        }
    }
}
