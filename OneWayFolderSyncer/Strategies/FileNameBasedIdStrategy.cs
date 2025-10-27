namespace FolderSyncing.Strategies
{
    using FolderSyncing.Core;

    /// <summary>
    /// Strategy that uses the name of file to uniquely identfiy each file.
    /// When a file is renmaed we loose the idea -
    ///  the corresponing file can be found by content or a volume file ID could be used.
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
