using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public sealed class GeneratedAssetValidationIssue
    {
        public string stableId = string.Empty;
        public GeneratedAssetValidationSeverity severity = GeneratedAssetValidationSeverity.Warning;
        public string message = string.Empty;

        public GeneratedAssetValidationIssue()
        {
        }

        public GeneratedAssetValidationIssue(string stableId, GeneratedAssetValidationSeverity severity, string message)
        {
            this.stableId = stableId ?? string.Empty;
            this.severity = severity;
            this.message = message ?? string.Empty;
        }
    }
}
