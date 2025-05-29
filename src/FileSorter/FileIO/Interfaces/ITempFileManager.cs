namespace FileSorter.FileIO.Interfaces;

public interface ITempFileManager : IDisposable
{
    string CreateTemporaryFile();
    void CleanupTemporaryFiles(IEnumerable<string>? fileNames = null);
}
