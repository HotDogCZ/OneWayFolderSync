namespace FolderSyncing.Core
{
    internal class SourceReplicaDirectoryPair
    {
        public IndexedDirectory Source { get; }
        public IndexedDirectory Replica { get; }

        public SourceReplicaDirectoryPair(IndexedDirectory source, IndexedDirectory replica)
        {
            this.Source = source;
            this.Replica = replica;
        }
    }
}
