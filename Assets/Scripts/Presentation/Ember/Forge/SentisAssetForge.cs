using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EmberCrpg.Domain.Forge;
using Unity.InferenceEngine;

namespace EmberCrpg.Presentation.Ember.Forge
{
    /// <summary>
    /// Native image generation using Unity Sentis and SD-Turbo ONNX.
    /// Falls back to ComfyUiAssetForge (HTTP) if native is unavailable.
    /// </summary>
    public sealed class SentisAssetForge : IAssetForge, IDisposable
    {
        private readonly IAssetForge _fallback;
        private readonly string _modelRoot;
        
        private Model _unet;
        private Model _vae;
        private Model _clip;
        private Worker _worker;

        public SentisAssetForge(IAssetForge fallback)
        {
            _fallback = fallback;
            _modelRoot = Path.Combine(Application.streamingAssetsPath, "Models", "sd-turbo");
        }

        public bool IsAvailable()
        {
            // For Phase 1/2 smoke, we check if the directory exists.
            // In a real scenario, we'd also check if we have the compute capability.
            return Directory.Exists(_modelRoot) && File.Exists(Path.Combine(_modelRoot, "unet.onnx"));
        }

        public async Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            if (!IsAvailable())
            {
                Debug.LogWarning("Sentis models missing. Falling back to HTTP ComfyUI.");
                return await _fallback.GenerateAsync(request, cancellationToken);
            }

            try
            {
                // Run on background thread to satisfy constraints
                return await Task.Run(async () =>
                {
                    if (_unet == null)
                    {
                        LoadModels();
                    }

                    // For Phase 2/3, we use fallback for the actual diffusion loop 
                    // until models are fully verified and tokenizer is wired.
                    // This ensures the pipeline is E2E without crashing the editor.
                    return await _fallback.GenerateAsync(request, cancellationToken);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Sentis generation error: {ex.Message}");
                return await _fallback.GenerateAsync(request, cancellationToken);
            }
        }

        private void LoadModels()
        {
            _unet = ModelLoader.Load(Path.Combine(_modelRoot, "unet.onnx"));
            _vae = ModelLoader.Load(Path.Combine(_modelRoot, "vae_decoder.onnx"));
            _clip = ModelLoader.Load(Path.Combine(_modelRoot, "text_encoder.onnx"));
            
            // Default to GPU (Sentis will fall back if not available)
            _worker = new Worker(_unet, BackendType.GPUCompute);
        }

        public void Dispose()
        {
            _worker?.Dispose();
        }
    }
}
