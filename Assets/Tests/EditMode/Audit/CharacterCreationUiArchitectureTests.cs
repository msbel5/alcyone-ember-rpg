using System.IO;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    public sealed class CharacterCreationUiArchitectureTests
    {
        [Test]
        public void CharacterCreationUi_DoesNotImportDomainOrSimulation()
        {
            var source = File.ReadAllText(FindProjectFile("Assets", "Scripts", "Presentation", "Ember", "UI", "CharacterCreationUI.cs"));

            Assert.That(source, Does.Not.Contain("using EmberCrpg.Domain."));
            Assert.That(source, Does.Not.Contain("using EmberCrpg.Simulation."));
        }

        private static string FindProjectFile(params string[] segments)
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                var parts = new string[segments.Length + 1];
                parts[0] = directory.FullName;
                for (int i = 0; i < segments.Length; i++)
                    parts[i + 1] = segments[i];

                var candidate = Path.Combine(parts);
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }

            Assert.Fail("Could not locate project file: " + string.Join("/", segments));
            return null;
        }
    }
}
