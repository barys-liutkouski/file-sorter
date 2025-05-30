using System.Text;

namespace FileSorter.FileIO.Interfaces;

public interface IFileService<T>
{
    IAsyncEnumerable<List<T>> ReadFileInChunksAsync(
        string filePath,
        Encoding encoding,
        int maxChunkSizeMB,
        int bufferSize = 4 * 1024 * 1024
    );
}
