namespace FolderSyncing
{
    public class ModifiedContentHashStrategy : IModifiedStrategy
    {
        public bool DirHasChanged(IndexedDirectory source, IndexedDirectory replica)
        {
            return source.ContentHashEquals(replica);
        }

        public bool FileHasChanged(IndexedFile source, IndexedFile replica)
        {
            return source.ContentHashEquals(replica);
        }
    }
}
