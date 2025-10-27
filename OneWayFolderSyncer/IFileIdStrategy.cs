namespace FolderSyncing
{
    public interface IFileIdStrategy
    {
        public string GetFileId(IndexedFile file);
        public string GetDirectoryId(IndexedDirectory dir);
    }
}
