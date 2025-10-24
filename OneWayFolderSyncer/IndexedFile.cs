using System.Diagnostics;
using System.Security.Cryptography;

namespace FolderSyncing
{
    internal class IndexedFile
    {
        /// <summary>
        /// For now the name of file is used as identifier - this causes issues with renamed files.
        /// Alternative could be windows-specific fileID retrieved by GetFileInformationByHandle
        /// or using contentHash as id
        /// </summary>
        public string fileId { get; }
        public long Size => fileInfo.Length;
        public string FilePath => fileInfo.FullName;
        public string FileName => fileInfo.Name;
        private FileInfo fileInfo;

        public IndexedFile(string sourceFilePath)
        {
            this.fileInfo = new FileInfo(sourceFilePath);
            this.fileId = this.fileInfo.Name;
        }

        public byte[] CalculateContentHash()
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream fileStream = File.OpenRead(FilePath))
                {
                    byte[] hashBytes = md5.ComputeHash(fileStream);
                    return hashBytes;
                }
            }

        }

        internal bool ConentHashEquals(IndexedFile other)
        {
            byte[] thisHash = CalculateContentHash();
            byte[] otherHash = other.CalculateContentHash();
            for (int i = 0; i < thisHash.Length; i++)
            {
                if (thisHash[i] != otherHash[i]) return false;
            }
            return true;
        }
    }
}