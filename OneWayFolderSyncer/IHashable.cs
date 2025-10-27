namespace FolderSyncing
{
    public interface IHashable
    {
        internal string GetContentHash();
        internal bool ContentHashEquals(IHashable other);
    }
}
