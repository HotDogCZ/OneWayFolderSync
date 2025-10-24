namespace FolderSyncing
{
    internal class IndexedDirectory
    {
        private DirectoryInfo directoryInfo;

        Dictionary<string, IndexedFile> indexedFiles = new();
        Dictionary<string, IndexedDirectory> indexedDirectories = new();

        public string directoryId { get; }
        public string DirectoryPath => directoryInfo.FullName;

        internal IndexedDirectory(string directoryPath)
        {
            this.directoryInfo = new(directoryPath);
            this.directoryId = directoryInfo.Name;
        }

        internal void BuildIndex()
        {
            indexedFiles.Clear();
            indexedDirectories.Clear();
            // handle files and subdirectories separately
            foreach (var file in directoryInfo.GetFiles())
            {
                IndexedFile indexedFile = new(file.FullName);
                indexedFiles.Add(indexedFile.fileId, indexedFile);
            }
            foreach (var dir in directoryInfo.GetDirectories())
            {
                IndexedDirectory indexedDirectory = new(dir.FullName);
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
    }
}
