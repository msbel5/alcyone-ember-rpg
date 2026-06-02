using System.IO;
using EmberCrpg.Data.Content;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Content
{
    internal static class ContentDatabaseTestContext
    {
        public static ContentDatabase Load()
        {
            return ContentDatabase.Load(new FixedContentPathProvider(FindContentRoot()));
        }

        private static string FindContentRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            Assert.Fail("Assets/StreamingAssets/Content was not found from the test directory.");
            return string.Empty;
        }
    }
}
