using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Timers;
using Timer = System.Timers.Timer;

namespace FolderSyncing
{
    public class OneWayFolderSyncer
    {
        private string sourceFolderPath;
        private string replicaFolderPath;
        private string logFilePath;
        private int syncPeriodSeconds;
        private Timer syncTimer;
        private IndexedDirectory sourceDirectory;
        private IndexedDirectory replicaDirectory;

        public OneWayFolderSyncer(
            string sourceFolderPath,
            string replicaFolderPath,
            string logFilePath,
            int syncPeriodInSeconds
        )
        {
            this.sourceFolderPath = sourceFolderPath;
            this.replicaFolderPath = replicaFolderPath;
            this.logFilePath = logFilePath;
            this.syncPeriodSeconds = syncPeriodInSeconds;

            syncTimer = new(syncPeriodInSeconds * 1000d);
            syncTimer.Elapsed += onSyncTimerElapsed;
            syncTimer.AutoReset = true;

            sourceDirectory = new(sourceFolderPath);
            replicaDirectory = new(replicaFolderPath);
        }

        internal void StartSyncing()
        {
            sourceDirectory.BuildIndex();
            replicaDirectory.BuildIndex();
            syncTimer.Start();
        }

        private void onSyncTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            SyncReplicaWithSource();
        }

        private void SyncReplicaWithSource()
        {
            Console.WriteLine("Syncing " + replicaFolderPath + " to match " + sourceFolderPath);
            sourceDirectory.BuildIndex();
            replicaDirectory.BuildIndex();
            // Replicate missing files
            foreach (IndexedFile file in sourceDirectory.GetIndexedFiles())
            {
                bool replicaExists = replicaDirectory.ContainsFile(file);
                if (!replicaExists)
                {
                    ReplicateFile(file);
                }
            }

            // Delete irrelevant files
            foreach (IndexedFile file in replicaDirectory.GetIndexedFiles())
            {
                bool sourceExists = sourceDirectory.ContainsFile(file);
                if (!sourceExists)
                {
                    DeleteFile(file);
                }
            }

            // Update changed files
        }

        private void DeleteFile(IndexedFile file)
        {
            File.Delete(file.filePath);
            LogDeletedFile(file);
        }

        private void ReplicateFile(IndexedFile file)
        {
            File.Copy(file.filePath, Path.Combine(this.replicaFolderPath, file.fileName));
            LogReplicatedFile(file);
        }

        private void LogReplicatedFile(IndexedFile file)
        {
            Console.WriteLine("Replicated " + file.fileName + " from source.");
        }

        private void LogDeletedFile(IndexedFile file)
        {
            Console.WriteLine(
                "Deleted " + file.fileName + " from replica -- file no longer exists in source."
            );
        }

        internal void StopSyncing()
        {
            syncTimer.Stop();
            syncTimer.Dispose();
        }
    }

    internal class IndexedDirectory
    {
        private string directoryPath;
        Dictionary<string, IndexedFile> indexedFiles = new();

        internal IndexedDirectory(string directoryPath)
        {
            this.directoryPath = directoryPath;
        }

        internal void BuildIndex()
        {
            indexedFiles.Clear();
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                IndexedFile indexedFile = new(file);
                indexedFiles.Add(indexedFile.fileName, indexedFile);
            }
        }

        internal bool ContainsFile(IndexedFile file)
        {
            return indexedFiles.ContainsKey(file.fileName);
        }

        internal IndexedFile[] GetIndexedFiles()
        {
            return indexedFiles.Values.ToArray();
        }
    }

}
