namespace FolderSyncing.Strategies
{
    using FolderSyncing.Core;

    // File can be indetified by name.
    // However a better solution could be to use the ID of the file at the volume,
    // but that makes the solution paltform-specific -- https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info
    // For this reason I setup a strategy patter for the fileID.


    public interface IFileIdStrategy
    {
        public string GetFileId(IndexedFile file);
        public string GetDirectoryId(IndexedDirectory dir);
    }
}
