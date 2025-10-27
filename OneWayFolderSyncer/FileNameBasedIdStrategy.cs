namespace FolderSyncing
{
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
            return dir.DirectoryName;
        }
    }
}
