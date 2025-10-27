namespace FolderSyncing.Core
{
    using FolderSyncing.Strategies;
    using FolderSyncing.Utils;

    public partial class OneWayFolderSyncer
    {
        private sealed class DirectorySyncer
        {
            private readonly FileSyncer fileSyncer;
            private readonly IFileIdStrategy fileIdStrategy;
            private readonly IModifiedStrategy modifiedStrategy;
            private readonly OneWayFolderSyncer oneWayFolderSyncer;

            public DirectorySyncer(OneWayFolderSyncer oneWayFolderSyncer)
            {
                this.fileIdStrategy = oneWayFolderSyncer.fileIdStrategy;
                this.modifiedStrategy = oneWayFolderSyncer.modifiedStrategy;
                this.oneWayFolderSyncer = oneWayFolderSyncer;

                fileSyncer = new(oneWayFolderSyncer);
            }

            public void SyncDirectory(IndexedDirectory currentSourceDirectory)
            {
                string replicaPath = oneWayFolderSyncer.MirrorPathToReplica(
                    currentSourceDirectory.DirectoryPath
                );
                IndexedDirectory currentReplicaDirectory = new IndexedDirectory(
                    replicaPath,
                    fileIdStrategy,
                    modifiedStrategy
                );
                currentSourceDirectory.BuildIndex();
                currentReplicaDirectory.BuildIndex();
                fileSyncer.SyncFiles(currentSourceDirectory, currentReplicaDirectory);
                SyncDirectories(currentSourceDirectory, currentReplicaDirectory);

                // sync files on top level
                //foreach directory - recursion

                foreach (var dir in currentSourceDirectory.GetIndexedSubdirs())
                {
                    Console.WriteLine($"Syncing {dir.DirectoryId}");
                    SyncDirectory(dir);
                }
            }

            private void SyncDirectories(
                IndexedDirectory sourceDirectory,
                IndexedDirectory replicaDirectory
            )
            {
                List<IndexedDirectory> renameAdepts = FindInvalidDirectories(
                    sourceDirectory,
                    replicaDirectory
                );

                // add missing directories
                MakeMissingDirectories(
                    sourceDirectory.GetIndexedSubdirs(),
                    replicaDirectory,
                    renameAdepts
                );

                foreach (IndexedDirectory deletion in renameAdepts)
                {
                    FileSystemManipulation.DeleteDirectory(deletion);
                }
            }

            private void MakeMissingDirectories(
                IEnumerable<IndexedDirectory> sourceSubdirs,
                IndexedDirectory replicaDirectory,
                List<IndexedDirectory> renameAdepts
            )
            {
                foreach (IndexedDirectory sourceSubDir in sourceSubdirs)
                {
                    IndexedDirectory replicatedDirectory = replicaDirectory.GetDirById(
                        sourceSubDir.DirectoryId
                    );

                    if (replicatedDirectory != null)
                    {
                        // Replicated directory exists already.
                        continue;
                    }
                    IndexedDirectory dirToRename = FindDirectoryWithSameContent(
                        sourceSubDir,
                        renameAdepts
                    );
                    if (dirToRename == null)
                    {
                        ReplicateDirectory(sourceSubDir);
                        replicaDirectory.IndexDirectory(sourceSubDir);
                    }
                    else
                    {
                        FileSystemManipulation.RenameDirectory(dirToRename, sourceSubDir);
                        renameAdepts.Remove(dirToRename);
                    }
                }
            }

            private static List<IndexedDirectory> FindInvalidDirectories(
                IndexedDirectory sourceDirectory,
                IndexedDirectory replicaDirectory
            )
            {
                List<IndexedDirectory> toDelete = new();
                foreach (IndexedDirectory dir in replicaDirectory.GetIndexedSubdirs())
                {
                    bool sourceExists = sourceDirectory.ContainsDirectory(dir);
                    if (!sourceExists)
                    {
                        toDelete.Add(dir);
                    }
                }

                return toDelete;
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

            private void ReplicateDirectory(IndexedDirectory sourceSubDir)
            {
                try
                {
                    Directory.CreateDirectory(
                        oneWayFolderSyncer.MirrorPathToReplica(sourceSubDir.DirectoryPath)
                    );
                    Logger.LogReplicatedDirectory(sourceSubDir);
                }
                catch (IOException e)
                {
                    Logger.LogException(e);
                }
            }
        }
    }
}
