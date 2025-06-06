using System.Text;
using Common.Interfaces;
using FileSorter.FileIO.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSorter.FileIO;

public class FileService<T> : IFileService<T>
    where T : IStringSerializable<T>
{
    private readonly ILogger<FileService<T>> _logger;

    public FileService(ILogger<FileService<T>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<List<T>> ReadFileInChunksAsync(
        string filePath,
        Encoding encoding,
        int maxChunkSizeMB,
        int bufferSize = 4 * 1024 * 1024
    )
    {
        ArgumentNullException.ThrowIfNull(encoding);

        var chunk = new List<T>();
        long chunkSize = 0;
        long maxChunkSize = (long)maxChunkSizeMB * 1024 * 1024;

        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: bufferSize
        );
        using var reader = new StreamReader(fileStream);

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null)
            {
                yield return chunk;
                break;
            }
            if (T.TryParse(line, encoding, out var parsed, out var _))
            {
                chunk.Add(parsed);
                chunkSize += encoding.GetByteCount(line);
            }

            if (chunkSize > maxChunkSize)
            {
                yield return chunk;
                chunk = [];
                chunkSize = 0;
            }
        }
    }
}
