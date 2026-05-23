Native DLLs for pure-C# AI inference (ONNX Runtime + LLamaSharp)
=================================================================

Drop the following DLLs into THIS folder before building Ember:

LLamaSharp (LLM, GGUF):
  - LLamaSharp.dll                    (managed, all platforms)
  - LLamaSharp.Backend.Cpu (runtimes\win-x64\native\llama.dll for Windows)
  - Source: https://www.nuget.org/packages/LLamaSharp/0.27.0
            https://www.nuget.org/packages/LLamaSharp.Backend.Cpu/0.18.0
  - Place llama.dll next to LLamaSharp.dll on Windows; the Cuda12 backend ships
    separate native binaries under runtimes\win-x64\native\.

Microsoft.ML.OnnxRuntime (image gen + embeddings):
  - Microsoft.ML.OnnxRuntime.dll      (managed)
  - onnxruntime.dll                   (native, Windows x64 — runtimes\win-x64\native\)
  - onnxruntime_providers_shared.dll  (native helper)
  - Source: https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime/1.26.0
            https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime.Gpu/1.18.0  (optional, for CUDA)

Microsoft.ML.Tokenizers (CLIP tokenizer JSON + WordPiece for MiniLM):
  - Microsoft.ML.Tokenizers.dll
  - Source: https://www.nuget.org/packages/Microsoft.ML.Tokenizers/3.0.0-preview.26160.2

After dropping the DLLs:
  1. Add `USE_ONNX_RUNTIME` AND `USE_LLAMASHARP` to
     ProjectSettings/ProjectSettings.asset scriptingDefineSymbols (Player ->
     Other Settings -> Scripting Define Symbols), or set them per-asmdef
     versionDefines.
  2. Refresh the Asset Database. Unity will generate .meta files for the
     newly-imported DLLs.

Cross-platform note:
  - Linux x64 lives under Assets/Plugins/x86_64/ on Unity (same folder).
    Drop the Linux .so equivalents alongside the .dll files; Unity will
    pick the right one based on platform settings in the .meta file.
  - Add additional runtime IDs (linux-x64, osx-arm64) when shipping on
    those platforms.

See docs/ai-stack-setup.md for the full setup guide.
