using System.Text;
using Common.Interfaces;
using FileSorter.FileIO.Interfaces;

namespace FileSorter.FileIO;

public class ItemWriter<T> : IItemWriter<T>
    where T : IStringSerializable<T>
{
    private bool _isDisposed;
    private readonly FileStream _fileStream;
    private readonly StreamWriter _streamWriter;

    public ItemWriter(string path, Encoding encoding)
    {
        _fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4 * 1024 * 1024);
        _streamWriter = new StreamWriter(_fileStream, encoding);
    }

    public async Task WriteAsync(T item)
    {
        await _streamWriter.WriteLineAsync(item.ToString());
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                _streamWriter.Dispose();
                _fileStream.Dispose();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
