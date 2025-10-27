namespace FolderSyncing.Core
{
    using FolderSyncing.Strategies;

    public class IndexedDirectory : IHashable
    {
        private readonly DirectoryInfo directoryInfo;

        private readonly Dictionary<string, IndexedFile> indexedFiles = new();
        private readonly Dictionary<string, IndexedDirectory> indexedDirectories = new();
        private string cachedContentHash = "";

        public string DirectoryId => fileIdStrategy.GetDirectoryId(this);
        public string DirectoryPath => directoryInfo.FullName;
        public string DirectoryName => directoryInfo.Name;
        public DateTime LastModified => directoryInfo.LastWriteTimeUtc;
        private readonly IFileIdStrategy fileIdStrategy;
        private readonly IModifiedStrategy modifiedStrategy;

        internal IndexedDirectory(
            string directoryPath,
            IFileIdStrategy fileIdStrategy,
            IModifiedStrategy modifiedStrategy
        )
        {
            this.directoryInfo = new(directoryPath);
            this.fileIdStrategy = fileIdStrategy;
            this.modifiedStrategy = modifiedStrategy;

            if (modifiedStrategy is ModifiedContentHashStrategy)
            {
                this.cachedContentHash = CalculateContentHash();
            }
        }

        internal void BuildIndex()
        {
            indexedFiles.Clear();
            indexedDirectories.Clear();
            // handle files and subdirectories separately
            foreach (var file in directoryInfo.GetFiles())
            {
                IndexedFile indexedFile = new(file.FullName, fileIdStrategy, modifiedStrategy);
                indexedFiles.Add(indexedFile.FileId, indexedFile);
            }
            foreach (var dir in directoryInfo.GetDirectories())
            {
                IndexedDirectory indexedDirectory = new(
                    dir.FullName,
                    fileIdStrategy,
                    modifiedStrategy
                );
                indexedDirectories.Add(indexedDirectory.DirectoryId, indexedDirectory);
                indexedDirectory.BuildIndex();
            }
        }

        internal bool ContainsFile(IndexedFile file)
        {
            return indexedFiles.ContainsKey(file.FileId);
        }

        internal IndexedFile GetFileById(string id)
        {
            return this.indexedFiles.GetValueOrDefault(id, null);
        }

        internal IndexedFile[] GetIndexedFiles()
        {
            return indexedFiles.Values.ToArray();
        }

        internal IndexedDirectory[] GetIndexedSubdirs()
        {
            return indexedDirectories.Values.ToArray();
        }

        internal IndexedDirectory GetDirById(string directoryId)
        {
            return indexedDirectories.GetValueOrDefault(directoryId, null);
        }

        internal void IndexDirectory(IndexedDirectory sourceSubDir)
        {
            indexedDirectories.Add(sourceSubDir.DirectoryId, sourceSubDir);
        }

        private string CalculateContentHash()
        {
            var fileConentHashes = indexedFiles
                .OrderBy(f => f.Value.FileName)
                .Select(f => f.Value.GetContentHash() + f.Value.FileName);

            var directoryConentHashes = indexedDirectories
                .OrderBy(f => f.Value.DirectoryName)
                .Select(f => f.Value.GetContentHash() + f.Value.DirectoryName);
            string combinedHash = string.Join("", fileConentHashes.Concat(directoryConentHashes));
            return combinedHash;
        }

        internal bool ContainsDirectory(IndexedDirectory dir)
        {
            return GetDirById(dir.DirectoryId) != null;
        }

        public string GetContentHash()
        {
            if (string.IsNullOrEmpty(cachedContentHash))
            {
                cachedContentHash = CalculateContentHash();
            }
            return cachedContentHash;
        }

        public bool ContentHashEquals(IHashable other)
        {
            return GetContentHash() == other.GetContentHash();
        }
    }
}
