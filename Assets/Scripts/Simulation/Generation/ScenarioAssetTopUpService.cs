using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class ScenarioAssetTopUpService
    {
        private readonly string _projectRoot;
        private readonly IAssetForge _forge;

        public ScenarioAssetTopUpService(string projectRoot, IAssetForge forge)
        {
            _projectRoot = string.IsNullOrWhiteSpace(projectRoot) ? throw new ArgumentException("Project root is required.", nameof(projectRoot)) : projectRoot;
            _forge = forge ?? throw new ArgumentNullException(nameof(forge));
        }

        public Task<VisibleGenerationFlowResult> RunVisibleTopUpAsync(IReadOnlyList<ManifestEntry> scenarioEntries, CancellationToken cancellationToken, int maxGenerationEntries = int.MaxValue)
        {
            var failureLog = new GenerationFailureLog(Path.Combine(_projectRoot, "Logs", "generation-failures.json"));
            var flow = new VisibleGenerationFlow(_projectRoot, _forge, StaticPromptCatalog.CreateDefault(), failureLog);
            return flow.RunCoreAssetTopUpAsync(scenarioEntries ?? Array.Empty<ManifestEntry>(), cancellationToken, maxGenerationEntries);
        }
    }
}
