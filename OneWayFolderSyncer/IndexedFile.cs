namespace FolderSyncing
{
    internal class IndexedFile
    {
        public string filePath;
        public string fileName;

        public IndexedFile(string sourceFile)
        {
            this.filePath = sourceFile;
            this.fileName = Path.GetFileName(sourceFile);
        }
    }
}
