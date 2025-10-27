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
        private readonly string sourceFolderPath;
        private readonly string replicaFolderPath;
        private readonly Timer syncTimer;
        private readonly IFileIdStrategy fileIdStrategy;

        public OneWayFolderSyncer(
            string sourceFolderPath,
            string replicaFolderPath,
            string logFilePath,
            int syncPeriodInSeconds,
            IFileIdStrategy fileIdStrategy
        )
        {
            this.sourceFolderPath = Path.GetFullPath(sourceFolderPath);
            this.replicaFolderPath = Path.GetFullPath(replicaFolderPath);

            if (!Directory.Exists(this.sourceFolderPath))
            {
                throw new DirectoryNotFoundException(sourceFolderPath);
            }
            if (!Directory.Exists(this.replicaFolderPath))
            {
                throw new DirectoryNotFoundException(replicaFolderPath);
            }

            logFilePath = Path.GetFullPath(logFilePath);
            Logger.Initialize(logFilePath);

            syncTimer = new(syncPeriodInSeconds * 1000d);
            syncTimer.Elapsed += onSyncTimerElapsed;
            syncTimer.AutoReset = true;
            this.fileIdStrategy = fileIdStrategy;
        }

        internal void StartSyncing()
        {
            Logger.LogStart(sourceFolderPath, replicaFolderPath, syncTimer.Interval);
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
            SyncDirectory(new(sourceFolderPath, fileIdStrategy));
        }

        private void SyncDirectory(IndexedDirectory currentSourceDirectory)
        {
            string replicaPath = SourcePathToReplicaPath(currentSourceDirectory.DirectoryPath);
            IndexedDirectory currentReplicaDirectory = new IndexedDirectory(
                replicaPath,
                fileIdStrategy
            );
            currentSourceDirectory.BuildIndex();
            currentReplicaDirectory.BuildIndex();
            SyncFiles(currentSourceDirectory, currentReplicaDirectory);
            SyncDirectories(currentSourceDirectory, currentReplicaDirectory);

            // sync files on top level
            //foreach directory - recursion

            foreach (var dir in currentSourceDirectory.GetIndexedSubdirs())
            {
                Console.WriteLine($"Syncing {dir.directoryId}");
                SyncDirectory(dir);
            }
        }

        private void SyncFiles(IndexedDirectory sourceDirectory, IndexedDirectory replicaDirectory)
        {
            List<IndexedFile> unconfirmedReplicatedFiles = replicaDirectory
                .GetIndexedFiles()
                .ToList();

            // Replicate missing files
            List<SourceReplicaFilePair> potentiallyUpdatedFiles = new();

            // Delete irrelevant files
            List<IndexedFile> filesToDelete = new();
            foreach (IndexedFile file in unconfirmedReplicatedFiles)
            {
                bool sourceExists = sourceDirectory.ContainsFile(file);
                if (!sourceExists)
                {
                    filesToDelete.Add(file);
                }
            }

            foreach (IndexedFile sourceFile in sourceDirectory.GetIndexedFiles())
            {
                IndexedFile replicatedFile = replicaDirectory.GetFileById(sourceFile.fileId);
                if (replicatedFile == null)
                {
                    // sourceFile is not in replica or is renamed
                    IndexedFile fileToRename = FindFileWithSameContent(sourceFile, filesToDelete);
                    if (fileToRename == null)
                    {
                        ReplicateFile(sourceFile);
                    }
                    else
                    {
                        filesToDelete.Remove(fileToRename);
                        RenameFile(fileToRename, sourceFile);
                    }
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
                else if (!source.ContentHashEquals(replica))
                {
                    // Compare content hashes
                    UpdateFile(source, replica);
                    Console.WriteLine("Conent has changed");
                }
            }

            foreach (IndexedFile toDelete in filesToDelete)
            {
                DeleteFile(toDelete);
            }
        }

        private void RenameFile(IndexedFile fileToRename, IndexedFile targetName)
        {
            string targetFileName = targetName.FileName;
            string newPath = Path.Combine(
                Path.GetDirectoryName(fileToRename.FilePath),
                targetFileName
            );
            try
            {
                File.Move(fileToRename.FilePath, newPath);
                Logger.LogRenamed(fileToRename.FilePath, newPath);
            }
            catch (IOException e)
            {
                Logger.LogException(e);
            }
        }

        private IndexedFile FindFileWithSameContent(
            IndexedFile targetFile,
            List<IndexedFile> adepts
        )
        {
            foreach (var adept in adepts)
            {
                if (adept.ContentHashEquals(targetFile))
                {
                    return adept;
                }
            }
            return null;
        }

        private void SyncDirectories(
            IndexedDirectory sourceDirectory,
            IndexedDirectory replicaDirectory
        )
        {
            List<IndexedDirectory> unconfirmedReplicaDirectories = replicaDirectory
                .GetIndexedSubdirs()
                .ToList();

            List<IndexedDirectory> toDelete = new();
            foreach (IndexedDirectory dir in unconfirmedReplicaDirectories)
            {
                bool sourceExists = sourceDirectory.ContainsDirectory(dir);
                if (!sourceExists)
                {
                    toDelete.Add(dir);
                }
            }

            // add missing directories
            foreach (IndexedDirectory sourceSubDir in sourceDirectory.GetIndexedSubdirs())
            {
                IndexedDirectory replicatedDirectory = replicaDirectory.GetDirById(
                    sourceSubDir.directoryId
                );
                if (replicatedDirectory == null)
                {
                    IndexedDirectory dirToRename = FindDirectoryWithSameContent(
                        sourceSubDir,
                        toDelete
                    );
                    if (dirToRename == null)
                    {
                        ReplicateDirectory(sourceSubDir);
                        replicaDirectory.IndexDirectory(sourceSubDir);
                    }
                    else
                    {
                        RenameDirectory(dirToRename, sourceSubDir);
                        toDelete.Remove(dirToRename);
                    }
                }
                else
                {
                    unconfirmedReplicaDirectories.RemoveAll(
                        (x) => x.directoryId == sourceSubDir.directoryId
                    );
                }
            }

            foreach (IndexedDirectory deletion in toDelete)
            {
                DeleteDirectory(deletion);
            }
        }

        private IndexedDirectory FindDirectoryWithSameContent(
            IndexedDirectory sourceSubDir,
            List<IndexedDirectory> adepts
        )
        {
            foreach (var adept in adepts)
            {
                if (sourceSubDir.ContentHashEquals(adept))
                {
                    return adept;
                }
            }
            return null;
        }

        private void RenameDirectory(IndexedDirectory dirToRename, IndexedDirectory targetDirName)
        {
            string parentPath = Path.GetDirectoryName(dirToRename.DirectoryPath)!;
            string newDirName = Path.GetFileName(targetDirName.DirectoryPath);
            string newFullPath = Path.Combine(parentPath, newDirName);

            Directory.Move(dirToRename.DirectoryPath, newFullPath);

            Logger.LogRenamed(dirToRename.DirectoryPath, newFullPath);
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
                Logger.LogException(e);
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
                Logger.LogException(e);
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
                Logger.LogException(e);
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
                Logger.LogException(e);
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
