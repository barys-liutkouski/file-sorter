using FileSorter.FileIO.Interfaces;

namespace FileSorter.ExternalSort.Core.Interfaces;

public interface IKWayMerger
{
    Task PerformKWayMergeAsync<T>(
        IReadOnlyList<IAsyncEnumerable<T>> inputStreams,
        IItemWriter<T> outputWriter,
        Action<string> progressCallback
    )
        where T : IComparable<T>;
}
