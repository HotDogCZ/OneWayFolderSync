using FolderSyncing.Core;
using FolderSyncing.Strategies;
using FolderSyncing.Utils;

static class Program
{
    public const string TIME_STRATEGY = "modifiedtime";
    public const string HASH_STRATEGY = "modifiedhash";

    static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "-h")
        {
            PrintHelp();
            return;
        }
        SyncConfig syncConfig = ParseArguments(args);
        OneWayFolderSyncer oneWayFolderSyncer = new(syncConfig);

        oneWayFolderSyncer.StartSyncing();
        Console.WriteLine("To stop synchronization press enter.");
        Console.ReadLine();
        oneWayFolderSyncer.StopSyncing();
    }

    private static SyncConfig ParseArguments(string[] args)
    {
        if (args.Length != 4 && args.Length != 5)
            throw new ArgumentException($"Expected 4 or 5 arguments, got {args.Length}");

        string sourcePath = args[0];
        string replicaPath = args[1];
        string syncPeriodArg = args[2];
        string logPath = args[3];
        string strategyArg = args.Length == 5 ? args[4].ToLower() : HASH_STRATEGY;

        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"Source directory '{sourcePath}' not found.");
        if (!Directory.Exists(replicaPath))
            throw new DirectoryNotFoundException($"Replica directory '{replicaPath}' not found.");
        if (!Directory.Exists(Path.GetDirectoryName(logPath)))
            throw new DirectoryNotFoundException($"Log path '{logPath}' is invalid.");

        if (!int.TryParse(syncPeriodArg, out int syncPeriod) || syncPeriod <= 0)
            throw new ArgumentException($"Invalid synchronization period '{syncPeriodArg}'.");

        IModifiedStrategy modifiedStrategy;
        switch (strategyArg)
        {
            case TIME_STRATEGY:
                modifiedStrategy = new ModifiedTimeStrategy();
                break;
            case HASH_STRATEGY:
                modifiedStrategy = new ModifiedContentHashStrategy();
                break;
            default:
                throw new ArgumentException(
                    $"Invalid comparison strategy '{strategyArg}'. Use '{TIME_STRATEGY}' or '{HASH_STRATEGY}'."
                );
        }

        IFileIdStrategy defaultFileIdStrategy = new FileNameBasedIdStrategy();
        return new SyncConfig(
            sourcePath,
            replicaPath,
            logPath,
            syncPeriod,
            modifiedStrategy,
            defaultFileIdStrategy
        );
    }

    private static void PrintHelp()
    {
        string helpText =
            @"
Usage:
  OneWayFolderSyncer <source_folder> <replica_folder> <sync_period_seconds> <log_path>

Description:
  Synchronizes the contents of the replica folder to match the source folder at regular intervals.
  All changes in the source folder (additions, deletions, modifications) will be reflected 
  in the replica folder.

Arguments:
  <source_folder>         Path to the folder that will be used as the source.
  <replica_folder>        Path to the folder where the replica will be stored.
  <sync_period_seconds>   How often synchronization should occur (in seconds).
  <log_path>              Path to a log file (.txt) or a folder where the log will be stored.
  [comparison_strategy]   (Optional) Method used to detect modified files:
                            'modifiedtime' - compares last modified timestamps - might not be reliable in some cases.
                            'modifiedhash' - compares file content hashes (default) - can be slower.

Options:
  -h                      Show this help message and exit.

Example:
  OneWayFolderSyncer C:\Source C:\Replica 60 C:\Logs\replica_log.txt
  OneWayFolderSyncer C:\Source C:\Replica 60 C:\Logs\replica_log.txt modifiedtime
";
        Console.WriteLine(helpText);
    }
}
