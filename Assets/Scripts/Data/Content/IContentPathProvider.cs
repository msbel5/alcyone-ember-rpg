using System.IO;

namespace EmberCrpg.Data.Content
{
    public interface IContentPathProvider
    {
        string ContentRootPath { get; }
    }

    public sealed class FixedContentPathProvider : IContentPathProvider
    {
        public FixedContentPathProvider(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        public string ContentRootPath { get; }
    }

    public sealed class ProjectStreamingAssetsContentPathProvider : IContentPathProvider
    {
        private readonly string _projectRootPath;

        public ProjectStreamingAssetsContentPathProvider(string projectRootPath)
        {
            _projectRootPath = projectRootPath;
        }

        public string ContentRootPath => Path.Combine(_projectRootPath, "Assets", "StreamingAssets", "Content");
    }
}
