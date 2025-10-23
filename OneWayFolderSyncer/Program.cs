using System;
using FolderSyncing;

class Program
{
    static void Main(string[] args)
    {
        string replicaPath = "C:\\Users\\vojte\\VeeamSync\\replica";
        string sourcePath = "C:\\Users\\vojte\\VeeamSync\\source";
        string logPath = "C:\\Users\\vojte\\VeeamSync";
        int syncPeriod = 5;

        OneWayFolderSyncer oneWayFolderSyncer = new(sourcePath, replicaPath, logPath, syncPeriod);
        oneWayFolderSyncer.StartSyncing();
        Console.WriteLine("To stop synchronization press any enter.");
        Console.ReadLine();
        oneWayFolderSyncer.StopSyncing();
    }
}
