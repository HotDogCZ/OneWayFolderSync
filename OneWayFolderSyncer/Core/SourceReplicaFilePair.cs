using FolderSyncing;

namespace FolderSyncing.Core
{
    internal class SourceReplicaFilePair
    {
        public IndexedFile Source { get; }
        public IndexedFile Replica { get; }

        public SourceReplicaFilePair(IndexedFile source, IndexedFile replica)
        {
            this.Source = source;
            this.Replica = replica;
        }
    }
}
