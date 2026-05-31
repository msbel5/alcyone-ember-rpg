using System.Reflection;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class DiffusionPipelineClampTests
    {
        [TestCase("EmberCrpg.Simulation.Forge.SdxlTurboPipeline")]
        [TestCase("EmberCrpg.Simulation.Forge.Sd15LcmPipeline")]
        public void ClampDimension_SnapsToMultipleOfEight(string typeName)
        {
            var type = typeof(EmberCrpg.Simulation.Forge.OnnxAssetForge).Assembly.GetType(typeName, true);
            var method = type.GetMethod("ClampDimension", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method.Invoke(null, new object[] { 257 }), Is.EqualTo(256));
            Assert.That(method.Invoke(null, new object[] { 263 }), Is.EqualTo(256));
        }

        [Test]
        public void SdxlEulerSchedule_FourSteps_MatchesScaledLinearMilestones()
        {
            var type = typeof(EmberCrpg.Simulation.Forge.OnnxAssetForge).Assembly.GetType(
                "EmberCrpg.Simulation.Forge.SdxlTurboPipeline",
                true);
            var method = type.GetMethod("BuildEulerSchedule", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) Assert.Ignore("SDXL runtime schedule is compiled only when USE_ONNX_RUNTIME is defined.");

            var args = new object[] { 4, null, null };
            method.Invoke(null, args);

            var timesteps = (float[])args[1];
            var sigmas = (float[])args[2];

            Assert.That(timesteps, Is.EqualTo(new[] { 999f, 666f, 333f, 0f }));
            Assert.That(sigmas, Has.Length.EqualTo(5));
            Assert.That(sigmas[0], Is.EqualTo(14.6146f).Within(0.001f));
            Assert.That(sigmas[1], Is.EqualTo(2.9183f).Within(0.001f));
            Assert.That(sigmas[2], Is.EqualTo(0.9324f).Within(0.001f));
            Assert.That(sigmas[3], Is.EqualTo(0.0292f).Within(0.001f));
            Assert.That(sigmas[4], Is.EqualTo(0f));
        }
    }
}
