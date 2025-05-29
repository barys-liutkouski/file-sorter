namespace FileSorter.FileIO.Interfaces;

public interface IItemWriter<T> : IDisposable
{
    Task WriteAsync(T item);
}
