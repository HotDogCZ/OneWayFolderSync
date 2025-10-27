namespace FolderSyncing.Core
{
    using System.Security.Cryptography;
    using FolderSyncing.Strategies;

    public class IndexedFile : IHashable
    {
        public string FileId => fileIdStrategy.GetFileId(this);
        public long Size => fileInfo.Length;

        public string FilePath => fileInfo.FullName;

        public string FileName => fileInfo.Name;

        public DateTime LastModified => fileInfo.LastWriteTimeUtc;
        private readonly IFileIdStrategy fileIdStrategy;
        private readonly IModifiedStrategy modifiedStrategy;
        private readonly FileInfo fileInfo;
        private string cachedContentHash = "";

        public bool HasChanged(IndexedFile other) => modifiedStrategy.FileHasChanged(this, other);

        public IndexedFile(
            string sourceFilePath,
            IFileIdStrategy fileIdStrategy,
            IModifiedStrategy modifiedStrategy
        )
        {
            this.fileInfo = new FileInfo(sourceFilePath);
            this.fileIdStrategy = fileIdStrategy;
            this.modifiedStrategy = modifiedStrategy;

            if (modifiedStrategy is ModifiedContentHashStrategy)
            {
                cachedContentHash = CalculateContentHash();
            }
        }

        public string CalculateContentHash()
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream fileStream = File.OpenRead(FilePath))
                {
                    byte[] hashBytes = md5.ComputeHash(fileStream);
                    return Convert.ToBase64String(hashBytes);
                }
            }
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
            return GetContentHash() != other.GetContentHash();
        }
    }
}
