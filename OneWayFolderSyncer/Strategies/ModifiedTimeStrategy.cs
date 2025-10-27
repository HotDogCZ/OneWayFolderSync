namespace FolderSyncing.Strategies
{
    using FolderSyncing.Core;

    /// <summary>
    /// File is considered to be changed when the LastModifiedTime changes.
    /// This might now work properly when files are edited in a specific way
    /// that does not change the modified time.
    /// </summary>
    public class ModifiedTimeStrategy : IModifiedStrategy
    {
        public bool FileHasChanged(IndexedFile source, IndexedFile replica)
        {
            return source.LastModified != replica.LastModified;
        }

        public string MethodDescription()
        {
            return "Last file modification time";
        }
    }
}
