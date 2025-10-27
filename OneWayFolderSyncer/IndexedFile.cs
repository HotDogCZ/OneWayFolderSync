using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.VisualBasic;

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
        private readonly IModifiedStrategy modifiedStrategy;
        public string FileId => fileIdStrategy.GetFileId(this);
        public long Size => fileInfo.Length;

        public string FilePath => fileInfo.FullName;

        public string FileName => fileInfo.Name;

        public DateTime LastModified => fileInfo.LastWriteTimeUtc;

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
                    Console.WriteLine("Hash calc");

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
            Console.WriteLine(
                $"Other: {((IndexedFile)other).FileName}  {other.GetContentHash()}. This: {((IndexedFile)this).FileName}  {this.GetContentHash()}"
            );
            return GetContentHash() != other.GetContentHash();
        }
    }
}
