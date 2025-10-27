namespace FolderSyncing.Strategies
{
    using FolderSyncing.Core;

    public interface IModifiedStrategy
    {
        public bool FileHasChanged(IndexedFile source, IndexedFile replica);
        public string MethodDescription();
    }
}
