using System.Security.Cryptography;
using System.Text;

namespace FolderSyncing
{
    public class IndexedDirectory : IHashable
    {
        private DirectoryInfo directoryInfo;

        Dictionary<string, IndexedFile> indexedFiles = new();
        Dictionary<string, IndexedDirectory> indexedDirectories = new();
        private readonly string contentHash;

        public string directoryId => fileIdStrategy.GetDirectoryId(this);

        private readonly IFileIdStrategy fileIdStrategy;
        public readonly string directoryName;
        public string DirectoryPath => directoryInfo.FullName;

        internal IndexedDirectory(string directoryPath, IFileIdStrategy fileIdStrategy)
        {
            this.directoryInfo = new(directoryPath);
            this.fileIdStrategy = fileIdStrategy;
            this.directoryName = directoryInfo.Name;
            this.contentHash = CalculateContentHash();
        }

        internal void BuildIndex()
        {
            indexedFiles.Clear();
            indexedDirectories.Clear();
            // handle files and subdirectories separately
            foreach (var file in directoryInfo.GetFiles())
            {
                IndexedFile indexedFile = new(file.FullName, fileIdStrategy);
                indexedFiles.Add(indexedFile.fileId, indexedFile);
            }
            foreach (var dir in directoryInfo.GetDirectories())
            {
                IndexedDirectory indexedDirectory = new(dir.FullName, fileIdStrategy);
                indexedDirectories.Add(indexedDirectory.directoryId, indexedDirectory);
                indexedDirectory.BuildIndex();
            }
        }

        internal bool ContainsFile(IndexedFile file)
        {
            return indexedFiles.ContainsKey(file.fileId);
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
            indexedDirectories.Add(sourceSubDir.directoryId, sourceSubDir);
        }

        private string CalculateContentHash()
        {
            var fileConentHashes = indexedFiles
                .OrderBy(f => f.Value.FileName)
                .Select(f => f.Value.GetContentHash() + f.Value.FileName);

            var directoryConentHashes = indexedDirectories
                .OrderBy(f => f.Value.directoryName)
                .Select(f => f.Value.GetContentHash() + f.Value.directoryName);
            string combinedHash = string.Join("", fileConentHashes.Concat(directoryConentHashes));
            return combinedHash;
        }


        internal bool ContainsDirectory(IndexedDirectory dir)
        {
            return GetDirById(dir.directoryId) != null;
        }

        public string GetContentHash()
        {
            return contentHash;
        }

        public bool ContentHashEquals(IHashable other)
        {
            return GetContentHash() == other.GetContentHash();
        }
    }
}
