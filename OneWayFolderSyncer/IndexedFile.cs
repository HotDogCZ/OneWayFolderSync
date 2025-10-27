using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace FolderSyncing
{
    public class IndexedFile : IHashable
    {
        /// <summary>
        /// For now the name of file is used as identifier - this causes issues with renamed files.
        /// Alternative could be windows-specific fileID retrieved by GetFileInformationByHandle
        /// or using contentHash as id
        /// </summary>
        private readonly IFileIdStrategy fileIdStrategy;
        public string fileId => fileIdStrategy.GetFileId(this);
        public long Size => fileInfo.Length;

        public string FilePath => fileInfo.FullName;

        public string FileName => fileInfo.Name;

        private readonly FileInfo fileInfo;
        private readonly string contentHash;

        public IndexedFile(string sourceFilePath, IFileIdStrategy fileIdStrategy)
        {
            this.fileInfo = new FileInfo(sourceFilePath);
            this.fileIdStrategy = fileIdStrategy;
            contentHash = CalculateContentHash();
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
            return contentHash;
        }

        public bool ContentHashEquals(IHashable other)
        {
            return GetContentHash() == other.GetContentHash();
        }
    }

    public interface IHashable
    {
        internal string GetContentHash();
        internal bool ContentHashEquals(IHashable other);
    }

    public interface IFileIdStrategy
    {
        public string GetFileId(IndexedFile file);
        public string GetDirectoryId(IndexedDirectory dir);
    }

    /// <summary>
    /// Strategy that uses the name of file to uniquely identfiy each file.
    /// This strategy will not be able to detect when a file is renamed -> it will be deleted and coppied again
    /// </summary>
    public class FileNameBasedIdStrategy : IFileIdStrategy
    {
        public string GetFileId(IndexedFile file)
        {
            return file.FileName;
        }

        public string GetDirectoryId(IndexedDirectory dir)
        {
            return dir.directoryName;
        }
    }
}
