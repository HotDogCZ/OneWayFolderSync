using FolderSyncing;

internal class SourceReplicaFilePair
{
    public IndexedFile source { get; }
    public IndexedFile replica { get; }

    public SourceReplicaFilePair(IndexedFile source, IndexedFile replica)
    {
        this.source = source;
        this.replica = replica;
    }
}
