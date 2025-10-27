namespace FolderSyncing
{
    public class ModifiedTimeStrategy : IModifiedStrategy
    {
        public bool DirHasChanged(IndexedDirectory source, IndexedDirectory replica)
        {
            return source.LastModified == replica.LastModified;
        }

        public bool FileHasChanged(IndexedFile source, IndexedFile replica)
        {
            return source.LastModified != replica.LastModified;
        }
    }
}
