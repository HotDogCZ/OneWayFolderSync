namespace FolderSyncing.Utils
{
    using FolderSyncing.Core;

    internal static class FileSystemManipulation
    {
        public static void RenameDirectory(
            IndexedDirectory dirToRename,
            IndexedDirectory targetDirName
        )
        {
            string parentPath = Path.GetDirectoryName(dirToRename.DirectoryPath)!;
            string newDirName = Path.GetFileName(targetDirName.DirectoryPath);
            string newFullPath = Path.Combine(parentPath, newDirName);

            Directory.Move(dirToRename.DirectoryPath, newFullPath);

            Logger.LogRenamed(dirToRename.DirectoryPath, newFullPath);
        }

        public static void DeleteDirectory(IndexedDirectory dir)
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

        public static void DeleteFile(IndexedFile file)
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

        public static void RenameFile(IndexedFile fileToRename, IndexedFile targetName)
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
    }
}
