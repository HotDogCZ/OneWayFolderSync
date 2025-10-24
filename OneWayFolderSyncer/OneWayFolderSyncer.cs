using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Timers;
using FolderSyncing;
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
            SyncReplicaWithSource();
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
            List<IndexedFile> unconfirmedReplicatedFiles = replicaDirectory
                .GetIndexedFiles()
                .ToList();

            // Replicate missing files
            List<SourceReplicaFilePair> potentiallyUpdatedFiles = new();

            foreach (IndexedFile sourceFile in sourceDirectory.GetIndexedFiles())
            {
                IndexedFile replicatedFile = replicaDirectory.GetFileById(sourceFile.fileId);
                if (replicatedFile == null)
                {
                    // sourceFile is not in replica
                    ReplicateFile(sourceFile);
                }
                else
                {
                    // sourceFile is in replica
                    potentiallyUpdatedFiles.Add(new(sourceFile, replicatedFile));
                }
            }
            // Update changed files
            foreach (SourceReplicaFilePair updated in potentiallyUpdatedFiles)
            {
                IndexedFile replica = updated.replica;
                IndexedFile source = updated.source;
                // If files are different size -- definitely changed
                if (replica.Size != source.Size)
                {
                    UpdateFile(source, replica);
                    Console.WriteLine("Size has changed");
                }

                // Compare content hashes
                if (!source.ConentHashEquals(replica))
                {
                    UpdateFile(source, replica);
                    Console.WriteLine("Conent has changed");
                }
            }

            // Delete irrelevant files
            List<IndexedFile> toRemove = new();
            foreach (IndexedFile file in unconfirmedReplicatedFiles)
            {
                bool sourceExists = sourceDirectory.ContainsFile(file);
                if (!sourceExists)
                {
                    DeleteFile(file);
                }
                toRemove.Remove(file);
            }
            unconfirmedReplicatedFiles.RemoveAll((x) => toRemove.Contains(x));

            HandleDirectorySyncing(sourceDirectory, replicaDirectory);
        }

        private void HandleDirectorySyncing(
            IndexedDirectory sourceDirectory,
            IndexedDirectory replicaDirectory
        )
        {
            List<SourceReplicaDirectoryPair> potentiallyUpdated = new();
            foreach (IndexedDirectory sourceSubDir in sourceDirectory.GetIndexedSubdirs())
            {
                IndexedDirectory replicatedDirectory = replicaDirectory.GetDirById(
                    replicaDirectory.directoryId
                );
                if (replicatedDirectory == null)
                {
                    ReplicateDirectory(sourceSubDir);
                }
                else
                {
                    potentiallyUpdated.Add(new(sourceSubDir, replicatedDirectory));
                }
            }
            foreach (var dir in potentiallyUpdated)
            {
                HandleDirectorySyncing(dir.source, dir.replica);
            }
        }

        private void UpdateFile(IndexedFile source, IndexedFile replica)
        {
            LogUpdatedFile(source, replica);
            DeleteFile(replica);
            ReplicateFile(source);
        }

        private void DeleteFile(IndexedFile file)
        {
            File.Delete(file.FilePath);
            LogDeletedFile(file);
        }

        private void ReplicateFile(IndexedFile file)
        {
            File.Copy(file.FilePath, SourcePathToReplicaPath(file.FilePath));
            LogReplicatedFile(file);
        }

        private void ReplicateDirectory(IndexedDirectory sourceSubDir)
        {
            LogReplicatedDirectory(sourceSubDir);
            Directory.CreateDirectory(SourcePathToReplicaPath(sourceSubDir.DirectoryPath));
            foreach (var file in sourceSubDir.GetIndexedFiles())
            {
                ReplicateFile(file);
            }
            foreach (var subdir in sourceSubDir.GetIndexedSubdirs())
            {
                ReplicateDirectory(subdir);
            }
        }

        private void LogReplicatedDirectory(IndexedDirectory dir)
        {
            Console.WriteLine($"Replicated directory {dir.DirectoryPath} from source.");
        }

        private void LogReplicatedFile(IndexedFile file)
        {
            Console.WriteLine($"Replicated {file.FileName} from source.");
        }

        private void LogDeletedFile(IndexedFile file)
        {
            Console.WriteLine(
                $"Deleted {file.FileName} from replica -- file no longer exists in source."
            );
        }

        private void LogUpdatedFile(IndexedFile source, IndexedFile replica)
        {
            Console.WriteLine(
                $"Updated {replica.FileName} in replica to match {source.FileName} in source."
            );
        }

        internal void StopSyncing()
        {
            syncTimer.Stop();
            syncTimer.Dispose();
        }

        internal string SourcePathToReplicaPath(string sourcePath)
        {
            // turns C:\user\source\foo\foo.txt -> foo.txt
            string relativePath = Path.GetRelativePath(this.sourceFolderPath, sourcePath);
            // return C:\user\replica\foo\foo.txt
            return Path.Combine(this.replicaFolderPath, relativePath);
        }
    }
}

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
