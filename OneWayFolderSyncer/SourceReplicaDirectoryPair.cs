using FolderSyncing;

internal class SourceReplicaDirectoryPair
{
    public IndexedDirectory source { get; }
    public IndexedDirectory replica { get; }

    public SourceReplicaDirectoryPair(IndexedDirectory source, IndexedDirectory replica)
    {
        this.source = source;
        this.replica = replica;
    }
}
