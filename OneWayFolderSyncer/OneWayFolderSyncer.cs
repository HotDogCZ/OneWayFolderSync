using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Timers;
using Microsoft.VisualBasic.FileIO;
using Timer = System.Timers.Timer;

namespace FolderSyncing
{
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

            if (!Directory.Exists(this.sourceFolderPath))
            {
                throw new DirectoryNotFoundException(sourceFolderPath);
            }
            if (!Directory.Exists(this.replicaFolderPath))
            {
                throw new DirectoryNotFoundException(replicaFolderPath);
            }
            if (!Directory.Exists(Path.GetDirectoryName(logFilePath)))
            {
                throw new DirectoryNotFoundException(logFilePath);
            }
            this.fileIdStrategy = fileIdStrategy;
            this.modifiedStrategy = modifiedStrategy;

            Logger.Initialize(logFilePath);

            syncTimer = new(syncPeriodInSeconds * 1000d);
            syncTimer.Elapsed += (_, _) => SyncReplicaWithSource();
            syncTimer.AutoReset = true;

            directorySyncer = new(this);
        }

        public void StartSyncing()
        {
            Logger.LogStart(sourceFolderPath, replicaFolderPath, syncTimer.Interval);
            SyncReplicaWithSource();
            syncTimer.Start();
        }

        private void SyncReplicaWithSource()
        {
            Console.WriteLine("Syncing " + replicaFolderPath + " to match " + sourceFolderPath);
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
