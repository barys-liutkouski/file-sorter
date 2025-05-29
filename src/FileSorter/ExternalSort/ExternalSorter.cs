using System.Text;
using Microsoft.Extensions.Logging;
using FileSorter.ExternalSort.Core.Interfaces;
using FileSorter.ExternalSort.Core;
using FileSorter.ExternalSort.Interfaces;
using FileSorter.FileIO.Interfaces;
using FileSorter.FileIO;
using Common.Interfaces;

namespace FileSorter.ExternalSort;

public class ExternalSorter<T> : IExternalSorter
    where T : IStringSerializable<T>, IComparable<T>
{
    private readonly IFileService<T> _fileService;
    private readonly ITempFileManager _tempFileManager;
    private readonly IKWayMerger _kWayMerger;
    private readonly ILogger<ExternalSorter<T>> _logger;
    private bool _disposedValue;

    public ExternalSorter(IFileService<T> fileService, ITempFileManager tempFileManager, IKWayMerger kWayMerger, ILogger<ExternalSorter<T>> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _tempFileManager = tempFileManager ?? throw new ArgumentNullException(nameof(tempFileManager));
        _kWayMerger = kWayMerger ?? throw new ArgumentNullException(nameof(kWayMerger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SortFileAsync(string inputFilePath, string outputFilePath, SortOptions sortOptions)
    {
        ArgumentNullException.ThrowIfNull(sortOptions);
        ArgumentNullException.ThrowIfNull(sortOptions.ProgressCallback);
        ArgumentNullException.ThrowIfNull(sortOptions.Encoding);

        _logger.LogInformation("Starting chunked file sorting...");
        _logger.LogInformation("Using encoding: {EncodingName} ({EncodingWebName})", sortOptions.Encoding.EncodingName, sortOptions.Encoding.WebName);

        var tempFiles = new List<string>();

        try
        {
            await foreach (var nextChunk in _fileService.ReadFileInChunksAsync(inputFilePath, sortOptions.Encoding, sortOptions.MaxChunkSizeMB))
            {
                var tempFilePath = _tempFileManager.CreateTemporaryFile();
                tempFiles.Add(tempFilePath);

                nextChunk.Sort();
                await WriteChunkToFileAsync(tempFilePath, sortOptions.Encoding, nextChunk);

                sortOptions.ProgressCallback($"Chunk {tempFiles.Count} read, sorted, and written, to temporary file: {tempFilePath}.");
            }

            _logger.LogInformation("Created {TempFileCount} temporary files for sorted chunks", tempFiles.Count);

            await KWayMergeFilesAsync(tempFiles, outputFilePath, sortOptions);
            _logger.LogInformation("K-way merge completed - final sorted file written to: {OutputFilePath}", outputFilePath);
        }
        finally
        {
            _tempFileManager.CleanupTemporaryFiles();
        }
    }

    private async Task KWayMergeFilesAsync(List<string> fileNames, string outputFileName, SortOptions sortOptions)
    {
        if (fileNames.Count <= sortOptions.MaxFileHandles)
        {
            var inputStreams = fileNames.Select(filename => new ChunkReader<T>(filename, sortOptions.Encoding)).ToList();
            using var output = new ItemWriter<T>(outputFileName, sortOptions.Encoding);
            await _kWayMerger.PerformKWayMergeAsync(inputStreams, output, sortOptions.ProgressCallback);
            return;
        }

        var chunks = fileNames.Chunk(sortOptions.MaxFileHandles).ToList();

        _logger.LogInformation("Too many chunks for a single k-way merge. Split to {ChunkCount} sub-chunks", chunks.Count);

        var tempResultFiles = new List<string>();

        var chunkNumber = 0;
        foreach (var chunk in chunks)
        {
            var chunkResultFile = _tempFileManager.CreateTemporaryFile();
            tempResultFiles.Add(chunkResultFile);

            await KWayMergeFilesAsync(chunk.ToList(), chunkResultFile, sortOptions);

            _logger.LogInformation("Finished k-way merge for sub-chunk {ChunkNumber}. {ChunksRemaining} sub-chunks remaining", ++chunkNumber, chunks.Count - chunkNumber);
            _tempFileManager.CleanupTemporaryFiles(chunk);
        }

        await KWayMergeFilesAsync(tempResultFiles, outputFileName, sortOptions);
    }

    private async Task WriteChunkToFileAsync(string filePath, Encoding encoding, List<T> chunk)
    {
        using var writer = new ItemWriter<T>(filePath, encoding);
        foreach (var item in chunk)
        {
            await writer.WriteAsync(item);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _disposedValue = true;

            if (disposing)
            {
                _tempFileManager.Dispose();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
