using FileSorter.FileIO.Interfaces;
using FileSorter.ExternalSort.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSorter.ExternalSort.Core;

public class KWayMerger : IKWayMerger
{
    private readonly ILogger<KWayMerger> _logger;

    public KWayMerger(ILogger<KWayMerger> logger)
    {
        _logger = logger;
    }

    private struct HeapEntry<T>(T value, IAsyncEnumerator<T> enumerator) : IComparable<HeapEntry<T>>
        where T : IComparable<T>
    {
        public T Value = value;
        public readonly IAsyncEnumerator<T> Enumerator = enumerator;

        public int CompareTo(HeapEntry<T> other)
        {
            return Value.CompareTo(other.Value);
        }
    }

    public async Task PerformKWayMergeAsync<T>(
        IReadOnlyList<IAsyncEnumerable<T>> inputStreams,
        IItemWriter<T> outputWriter,
        Action<string> progressCallback)
        where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(inputStreams);
        ArgumentNullException.ThrowIfNull(outputWriter);
        ArgumentNullException.ThrowIfNull(progressCallback);

        if (!inputStreams.Any())
        {
            _logger.LogInformation("No temporary files to merge. Output file will be empty.");
            return;
        }

        _logger.LogInformation("Starting K-way merge of {FileCount} files...", inputStreams.Count);

        var minHeap = new PriorityQueue<HeapEntry<T>, HeapEntry<T>>(inputStreams.Count);

        try
        {
            for (int i = 0; i < inputStreams.Count; i++)
            {
                var enumerator = inputStreams[i].GetAsyncEnumerator();
                if (await enumerator.MoveNextAsync())
                {
                    var entry = new HeapEntry<T>(enumerator.Current, enumerator);
                    minHeap.Enqueue(entry, entry);
                }
                else
                {
                    await enumerator.DisposeAsync();
                }
            }

            long totalLinesWritten = 0;
            long lastProgressReportLines = 0;

            while (minHeap.Count > 0)
            {
                var minEntry = minHeap.Dequeue();
                await outputWriter.WriteAsync(minEntry.Value);
                totalLinesWritten++;

                if (totalLinesWritten - lastProgressReportLines >= 1000000)
                {
                    progressCallback($"Processed {totalLinesWritten} lines");
                    lastProgressReportLines = totalLinesWritten;
                }

                var enumerator = minEntry.Enumerator;
                if (await enumerator.MoveNextAsync())
                {
                    minEntry.Value = enumerator.Current;
                    minHeap.Enqueue(minEntry, minEntry);
                }
                else
                {
                    await enumerator.DisposeAsync();
                }
            }
            _logger.LogInformation("K-way merge completed: {TotalLinesWritten:N0} total lines written", totalLinesWritten);
        }
        finally
        {
            foreach (var (element, _) in minHeap.UnorderedItems)
            {
                var enumerator = element.Enumerator;
                await enumerator.DisposeAsync();
            }
        }
    }
}
