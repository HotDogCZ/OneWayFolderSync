using System;
using FolderSyncing;

class Program
{
    static void Main(string[] args)
    {
        string sourcePath = args[0];
        string replicaPath = args[1];
        string syncPeriodArg = args[2];
        string logPath = args[3];

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
            oneWayFolderSyncer = new(sourcePath, replicaPath, logPath, syncPeriod);
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
}
