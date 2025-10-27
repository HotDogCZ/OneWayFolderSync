namespace FolderSyncing.Strategies
{
    public interface IHashable
    {
        internal string GetContentHash();
        internal bool ContentHashEquals(IHashable other);
    }
}
