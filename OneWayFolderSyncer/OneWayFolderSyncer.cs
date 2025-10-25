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
    public class OneWayFolderSyncer
    {
        private string sourceFolderPath;
        private string replicaFolderPath;
        private string logFilePath;
        private int syncPeriodSeconds;
        private Timer syncTimer;

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
            SyncDirectory(new(sourceFolderPath));
        }

        private void SyncDirectory(IndexedDirectory currentSourceDirectory)
        {
            string replicaPath = SourcePathToReplicaPath(currentSourceDirectory.DirectoryPath);
            IndexedDirectory currentReplicaDirectory = new IndexedDirectory(replicaPath);
            currentSourceDirectory.BuildIndex();
            currentReplicaDirectory.BuildIndex();
            SyncFiles(currentSourceDirectory, currentReplicaDirectory);
            SyncDirectories(currentSourceDirectory, currentReplicaDirectory);
            // sync files on top level
            //foreach directory - recursion

            foreach (var dir in currentSourceDirectory.GetIndexedSubdirs())
            {
                SyncDirectory(dir);
                Console.WriteLine($"Syncing {dir.directoryId}");
            }
        }

        private void SyncFiles(IndexedDirectory sourceDirectory, IndexedDirectory replicaDirectory)
        {
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
                    // It could have been updated.
                    potentiallyUpdatedFiles.Add(new(sourceFile, replicatedFile));
                    // Its existence is confirmed - it is supposed to be in replica folder.
                    unconfirmedReplicatedFiles.RemoveAll((x) => x.fileId == sourceFile.fileId);
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
            foreach (IndexedFile file in unconfirmedReplicatedFiles)
            {
                bool sourceExists = sourceDirectory.ContainsFile(file);
                if (!sourceExists)
                {
                    DeleteFile(file);
                }
            }
        }

        private void SyncDirectories(
            IndexedDirectory sourceDirectory,
            IndexedDirectory replicaDirectory
        )
        {
            List<SourceReplicaDirectoryPair> potentiallyUpdated = new();
            List<IndexedDirectory> unconfirmedReplicaDirectories = replicaDirectory
                .GetIndexedSubdirs()
                .ToList();
            ;
            // add missing directories
            foreach (IndexedDirectory sourceSubDir in sourceDirectory.GetIndexedSubdirs())
            {
                IndexedDirectory replicatedDirectory = replicaDirectory.GetDirById(
                    sourceSubDir.directoryId
                );
                if (replicatedDirectory == null)
                {
                    ReplicateDirectory(sourceSubDir);
                    replicaDirectory.IndexDirectory(sourceSubDir);
                }
                else
                {
                    potentiallyUpdated.Add(new(sourceSubDir, replicatedDirectory));
                    unconfirmedReplicaDirectories.RemoveAll(
                        (x) => x.directoryId == sourceSubDir.directoryId
                    );
                }
            }
            // check if directory was updated
            foreach (SourceReplicaDirectoryPair subDir in potentiallyUpdated)
            {
                if (!subDir.source.ContentHashEquals(subDir.replica))
                {
                    SyncDirectory(subDir.source);
                }
            }

            foreach (IndexedDirectory dir in unconfirmedReplicaDirectories)
            {
                DeleteDirectory(dir);
            }
        }

        private void UpdateFile(IndexedFile source, IndexedFile replica)
        {
            LogUpdatedFile(source, replica);
            DeleteFile(replica);
            ReplicateFile(source);
        }

        private void DeleteDirectory(IndexedDirectory dir)
        {
            try
            {
                LogDeletingDirectory(dir);
                foreach (IndexedDirectory subdir in dir.GetIndexedSubdirs())
                {
                    DeleteDirectory(subdir);
                }
                foreach (IndexedFile file in dir.GetIndexedFiles())
                {
                    DeleteFile(file);
                }
                Directory.Delete(dir.DirectoryPath);
                LogDeletedDirectory(dir);
            }
            catch (IOException e)
            {
                LogExcecption(e);
            }
        }

        private void LogDeletingDirectory(IndexedDirectory dir)
        {
            Console.WriteLine($"Trying to delete directory {dir.directoryId}...");
        }

        private void LogDeletedDirectory(IndexedDirectory dir)
        {
            Console.WriteLine($"Fully deleted directory {dir.directoryId}");
        }

        private void DeleteFile(IndexedFile file)
        {
            try
            {
                File.Delete(file.FilePath);
                LogDeletedFile(file);
            }
            catch (IOException e)
            {
                LogExcecption(e);
            }
        }

        private void ReplicateFile(IndexedFile file)
        {
            try
            {
                File.Copy(file.FilePath, SourcePathToReplicaPath(file.FilePath));
                LogReplicatedFile(file);
            }
            catch (IOException e)
            {
                LogExcecption(e);
            }
        }

        public void LogExcecption(Exception e)
        {
            Console.WriteLine(e.Message, e.StackTrace);
        }

        private void ReplicateDirectory(IndexedDirectory sourceSubDir)
        {
            try
            {
                Directory.CreateDirectory(SourcePathToReplicaPath(sourceSubDir.DirectoryPath));
                LogReplicatedDirectory(sourceSubDir);
            }
            catch (IOException e)
            {
                LogExcecption(e);
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
