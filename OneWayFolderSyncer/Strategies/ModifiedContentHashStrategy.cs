namespace FolderSyncing.Strategies
{
    using FolderSyncing.Core;

    /// <summary>
    /// A file is considered to be changed when it content changes. 
    /// MD5 hash on the content is used to decide.
    /// </summary>
    public class ModifiedContentHashStrategy : IModifiedStrategy
    {
        public bool FileHasChanged(IndexedFile source, IndexedFile replica)
        {
            return source.ContentHashEquals(replica);
        }

        public string MethodDescription()
        {
            return "MD5 file content hashing";
        }
    }
}
