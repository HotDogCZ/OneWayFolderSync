using System;
using FolderSyncing;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "-h")
        {
            PrintHelp();
            return;
        }
        else if (args.Length != 4 && args.Length != 0) // TODO REMOVE zero case
        {
            Console.WriteLine(
                $"Failed to run the program - expected 4 arguments got {args.Length}"
            );
            PrintHelp();
            return;
        }
        string sourcePath;
        string replicaPath;
        string syncPeriodArg;
        string logPath;
        if (args.Length == 0)
        {
            sourcePath = @"C:\Users\vojte\OneWayFolderSync\source";
            replicaPath = @"C:\Users\vojte\OneWayFolderSync\replica";
            syncPeriodArg = "5";
            logPath = @".\log.txt";
        }
        else
        {
            sourcePath = args[0];
            replicaPath = args[1];
            syncPeriodArg = args[2];
            logPath = args[3];
        }

        int syncPeriod = 10;

        try
        {
            syncPeriod = int.Parse(syncPeriodArg);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Invalid argument '{args[0]}' for SyncPeriod. {e.Message}");
            Console.Error.WriteLine($"Default value '{syncPeriod}' will be used.");
        }

        OneWayFolderSyncer oneWayFolderSyncer;
        try
        {
            oneWayFolderSyncer = new(
                sourcePath,
                replicaPath,
                logPath,
                syncPeriod,
                new FileNameBasedIdStrategy()
            );
        }
        catch (DirectoryNotFoundException e)
        {
            Console.Error.WriteLine(
                "[ERROR] Source or Replica directory does not exist. " + e.Message
            );
            return;
        }

        oneWayFolderSyncer.StartSyncing();
        Console.WriteLine("To stop synchronization press enter.");
        Console.ReadLine();
        oneWayFolderSyncer.StopSyncing();
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

Options:
  -h                      Show this help message and exit.

Example:
  OneWayFolderSyncer C:\Source C:\Replica 60 C:\Logs\replica_log.txt
";
        Console.WriteLine(helpText);
    }
}
