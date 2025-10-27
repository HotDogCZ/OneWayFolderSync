namespace FolderSyncing.Utils
{
    using FolderSyncing.Strategies;

    public class SyncConfig
    {
        public SyncConfig(
            string sourcePath,
            string replicaPath,
            string logPath,
            int syncPeriod,
            IModifiedStrategy modifiedStrategy,
            IFileIdStrategy defaultFileIdStrategy
        )
        {
            SourcePath = sourcePath;
            ReplicaPath = replicaPath;
            LogPath = logPath;
            SyncPeriod = syncPeriod;
            ModifiedStrategy = modifiedStrategy;
            FileIdStrategy = defaultFileIdStrategy;
        }

        public string SourcePath { get; set; }
        public string ReplicaPath { get; set; }
        public string LogPath { get; set; }
        public int SyncPeriod { get; set; }
        public IModifiedStrategy ModifiedStrategy { get; set; }
        public IFileIdStrategy FileIdStrategy { get; set; }
    }
}
