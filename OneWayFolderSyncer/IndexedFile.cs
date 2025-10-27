using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace FolderSyncing
{
    public class IndexedFile
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

        public IndexedFile(string sourceFilePath, IFileIdStrategy fileIdStrategy)
        {
            this.fileInfo = new FileInfo(sourceFilePath);
            this.fileIdStrategy = fileIdStrategy;
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
                if (thisHash[i] != otherHash[i])
                    return false;
            }
            return true;
        }
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

    /// <summary>
    /// Uses combination of name and coennt of the file to uniquely indetify.
    /// When a file is renamed - look for a file that is not valid but has same conent -
    /// If such file exists we renmae that file and keep using it.
    /// </summary>
    public class ContentBasedIdStrategy : IFileIdStrategy
    {
        public string GetDirectoryId(IndexedDirectory dir)
        {
            return dir.directoryName;
        }

        public string GetFileId(IndexedFile file)
        {
            return Convert.ToBase64String(file.CalculateContentHash()) + file.FileName;
        }
    }
}
