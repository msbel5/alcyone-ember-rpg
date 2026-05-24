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
    }
}
