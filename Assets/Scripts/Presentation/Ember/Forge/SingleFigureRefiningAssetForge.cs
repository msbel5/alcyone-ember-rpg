using System;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Simulation.Forge;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class SingleFigureRefiningAssetForge : IAssetForge, IDisposable
    {
        private readonly IAssetForge _inner;
        private readonly IImageMatteService _matteService;
        private readonly SingleFigureSpriteRefiner _refiner;

        public SingleFigureRefiningAssetForge(IAssetForge inner, IImageMatteService matteService, ISingleFigureGate gate, SingleFigureRefinementOptions options, Func<AssetGenerationRequest, bool> shouldRefine, Action<string> log)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _matteService = matteService ?? throw new ArgumentNullException(nameof(matteService));
            _refiner = new SingleFigureSpriteRefiner(_inner, _matteService, gate, new OnnxPngSpriteImageCodec(), options, shouldRefine, log);
        }

        public bool IsAvailable() => _inner.IsAvailable();

        public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            return _refiner.GenerateAsync(request, cancellationToken);
        }

        public void Dispose()
        {
            if (_matteService is IDisposable matteDisposable) matteDisposable.Dispose();
            if (_inner is IDisposable innerDisposable) innerDisposable.Dispose();
        }
    }
}
