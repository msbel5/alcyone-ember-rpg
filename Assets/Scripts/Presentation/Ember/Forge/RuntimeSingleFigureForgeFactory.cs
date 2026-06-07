using System;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.Forge;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Forge
{
    /// <summary>Builds the runtime NPC-sprite refinement decorator used by every ForgeLocator path.</summary>
    public static class RuntimeSingleFigureForgeFactory
    {
        private const byte AlphaThreshold = 160;
        private const int CropPadding = 12;
        private const int MinimumLargeComponentPixels = 1024;
        private const float DominantComponentRatio = 0.7f;
        private const float UpperBodyFraction = 0.42f;
        private const int UpperBodyMinimumPixels = 400;
        private const int MaxAttempts = 8;

        public static SingleFigureRefiningAssetForge WrapNpcBillboards(IAssetForge serializedForge, string modelRoot, Action<string> log = null)
        {
            if (serializedForge == null) throw new ArgumentNullException(nameof(serializedForge));
            if (string.IsNullOrWhiteSpace(modelRoot)) throw new ArgumentException("Model root is required.", nameof(modelRoot));

            var matte = new OnnxImageMatteService(modelRoot);
            var gate = new ConnectedComponentSingleFigureGate(
                AlphaThreshold,
                MinimumLargeComponentPixels,
                DominantComponentRatio,
                UpperBodyFraction,
                UpperBodyMinimumPixels);
            var options = new SingleFigureRefinementOptions(
                MaxAttempts,
                AlphaThreshold,
                CropPadding,
                MinimumLargeComponentPixels,
                DominantComponentRatio,
                allowBestEffortFallback: false);

            return new SingleFigureRefiningAssetForge(
                serializedForge,
                matte,
                gate,
                options,
                SingleFigureSpritePolicies.NpcOnly,
                log ?? (message => Debug.Log(message)));
        }
    }
}
