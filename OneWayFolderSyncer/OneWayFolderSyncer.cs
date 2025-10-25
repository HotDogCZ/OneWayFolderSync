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

            syncTimer = new(syncPeriodInSeconds * 1000d);
            syncTimer.Elapsed += onSyncTimerElapsed;
            syncTimer.AutoReset = true;

            Logger.Initialize(logFilePath);
        }

        internal void StartSyncing()
        {
            Logger.LogStart(sourceFolderPath, replicaFolderPath);
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
            Logger.LogUpdatedFile(source, replica);
            DeleteFile(replica);
            ReplicateFile(source);
        }

        private void DeleteDirectory(IndexedDirectory dir)
        {
            try
            {
                Logger.LogDeletingDirectory(dir);
                foreach (IndexedDirectory subdir in dir.GetIndexedSubdirs())
                {
                    DeleteDirectory(subdir);
                }
                foreach (IndexedFile file in dir.GetIndexedFiles())
                {
                    DeleteFile(file);
                }
                Directory.Delete(dir.DirectoryPath);
                Logger.LogDeletedDirectory(dir);
            }
            catch (IOException e)
            {
                Logger.LogExcecption(e);
            }
        }

        private void DeleteFile(IndexedFile file)
        {
            try
            {
                File.Delete(file.FilePath);
                Logger.LogDeletedFile(file);
            }
            catch (IOException e)
            {
                Logger.LogExcecption(e);
            }
        }

        private void ReplicateFile(IndexedFile file)
        {
            try
            {
                File.Copy(file.FilePath, SourcePathToReplicaPath(file.FilePath));
                Logger.LogReplicatedFile(file);
            }
            catch (IOException e)
            {
                Logger.LogExcecption(e);
            }
        }

        private void ReplicateDirectory(IndexedDirectory sourceSubDir)
        {
            try
            {
                Directory.CreateDirectory(SourcePathToReplicaPath(sourceSubDir.DirectoryPath));
                Logger.LogReplicatedDirectory(sourceSubDir);
            }
            catch (IOException e)
            {
                Logger.LogExcecption(e);
            }
        }

        internal void StopSyncing()
        {
            syncTimer.Stop();
            syncTimer.Dispose();
            Logger.LogStop();
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
