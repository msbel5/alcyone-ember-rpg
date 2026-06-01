using System;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.Forge;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class SerializedAssetForge : IAssetForge, IDisposable
    {
        private readonly IAssetForge _innerForge;
        private readonly GenerationManager _manager;

        public SerializedAssetForge(IAssetForge innerForge, IResourceProbe resourceProbe)
        {
            _innerForge = innerForge ?? throw new ArgumentNullException(nameof(innerForge));
            _manager = new GenerationManager(_innerForge, resourceProbe: resourceProbe ?? new NullResourceProbe());
        }

        public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            return _manager.GenerateAsync(request, AssetForgePriority.Background, cancellationToken);
        }

        public bool IsAvailable()
        {
            return _innerForge.IsAvailable();
        }

        public void Dispose()
        {
            _manager.Dispose();
        }
    }
}
