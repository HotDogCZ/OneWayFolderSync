namespace FolderSyncing
{
    internal static class Logger
    {
        private static string logFilePath = "";

        public static void Initialize(string logFolderPath)
        {
            if (File.Exists(logFolderPath))
            {
                logFilePath = logFolderPath;
            }
            else
            {
                logFilePath = Path.Combine(logFolderPath, "log.txt");
            }
        }

        private static void LogMessage(string message)
        {
            message = $"{DateTime.Now}: {message}";
            Console.WriteLine(message);

            EnsurePathValid();

            try
            {
                using (StreamWriter streamWriter = File.AppendText(logFilePath))
                {
                    streamWriter.WriteLine(message);
                }
            }
            catch (Exception e)
            {
                // Catch exceptions to prevent crashing of program beacuse of a failed log
                Console.Error.WriteLine($"[Logger error] Failed to write log: {e.Message}");
            }
        }

        public static void LogReplicatedDirectory(IndexedDirectory dir)
        {
            LogMessage($"Replicated directory {dir.DirectoryPath} from source.");
        }

        public static void LogReplicatedFile(IndexedFile file)
        {
            LogMessage($"Replicated {file.FileName} from source.");
        }

        public static void LogDeletedFile(IndexedFile file)
        {
            LogMessage($"Deleted {file.FileName} from replica -- file no longer exists in source.");
        }

        public static void LogUpdatedFile(IndexedFile source, IndexedFile replica)
        {
            LogMessage(
                $"Updated {replica.FileName} in replica to match {source.FileName} in source."
            );
        }

        public static void LogException(Exception e)
        {
            LogMessage($"[EXCEPTION] {e.Message}\n{e.StackTrace}");
        }

        public static void LogDeletingDirectory(IndexedDirectory dir)
        {
            LogMessage($"Trying to delete directory {dir.DirectoryId}...");
        }

        public static void LogDeletedDirectory(IndexedDirectory dir)
        {
            LogMessage($"Fully deleted directory {dir.DirectoryId}");
        }

        internal static void LogStart(string sourcePath, string replicaPath, double interval)
        {
            LogMessage(
                $"Synchronization starts. Source: {sourcePath}. Replica: {replicaPath}. Synchroniazion period: {interval / 1000d} seconds."
            );
        }

        internal static void LogStop()
        {
            LogMessage($"Synchronization ended.");
        }

        private static void EnsurePathValid()
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                Console.WriteLine($"[Logger] No Path for logs supplied. Only logging to console");
            }
            else
            {
                string logsDirectory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(logsDirectory))
                {
                    Console.WriteLine(
                        $"[Logger] Directory '{logsDirectory}' for logs does not exist - it will be created."
                    );
                    Directory.CreateDirectory(logsDirectory);
                }
            }
        }

        internal static void LogRenamed(string fileToRename, string newPath)
        {
            LogMessage(
                $"Renamed {fileToRename} to {Path.GetFileName(newPath)} - the content matches."
            );
        }
    }
}
