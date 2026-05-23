using System;
using System.Collections.Generic;
#if USE_ONNX_RUNTIME
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
#endif

namespace EmberCrpg.Simulation.Forge
{
    internal sealed class OnnxSessionFactory
    {
        private readonly OnnxExecutionProviderPreference _providerPreference;

        public OnnxSessionFactory(OnnxExecutionProviderPreference providerPreference)
        {
            _providerPreference = providerPreference;
        }

        public OnnxExecutionProviderPreference ProviderPreference => _providerPreference;

#if USE_ONNX_RUNTIME
        public InferenceSession CreateSession(string modelPath)
        {
            using (var options = CreateOptions())
            {
                return new InferenceSession(modelPath, options);
            }
        }

        public NamedOnnxValue CreateTokenInput(InferenceSession session, int[] tokenIds)
        {
            string name = ResolveInputName(session, "input_ids");
            var metadata = session.InputMetadata[name];
            if (metadata.ElementDataType == TensorElementType.Int32)
            {
                var tensor = new DenseTensor<int>(new[] { 1, tokenIds.Length });
                for (int i = 0; i < tokenIds.Length; i++) tensor[0, i] = tokenIds[i];
                return NamedOnnxValue.CreateFromTensor(name, tensor);
            }

            var longTensor = new DenseTensor<long>(new[] { 1, tokenIds.Length });
            for (int i = 0; i < tokenIds.Length; i++) longTensor[0, i] = tokenIds[i];
            return NamedOnnxValue.CreateFromTensor(name, longTensor);
        }

        public NamedOnnxValue CreateFloatInput(InferenceSession session, string preferredName, float[] values, int[] dimensions)
        {
            string name = ResolveInputName(session, preferredName);
            var metadata = session.InputMetadata[name];
            if (metadata.ElementDataType == TensorElementType.Float16)
            {
                var fp16 = new Float16[values.Length];
                for (int i = 0; i < values.Length; i++) fp16[i] = (Float16)values[i];
                return NamedOnnxValue.CreateFromTensor(name, new DenseTensor<Float16>(fp16, dimensions));
            }

            return NamedOnnxValue.CreateFromTensor(name, new DenseTensor<float>(values, dimensions));
        }

        public static float[] ReadFloatTensor(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs, string preferredName)
        {
            DisposableNamedOnnxValue selected = null;
            foreach (var output in outputs)
            {
                if (selected == null) selected = output;
                if (string.Equals(output.Name, preferredName, StringComparison.Ordinal))
                {
                    selected = output;
                    break;
                }
            }

            if (selected == null)
                throw new InvalidOperationException("Inference produced no outputs.");

            try
            {
                var h = selected.AsTensor<Float16>();
                var arr = new float[h.Length];
                int i = 0;
                foreach (var value in h) arr[i++] = (float)value;
                return arr;
            }
            catch (InvalidCastException)
            {
                var f = selected.AsTensor<float>();
                var arr = new float[f.Length];
                int i = 0;
                foreach (var value in f) arr[i++] = value;
                return arr;
            }
        }

        public static string ResolveInputName(InferenceSession session, string preferredName)
        {
            foreach (var key in session.InputMetadata.Keys)
            {
                if (string.Equals(key, preferredName, StringComparison.Ordinal))
                    return key;
            }

            foreach (var key in session.InputMetadata.Keys)
                return key;

            throw new InvalidOperationException("Session has no inputs.");
        }

        private SessionOptions CreateOptions()
        {
            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            options.EnableCpuMemArena = true;
            options.EnableMemoryPattern = true;

            if (_providerPreference == OnnxExecutionProviderPreference.PreferCuda)
                options.AppendExecutionProvider_CUDA(0);

            return options;
        }
#endif

        public static bool IsCudaProviderFailure(Exception ex)
        {
            var text = ex == null ? string.Empty : ex.ToString();
            return text.IndexOf("cuda", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("provider", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("onnxruntime_providers", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
