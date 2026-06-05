using System;
using System.Collections.Generic;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public sealed class GeneratedTextureValidationReport
    {
        public float horizontalEdgeDifference;
        public float verticalEdgeDifference;
        public bool hasWarmBrightBlob;
        public bool hasStrongGradient;
        public List<string> warnings = new List<string>();
    }
}
