namespace FolderSyncing.Core
{
    using FolderSyncing.Strategies;
    using FolderSyncing.Utils;
    using Timer = System.Timers.Timer;

    public partial class OneWayFolderSyncer
    {
        private readonly string sourceFolderPath;
        private readonly string replicaFolderPath;
        private readonly Timer syncTimer;
        private readonly DirectorySyncer directorySyncer;
        private readonly IFileIdStrategy fileIdStrategy;
        private readonly IModifiedStrategy modifiedStrategy;

        public OneWayFolderSyncer(
            string sourceFolderPath,
            string replicaFolderPath,
            string logFilePath,
            int syncPeriodInSeconds,
            IFileIdStrategy fileIdStrategy,
            IModifiedStrategy modifiedStrategy
        )
        {
            this.sourceFolderPath = Path.GetFullPath(sourceFolderPath);
            this.replicaFolderPath = Path.GetFullPath(replicaFolderPath);
            logFilePath = Path.GetFullPath(logFilePath);

            this.fileIdStrategy = fileIdStrategy;
            this.modifiedStrategy = modifiedStrategy;

            Logger.Initialize(logFilePath);

            syncTimer = new(syncPeriodInSeconds * 1000d);
            syncTimer.Elapsed += (_, _) => SyncReplicaWithSource();
            syncTimer.AutoReset = true;

            directorySyncer = new(this);
        }

        public OneWayFolderSyncer(SyncConfig config)
            : this(
                config.SourcePath,
                config.ReplicaPath,
                config.LogPath,
                config.SyncPeriod,
                config.FileIdStrategy,
                config.ModifiedStrategy
            ) { }

        public void StartSyncing()
        {
            Logger.LogStart(
                sourceFolderPath,
                replicaFolderPath,
                syncTimer.Interval,
                modifiedStrategy
            );
            SyncReplicaWithSource();
            syncTimer.Start();
        }

        private void SyncReplicaWithSource()
        {
            directorySyncer.SyncDirectory(new(sourceFolderPath, fileIdStrategy, modifiedStrategy));
        }

        internal string MirrorPathToReplica(string sourcePath)
        {
            // turns C:\user\source\foo\foo.txt -> foo.txt
            string relativePath = Path.GetRelativePath(this.sourceFolderPath, sourcePath);
            // return C:\user\replica\foo\foo.txt
            return Path.Combine(this.replicaFolderPath, relativePath);
        }

        public void StopSyncing()
        {
            syncTimer.Stop();
            syncTimer.Dispose();
            Logger.LogStop();
        }
    }
}
