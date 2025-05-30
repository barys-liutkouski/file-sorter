using System.Text;
using Common.Exceptions;
using Common.Interfaces;

namespace FileSorter.ExternalSort.Core;

public class ChunkReader<T> : IAsyncEnumerable<T>
    where T : IStringSerializable<T>
{
    private readonly string _path;
    private readonly Encoding _encoding;
    private readonly int _bufferSize;

    public ChunkReader(string path, Encoding encoding, int bufferSize = 4 * 1024 * 1024)
    {
        _path = path;
        _encoding = encoding;
        _bufferSize = bufferSize;
    }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize);
        using var reader = new StreamReader(fs);

        while (true)
        {
            var nextLineText = await reader.ReadLineAsync(cancellationToken);
            if (nextLineText is null)
            {
                break;
            }

            if (T.TryParse(nextLineText, _encoding, out var item, out var error))
            {
                yield return item;
            }
            else
            {
                throw new LineParsingException(error);
            }
        }
    }
}
