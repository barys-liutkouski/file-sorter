using FileSorter.FileIO.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSorter.FileIO;

public class TempFileManager : ITempFileManager
{
    private readonly List<string> _temporaryFiles = [];
    private bool _isDisposed = false;
    private readonly ILogger<TempFileManager> _logger;
    private readonly string? _customTempDirectory;

    public TempFileManager(ILogger<TempFileManager> logger, string? customTempDirectoryPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customTempDirectory = customTempDirectoryPath;

        if (!string.IsNullOrWhiteSpace(_customTempDirectory))
        {
            try
            {
                Directory.CreateDirectory(_customTempDirectory);
                _logger.LogInformation("Custom temporary directory set: {DirectoryPath}", _customTempDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create or access custom temporary directory {DirectoryPath}. Falling back to default temporary directory.",
                    _customTempDirectory
                );
                _customTempDirectory = null;
                throw;
            }
        }
    }

    public string CreateTemporaryFile()
    {
        string tempFilePath;
        string effectiveTempDirectory;

        if (!string.IsNullOrWhiteSpace(_customTempDirectory))
        {
            effectiveTempDirectory = _customTempDirectory;
            tempFilePath = Path.Combine(effectiveTempDirectory, Path.GetRandomFileName());
            using var fs = File.Create(tempFilePath);
        }
        else
        {
            tempFilePath = Path.GetTempFileName();
            effectiveTempDirectory = Path.GetTempPath();
        }

        _temporaryFiles.Add(tempFilePath);
        _logger.LogInformation(
            "Created temporary file: {FileName} in directory {Directory}",
            Path.GetFileName(tempFilePath),
            effectiveTempDirectory
        );
        return tempFilePath;
    }

    public void CleanupTemporaryFiles(IEnumerable<string>? fileNames = null)
    {
        if (_temporaryFiles.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Cleaning up {FilesToRemoveCount} temporary files", _temporaryFiles.Count);
        var deletedFiles = 0;
        var failedDeletions = 0;
        var filesToRemove = fileNames ?? _temporaryFiles;

        foreach (var tempFile in filesToRemove)
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                    deletedFiles++;
                    _logger.LogInformation("Deleted temporary file: {FileName}", Path.GetFileName(tempFile));
                }
            }
            catch (Exception ex)
            {
                failedDeletions++;
                _logger.LogWarning(
                    ex,
                    "Failed to delete temporary file {FileName}: {ErrorMessage}",
                    Path.GetFileName(tempFile),
                    ex.Message
                );
            }
        }
        _temporaryFiles.RemoveAll(filesToRemove.Contains);
        _logger.LogInformation(
            "Cleanup completed: {DeletedFiles} files deleted, {FailedDeletions} failed deletions",
            deletedFiles,
            failedDeletions
        );
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                CleanupTemporaryFiles();
            }
        }
    }
}
