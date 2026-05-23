using System.Threading;
using System.Threading.Tasks;

namespace EmberCrpg.Domain.Forge
{
    public interface IAssetForge
    {
        Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken);
        bool IsAvailable();
    }
}
