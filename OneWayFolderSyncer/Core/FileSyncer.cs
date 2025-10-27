namespace FolderSyncing.Core
{
    using FolderSyncing.Utils;

    public partial class OneWayFolderSyncer
    {
        private sealed class FileSyncer
        {
            private readonly OneWayFolderSyncer oneWayFolderSyncer;

            public FileSyncer(OneWayFolderSyncer oneWayFolderSyncer)
            {
                this.oneWayFolderSyncer = oneWayFolderSyncer;
            }

            private void ReplicateFile(IndexedFile source)
            {
                try
                {
                    File.Copy(
                        source.FilePath,
                        oneWayFolderSyncer.MirrorPathToReplica(source.FilePath)
                    );
                    Logger.LogReplicatedFile(source);
                }
                catch (IOException e)
                {
                    Logger.LogException(e);
                }
            }

            /// <summary>
            /// Sync files in replicaDirectory to match the files in sourceDirectory. Directories are ignored.
            /// </summary>
            public void SyncFiles(
                IndexedDirectory sourceDirectory,
                IndexedDirectory replicaDirectory
            )
            {
                // Delete irrelevant files
                List<IndexedFile> replicatedFilesToDelete = FindInvalidReplicatedFiles(
                    sourceDirectory,
                    replicaDirectory
                );

                // Find or make replica for each file in source
                List<SourceReplicaFilePair> filesToUpdate = MakeReplicaFiles(
                    sourceDirectory.GetIndexedFiles(),
                    replicaDirectory,
                    replicatedFilesToDelete
                );

                UpdateFilesInReplica(filesToUpdate);

                foreach (IndexedFile toDelete in replicatedFilesToDelete)
                {
                    FileSystemManipulation.DeleteFile(toDelete);
                }
            }

            /// <summary>
            /// Make sure all files in source have their counterpart in replica.
            /// Create a new file or find one and rename it accordingly.
            /// </summary>
            /// <param name="renameAdepts"> Files in replica that are not present in source by id.</param>
            /// <returns>A list of files that have already been in replica before. Might have the content updated.</returns>
            private List<SourceReplicaFilePair> MakeReplicaFiles(
                IEnumerable<IndexedFile> sourceFiles,
                IndexedDirectory replicaDirectory,
                List<IndexedFile> renameAdepts
            )
            {
                List<SourceReplicaFilePair> filesToUpdate = new();
                foreach (IndexedFile sourceFile in sourceFiles)
                {
                    IndexedFile replicatedFile = replicaDirectory.GetFileById(sourceFile.FileId);
                    if (replicatedFile != null)
                    {
                        // sourceFile is in replica
                        // It could have been updated.
                        filesToUpdate.Add(new(sourceFile, replicatedFile));
                        continue;
                    }
                    HandleMissingReplicaFile(sourceFile, renameAdepts);
                }

                return filesToUpdate;
            }

            private void HandleMissingReplicaFile(
                IndexedFile sourceFile,
                List<IndexedFile> renameAdepts
            )
            {
                // sourceFile is not in replica or is renamed
                IndexedFile fileToRename = FindFileWithSameContent(sourceFile, renameAdepts);
                if (fileToRename == null)
                {
                    ReplicateFile(sourceFile);
                }
                else
                {
                    renameAdepts.Remove(fileToRename);
                    FileSystemManipulation.RenameFile(fileToRename, sourceFile);
                }
            }

            /// <summary>
            /// Update content of files in replica folder to match the files in source folder.
            /// </summary>
            private void UpdateFilesInReplica(List<SourceReplicaFilePair> filesToUpdate)
            {
                // Update changed files
                foreach (SourceReplicaFilePair updated in filesToUpdate)
                {
                    IndexedFile replica = updated.Replica;
                    IndexedFile source = updated.Source;
                    // If files are different size -- definitely changed
                    if (replica.Size != source.Size || source.HasChanged(replica))
                    {
                        UpdateFile(source, replica);
                    }
                }
            }

            /// <summary>
            /// Find files in replica that do not have their counterpart in source by id.
            /// </summary>
            private static List<IndexedFile> FindInvalidReplicatedFiles(
                IndexedDirectory sourceDirectory,
                IndexedDirectory replicaDirectory
            )
            {
                List<IndexedFile> replicatedFilesToDelete = new();
                foreach (IndexedFile file in replicaDirectory.GetIndexedFiles())
                {
                    IndexedFile sourceFile = sourceDirectory.GetFileById(file.FileId);
                    if (sourceFile == null)
                    {
                        replicatedFilesToDelete.Add(file);
                    }
                }

                return replicatedFilesToDelete;
            }

            /// <summary>
            /// File is updated by deleting adn coppying it again.
            /// File could also be updated by reading the bytes and udpating only the changed aprt of file.
            /// </summary>
            /// <param name="source">Files to replicate.</param>
            /// <param name="replica">Path where the copy will be pasted.</param>
            private void UpdateFile(IndexedFile source, IndexedFile replica)
            {
                FileSystemManipulation.DeleteFile(replica);
                ReplicateFile(source);
                Logger.LogUpdatedFile(source, replica);
            }

            private static IndexedFile FindFileWithSameContent(
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
        }
    }
}
