using System;
using FolderSyncing;

class Program
{
    static void Main(string[] args)
    {
        string projectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, @"..\..\..\..")
        );
        string replicaPath = Path.Combine(projectRoot, "replica");
        string sourcePath = Path.Combine(projectRoot, "source");
        string logPath = Path.Combine(projectRoot, "logs\\log.txt");
        int syncPeriod = 5;

        OneWayFolderSyncer oneWayFolderSyncer = new(sourcePath, replicaPath, logPath, syncPeriod);
        oneWayFolderSyncer.StartSyncing();
        Console.WriteLine("To stop synchronization press enter.");
        Console.ReadLine();
        oneWayFolderSyncer.StopSyncing();
    }
}
