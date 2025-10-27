namespace FolderSyncing
{
    public interface IModifiedStrategy
    {
        public bool FileHasChanged(IndexedFile source, IndexedFile replica);
        public bool DirHasChanged(IndexedDirectory source, IndexedDirectory replica);
    }
}
