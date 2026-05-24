using System.IO;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class GenerationFailureLogTests
    {
        [Test]
        public void AppendWritesOneValidJsonLineWithoutRawPrompt()
        {
            var path = Path.Combine(Path.GetTempPath(), "ember-generation-failures-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var log = new GenerationFailureLog(path);
                log.Append("item_sword", "item", "failed:secret-token", "OnnxRuntimeException", "sha256:abc", 42);
                var lines = File.ReadAllLines(path);
                Assert.That(lines.Length, Is.EqualTo(1));
                Assert.That(lines[0], Does.Contain("\"entryId\":\"item_sword\""));
                Assert.That(lines[0], Does.Contain("\"promptHash\":\"sha256:abc\""));
                Assert.That(lines[0], Does.Not.Contain("\"prompt\""));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
    }
}
