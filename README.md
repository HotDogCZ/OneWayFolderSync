# OneWayFolderSyncer

A C# cosnole application that synchronizes the contents of a **replica folder** to match a **source folder** at regular intervals.  
All changes in the source folder (additions, deletions, modifications) will be reflected in the replica folder.

---

## Fodler structure 
OneWayFolderSyncer\ - the whole project structure
bin\ - a release build of the application

## Features

- One-way synchronization from source to replica.
- Detects modifications using file **timestamps** or **content hashes**.
- Configurable synchronization interval.
- Logging of synchronization events.


---

## Usage

Usage:
  OneWayFolderSyncer <source_folder> <replica_folder> <sync_period_seconds> <log_path> [comparison_strategy] 

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
